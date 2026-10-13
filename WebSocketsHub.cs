using Betfair.ESASwagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadTrader.Simulator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpreadTrader
{
    // The former local OWIN/SignalR 2 hub is retired. The canonical proxy is
    // ASP.NET Core SignalR, so this small dependency-free client speaks its
    // JSON hub protocol over ClientWebSocket.
    internal class WebSocketsHub
    {
        public static WebSocketsHub Instance { get; } = new WebSocketsHub();
        private readonly BlockingCollection<string> _orderQueue = new BlockingCollection<string>(10000);
        private readonly BlockingCollection<MarketChangeDto> _marketChangeQueue = new BlockingCollection<MarketChangeDto>();
        private readonly object _handlersLock = new object();
        private readonly object _socketLock = new object();
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, List<BetsManager>> _ordersHandlers = new Dictionary<string, List<BetsManager>>();
        private readonly Dictionary<string, List<RunnersControl>> _marketHandlers = new Dictionary<string, List<RunnersControl>>();
        private readonly HashSet<string> _subscribedMarkets = new HashSet<string>(StringComparer.Ordinal);
        private ClientWebSocket _socket;
        private CancellationTokenSource _socketCts;
        private int _invocationId;
        private Properties.Settings props = Properties.Settings.Default;

        public void Attach(string marketId, BetsManager manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; lock (_handlersLock) { if (!_ordersHandlers.TryGetValue(marketId, out var h)) _ordersHandlers[marketId] = h = new List<BetsManager>(); if (!h.Contains(manager)) h.Add(manager); } }
        public void Detach(string marketId, BetsManager manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; lock (_handlersLock) { if (_ordersHandlers.TryGetValue(marketId, out var h)) { h.Remove(manager); if (h.Count == 0) _ordersHandlers.Remove(marketId); } } }
        public void Attach(string marketId, RunnersControl manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; bool subscribe = false; lock (_handlersLock) { if (!_marketHandlers.TryGetValue(marketId, out var h)) _marketHandlers[marketId] = h = new List<RunnersControl>(); if (!h.Contains(manager)) h.Add(manager); subscribe = h.Count == 1; } if (subscribe) _ = SubscribeAsync(marketId); }
        public void Detach(string marketId, RunnersControl manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; bool unsubscribe = false; lock (_handlersLock) { if (_marketHandlers.TryGetValue(marketId, out var h)) { h.Remove(manager); if (h.Count == 0) { _marketHandlers.Remove(marketId); unsubscribe = true; } } } if (unsubscribe) _ = UnsubscribeAsync(marketId); }
        private List<BetsManager> OrderHandlers(string id) { lock (_handlersLock) return _ordersHandlers.TryGetValue(id, out var h) ? new List<BetsManager>(h) : new List<BetsManager>(); }
        private List<RunnersControl> MarketHandlers(string id) { lock (_handlersLock) return _marketHandlers.TryGetValue(id, out var h) ? new List<RunnersControl>(h) : new List<RunnersControl>(); }
        public void Start() { _ = Task.Run(OrderLoop); _ = Task.Run(MarketLoop); }
        private WebSocketsHub() { ControlMessenger.MessageSent += OnMessageReceived; Start(); Connect(); }
        public void Simulate(OrderMarketChange change) { foreach (var manager in OrderHandlers(change.Id)) manager.OnOrderChanged(change); }
        private void OnMessageReceived(string name, object data) { if (name != "Reconnect") return; dynamic d = data; Connect(); _ = SubscribeAsync((string)d.MarketId); }
        private Uri HubUri { get { return new Uri("ws://" + props.WebSocketsUrl.TrimEnd('/') + "/marketHub"); } }
        private void OrderLoop() { foreach (var json in _orderQueue.GetConsumingEnumerable()) { var change = JsonConvert.DeserializeObject<OrderMarketChange>(json); if (change == null || change.Id == null) continue; foreach (var manager in OrderHandlers(change.Id)) { manager.OnOrderChanged(change); ControlMessenger.Send(""); } } }
        private void MarketLoop() { foreach (var change in _marketChangeQueue.GetConsumingEnumerable()) { var latest = change; while (_marketChangeQueue.TryTake(out var next)) latest = next; foreach (var manager in MarketHandlers(latest.MarketId)) manager.OnMarketChanged(latest); } }
        private async Task ConnectAsync()
        {
            try
            {
                var socket = new ClientWebSocket(); socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15); var cts = new CancellationTokenSource();
                await socket.ConnectAsync(HubUri, cts.Token); await SendRawAsync(socket, "{\"protocol\":\"json\",\"version\":1}\u001e", cts.Token);
                lock (_socketLock) { if (_socketCts != null) _socketCts.Cancel(); _socket = socket; _socketCts = cts; }
                Debug.WriteLine("Canonical SignalR stream connected: " + HubUri);
                string[] markets; lock (_handlersLock) markets = new List<string>(_subscribedMarkets).ToArray(); foreach (string market in markets) await InvokeAsync("SubscribeMarket", market);
                await ReceiveLoopAsync(socket, cts.Token);
            }
            catch (Exception ex) { Debug.WriteLine("Canonical SignalR stream error: " + ex.Message); }
        }
        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192]; var text = new StringBuilder();
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token); if (result.MessageType == WebSocketMessageType.Close) break; text.Append(Encoding.UTF8.GetString(buffer, 0, result.Count)); string all = text.ToString(); int end;
                while ((end = all.IndexOf('\u001e')) >= 0) { HandleMessage(all.Substring(0, end)); all = all.Substring(end + 1); } text.Clear(); text.Append(all);
            }
            lock (_socketLock) { if (ReferenceEquals(_socket, socket)) { _socket = null; _socketCts = null; } }
        }
        private void HandleMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || payload == "{}") return;
            try { var message = JObject.Parse(payload); if ((int?)message["type"] != 1) return; string target = (string)message["target"]; if (target == "marketChanged") { var change = message["arguments"]?[0]?.ToObject<MarketChangeDto>(); if (change != null) _marketChangeQueue.Add(change); } else if (target == "ordersChanged") _orderQueue.Add((string)message["arguments"]?[0]); }
            catch (Exception ex) { Debug.WriteLine("Canonical SignalR message ignored: " + ex.Message); }
        }
        private async Task InvokeAsync(string target, string marketId)
        {
            ClientWebSocket socket; CancellationToken token; lock (_socketLock) { socket = _socket; token = _socketCts == null ? CancellationToken.None : _socketCts.Token; }
            if (socket == null || socket.State != WebSocketState.Open) return; string json = JsonConvert.SerializeObject(new { type = 1, invocationId = Interlocked.Increment(ref _invocationId).ToString(), target = target, arguments = new[] { marketId } }) + "\u001e"; await SendRawAsync(socket, json, token);
        }
        private async Task SendRawAsync(ClientWebSocket socket, string value, CancellationToken token) { byte[] bytes = Encoding.UTF8.GetBytes(value); await _sendGate.WaitAsync(token); try { await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token); } finally { _sendGate.Release(); } }
        private async Task SubscribeAsync(string marketId) { lock (_handlersLock) _subscribedMarkets.Add(marketId); await InvokeAsync("SubscribeMarket", marketId); }
        private async Task UnsubscribeAsync(string marketId) { lock (_handlersLock) _subscribedMarkets.Remove(marketId); await InvokeAsync("UnsubscribeMarket", marketId); }
        private void Connect() { _ = ConnectAsync(); }
    }
}
