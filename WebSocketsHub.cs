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
        private readonly BlockingCollection<MarketChangeDto> _marketChangeQueue = new BlockingCollection<MarketChangeDto>(5000);
        private readonly object _handlersLock = new object();
        private readonly object _socketLock = new object();
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, List<BetsManager>> _ordersHandlers = new Dictionary<string, List<BetsManager>>();
        private readonly Dictionary<string, List<RunnersControl>> _marketHandlers = new Dictionary<string, List<RunnersControl>>();
        private readonly HashSet<string> _subscribedMarkets = new HashSet<string>(StringComparer.Ordinal);
        private ClientWebSocket _socket;
        private CancellationTokenSource _socketCts;
        private int _invocationId;
        private int _connectLoopStarted;
        private int _started;
        private int _droppedOrderMessages;
        private int _droppedMarketMessages;
        private long _connectAttempts;
        private long _reconnects;
        private long _messagesReceived;
        private long _marketMessages;
        private long _orderMessages;
        private long _invalidMessages;
        private long _lastConnectedTicks;
        private long _lastDisconnectedTicks;
        private long _lastMessageTicks;
        private long _lastErrorTicks;
        private string _lastError;
        private Timer _healthTimer;
        private int _connected;
        private volatile bool _stopped;
        private Properties.Settings props = Properties.Settings.Default;

        public void Attach(string marketId, BetsManager manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; lock (_handlersLock) { if (!_ordersHandlers.TryGetValue(marketId, out var h)) _ordersHandlers[marketId] = h = new List<BetsManager>(); if (!h.Contains(manager)) h.Add(manager); } }
        public void Detach(string marketId, BetsManager manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; lock (_handlersLock) { if (_ordersHandlers.TryGetValue(marketId, out var h)) { h.Remove(manager); if (h.Count == 0) _ordersHandlers.Remove(marketId); } } }
        public void Attach(string marketId, RunnersControl manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; bool subscribe = false; lock (_handlersLock) { if (!_marketHandlers.TryGetValue(marketId, out var h)) _marketHandlers[marketId] = h = new List<RunnersControl>(); if (!h.Contains(manager)) h.Add(manager); subscribe = h.Count == 1; } if (subscribe) _ = SubscribeAsync(marketId); }
        public void Detach(string marketId, RunnersControl manager) { if (string.IsNullOrEmpty(marketId) || manager == null) return; bool unsubscribe = false; lock (_handlersLock) { if (_marketHandlers.TryGetValue(marketId, out var h)) { h.Remove(manager); if (h.Count == 0) { _marketHandlers.Remove(marketId); unsubscribe = true; } } } if (unsubscribe) _ = UnsubscribeAsync(marketId); }
        private List<BetsManager> OrderHandlers(string id) { lock (_handlersLock) return _ordersHandlers.TryGetValue(id, out var h) ? new List<BetsManager>(h) : new List<BetsManager>(); }
        private List<RunnersControl> MarketHandlers(string id) { lock (_handlersLock) return _marketHandlers.TryGetValue(id, out var h) ? new List<RunnersControl>(h) : new List<RunnersControl>(); }
        public void Start() { if (_stopped || Interlocked.Exchange(ref _started, 1) != 0) return; _healthTimer = new Timer(_ => WriteHealthSnapshot(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10)); _ = Task.Run(OrderLoop); _ = Task.Run(MarketLoop); Connect(); }
        private WebSocketsHub() { ControlMessenger.MessageSent += OnMessageReceived; Start(); }
        public void Simulate(OrderMarketChange change) { foreach (var manager in OrderHandlers(change.Id)) manager.OnOrderChanged(change); }
        private void OnMessageReceived(string name, object data) { if (_stopped || name != "Reconnect") return; Connect(); }
        private Uri HubUri { get { return new Uri("ws://" + props.WebSocketsUrl.TrimEnd('/') + "/marketHub"); } }
        private void OrderLoop() { foreach (var json in _orderQueue.GetConsumingEnumerable()) { var change = JsonConvert.DeserializeObject<OrderMarketChange>(json); if (change == null || change.Id == null) continue; foreach (var manager in OrderHandlers(change.Id)) { manager.OnOrderChanged(change); ControlMessenger.Send(""); } } }
        private void MarketLoop() { foreach (var change in _marketChangeQueue.GetConsumingEnumerable()) { var latest = change; while (_marketChangeQueue.TryTake(out var next)) latest = next; foreach (var manager in MarketHandlers(latest.MarketId)) manager.OnMarketChanged(latest); } }
        private async Task ConnectAsync()
        {
            try
            {
                Interlocked.Increment(ref _connectAttempts);
                Trace.WriteLine("Canonical SignalR stream connection attempt: " + HubUri);
                var socket = new ClientWebSocket(); socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15); var cts = new CancellationTokenSource();
                await socket.ConnectAsync(HubUri, cts.Token); await SendRawAsync(socket, "{\"protocol\":\"json\",\"version\":1}\u001e", cts.Token);
                lock (_socketLock) { if (_socketCts != null) _socketCts.Cancel(); _socket = socket; _socketCts = cts; }
                var wasConnected = Interlocked.Exchange(ref _connected, 1);
                if (wasConnected == 0)
                {
                    Interlocked.Increment(ref _reconnects);
                    Interlocked.Exchange(ref _lastConnectedTicks, DateTime.UtcNow.Ticks);
                }
                Trace.WriteLine("Canonical SignalR stream connected: " + HubUri);
                ControlMessenger.Send("Stream Health Changed", GetHealthSnapshot());
                string[] markets; lock (_handlersLock) markets = new List<string>(_subscribedMarkets).ToArray(); foreach (string market in markets) await InvokeAsync("SubscribeMarket", market);
                await ReceiveLoopAsync(socket, cts.Token);
            }
            catch (OperationCanceledException) when (_stopped) { }
            catch (Exception ex) { Interlocked.Exchange(ref _connected, 0); Interlocked.Exchange(ref _lastErrorTicks, DateTime.UtcNow.Ticks); _lastError = ex.Message; Trace.WriteLine("Canonical SignalR stream error: " + ex); ControlMessenger.Send("Stream Health Changed", GetHealthSnapshot()); }
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
            if (Interlocked.Exchange(ref _connected, 0) != 0)
            {
                Interlocked.Exchange(ref _lastDisconnectedTicks, DateTime.UtcNow.Ticks);
                ControlMessenger.Send("Stream Health Changed", GetHealthSnapshot());
            }
        }
        private void HandleMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || payload == "{}") return;
            try
            {
                Interlocked.Increment(ref _messagesReceived);
                Interlocked.Exchange(ref _lastMessageTicks, DateTime.UtcNow.Ticks);
                var message = JObject.Parse(payload); if ((int?)message["type"] != 1) { Interlocked.Increment(ref _invalidMessages); return; } string target = (string)message["target"];
                if (target == "marketChanged")
                {
                    Interlocked.Increment(ref _marketMessages);
                    var change = message["arguments"]?[0]?.ToObject<MarketChangeDto>();
                    if (change != null && !_stopped && !_marketChangeQueue.TryAdd(change))
                        RecordDroppedMessage("market", ref _droppedMarketMessages);
                }
                else if (target == "ordersChanged" && !_stopped)
                {
                    Interlocked.Increment(ref _orderMessages);
                    var orderJson = (string)message["arguments"]?[0];
                    if (orderJson != null && !_orderQueue.TryAdd(orderJson))
                        RecordDroppedMessage("order", ref _droppedOrderMessages);
                }
            }
            catch (InvalidOperationException) when (_stopped) { }
            catch (Exception ex) { Interlocked.Increment(ref _invalidMessages); Debug.WriteLine("Canonical SignalR message ignored: " + ex.Message); }
        }
        private void RecordDroppedMessage(string kind, ref int counter)
        {
            int dropped = Interlocked.Increment(ref counter);
            if (dropped == 1 || dropped % 100 == 0)
                Trace.WriteLine($"Canonical SignalR {kind} queue full; dropped {dropped} message(s)");
        }
        private async Task InvokeAsync(string target, string marketId)
        {
            ClientWebSocket socket; CancellationToken token; lock (_socketLock) { socket = _socket; token = _socketCts == null ? CancellationToken.None : _socketCts.Token; }
            if (socket == null || socket.State != WebSocketState.Open) return; string json = JsonConvert.SerializeObject(new { type = 1, invocationId = Interlocked.Increment(ref _invocationId).ToString(), target = target, arguments = new[] { marketId } }) + "\u001e"; await SendRawAsync(socket, json, token);
        }
        private async Task SendRawAsync(ClientWebSocket socket, string value, CancellationToken token) { byte[] bytes = Encoding.UTF8.GetBytes(value); await _sendGate.WaitAsync(token); try { await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token); } finally { _sendGate.Release(); } }
        private async Task SubscribeAsync(string marketId) { try { lock (_handlersLock) _subscribedMarkets.Add(marketId); Trace.WriteLine("Canonical SignalR subscribe queued: " + marketId); await InvokeAsync("SubscribeMarket", marketId); } catch (Exception ex) { Trace.WriteLine("Canonical SignalR subscribe failed: " + ex.Message); } }
        private async Task UnsubscribeAsync(string marketId) { try { lock (_handlersLock) _subscribedMarkets.Remove(marketId); await InvokeAsync("UnsubscribeMarket", marketId); } catch (Exception ex) { Trace.WriteLine("Canonical SignalR unsubscribe failed: " + ex.Message); } }
        private void Connect()
        {
            if (Interlocked.Exchange(ref _connectLoopStarted, 1) != 0) return;
            _ = Task.Run(async () =>
            {
                while (!_stopped)
                {
                    await ConnectAsync();
                    if (!_stopped) await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });
        }

        public void Stop()
        {
            if (_stopped) return;
            _stopped = true;
            ControlMessenger.MessageSent -= OnMessageReceived;
            _orderQueue.CompleteAdding();
            _marketChangeQueue.CompleteAdding();
            _healthTimer?.Dispose();
            lock (_socketLock)
            {
                _socketCts?.Cancel();
                _socket?.Abort();
                _socket?.Dispose();
                _socket = null;
                _socketCts = null;
            }
            WriteHealthSnapshot();
        }

        private void WriteHealthSnapshot() => HealthDiagnostics.Write(GetHealthSnapshot());

        internal StreamHealthSnapshot GetHealthSnapshot()
        {
            lock (_handlersLock)
            {
                return new StreamHealthSnapshot
                {
                    GeneratedUtc = DateTime.UtcNow,
                    Connected = Volatile.Read(ref _connected) != 0,
                    HubUri = HubUri.ToString(),
                    LastConnectedUtc = ToUtc(Volatile.Read(ref _lastConnectedTicks)),
                    LastDisconnectedUtc = ToUtc(Volatile.Read(ref _lastDisconnectedTicks)),
                    LastMessageUtc = ToUtc(Volatile.Read(ref _lastMessageTicks)),
                    LastErrorUtc = ToUtc(Volatile.Read(ref _lastErrorTicks)),
                    LastError = _lastError,
                    ConnectAttempts = Volatile.Read(ref _connectAttempts),
                    Reconnects = Volatile.Read(ref _reconnects),
                    MessagesReceived = Volatile.Read(ref _messagesReceived),
                    MarketMessages = Volatile.Read(ref _marketMessages),
                    OrderMessages = Volatile.Read(ref _orderMessages),
                    InvalidMessages = Volatile.Read(ref _invalidMessages),
                    DroppedMarketMessages = Volatile.Read(ref _droppedMarketMessages),
                    DroppedOrderMessages = Volatile.Read(ref _droppedOrderMessages),
                    ActiveSubscriptions = _subscribedMarkets.Count,
                    MarketQueueCount = _marketChangeQueue.Count,
                    OrderQueueCount = _orderQueue.Count
                };
            }
        }

        private static DateTime? ToUtc(long ticks) => ticks == 0 ? (DateTime?)null : new DateTime(ticks, DateTimeKind.Utc);
    }
}
