using Betfair.ESASwagger.Model;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpreadTrader
{
	internal class WebSocketsHub
	{
		public static WebSocketsHub Instance { get; } = new WebSocketsHub();
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private readonly BlockingCollection<string> _orderQueue = new BlockingCollection<string>(10000);
		private readonly BlockingCollection<MarketChangeDto> _marketChangeQueue = new BlockingCollection<MarketChangeDto>();
		private Task _orderProcessor;
		private Task _marketChangeProcessor;
		private Properties.Settings props = Properties.Settings.Default;
		private readonly Dictionary<string, BetsManager> _ordersHandlers = new Dictionary<string, BetsManager>();
		private readonly Dictionary<string, RunnersControl> _marketHandlers = new Dictionary<string, RunnersControl>();

		public void Attach(string marketId, BetsManager manager)
		{
			if (_ordersHandlers.TryGetValue(marketId, out var existing))
			{
				if (!ReferenceEquals(existing, manager))
				{
					Debug.WriteLine($"Handler already registered for market {marketId}. " + $"Existing: {existing.GetHashCode()}, New: {manager.GetHashCode()}");
				}
				// already registered, no-op
				return;
			}
			_ordersHandlers[marketId] = manager;
		}
		public void Detach(string marketId, BetsManager manager)
		{
			if (_ordersHandlers.TryGetValue(marketId, out var existing) && existing == manager)
			{
				_ordersHandlers.Remove(marketId);
			}
		}
		public void Attach(string marketId, RunnersControl manager)
		{
			if (_marketHandlers.TryGetValue(marketId, out var existing))
			{
				if (!ReferenceEquals(existing, manager))
				{
					Debug.WriteLine( $"Handler already registered for market {marketId}. " + $"Existing: {existing.GetHashCode()}, New: {manager.GetHashCode()}");
				}
				// already registered, no-op
				return;
			}
			_marketHandlers[marketId] = manager;
			SubscribeAsync(marketId);
		}
		public void Detach(string marketId, RunnersControl manager)
		{
			if (_marketHandlers.TryGetValue(marketId, out var existing) && existing == manager)
			{
				_marketHandlers.Remove(marketId);
				UnsubscribeAsync(marketId);
			}
		}
		private void OrderProcessingLoop()
		{
			foreach (var json in _orderQueue.GetConsumingEnumerable())
			{
				Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] Dequeue. Queue={_orderQueue.Count}");

				if (props.RecordOrders)
					StreamRecorder.Record(json);

				var change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

				if (change?.Id == null)
					continue;

				if (_ordersHandlers.TryGetValue(change.Id, out var manager))
				{
					var sw = Stopwatch.StartNew();

					manager.OnOrderChanged(change);

					sw.Stop();

					if (sw.ElapsedMilliseconds > 50)
					{
						Debug.WriteLine($"SLOW OnOrderChanged {change.Id}: {sw.ElapsedMilliseconds} ms");
					}

					//Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] Before OnOrderChanged {change.Id}");
					//manager.OnOrderChanged(change);
					//Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] After OnOrderChanged {change.Id}");
				}
				else
				{
					Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} No handler for {change.Id}");
				}
			}
		}
		//private void OrderProcessingLoop()
		//{
		//	foreach (var json in _orderQueue.GetConsumingEnumerable())
		//	{
		//		if (props.RecordOrders)
		//			StreamRecorder.Record(json);

		//		var change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

		//		if (change?.Id == null)
		//			continue;

		//		if (_ordersHandlers.TryGetValue(change.Id, out var manager))
		//		{
		//			manager.OnOrderChanged(change); // better: pass object, not json
		//		}
		//	}
		//}
		private void MarketChangeProcessingLoop()
		{
			foreach (var change in _marketChangeQueue?.GetConsumingEnumerable())
			{
				var latest = change;

				// 🔥 Drain queue — keep only most recent
				while (_marketChangeQueue.TryTake(out var next))
				{
					latest = next;
				}

				if (_marketHandlers.TryGetValue(latest.MarketId, out var manager))
				{
					manager.OnMarketChanged(latest);
				}
				//if (_marketHandlers.TryGetValue(change.MarketId, out var manager))
				//{
				//	manager.OnMarketChanged(change); // better: pass object, not json
				//}
			}
		}
		public void Start()
		{
			_orderProcessor = Task.Run(() => OrderProcessingLoop());
			_marketChangeProcessor = Task.Run(() => MarketChangeProcessingLoop());
		}
		private WebSocketsHub() 
		{
			ControlMessenger.MessageSent += OnMessageReceived;
			hubConnection = new HubConnection("http://" + props.WebSocketsUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubConnection.Closed += () => { Debug.WriteLine($"SignalR Closed..."); }; 
			hubConnection.Error += (err) => { Debug.WriteLine($"SignalR Error...{err.Message}"); }; 
			hubConnection.ConnectionSlow += () => { Debug.WriteLine($"SignalR ConnectionSlow..."); }; 
			hubConnection.StateChanged += (state) => { Debug.WriteLine($"SignalR StateChanged...{state.OldState} => {state.NewState} "); }; 
			hubConnection.Reconnecting += () => { Debug.WriteLine("SignalR reconnecting..."); };
			hubConnection.Reconnected += () => { Debug.WriteLine("SignalR reconnected..."); };

			Start();

			hubProxy.On<MarketChangeDto>("marketChanged", (change) =>
			{
				_marketChangeQueue.Add(change);
			});
			hubProxy.On<string, string>("ordersChanged", (json1, json3) =>
			{
				_orderQueue.Add(json1);
			});
			Connect();
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Reconnect")
			{
				dynamic d = data;
				String marketId = d.MarketId;
				Connect();
				SubscribeAsync(marketId);
			}
		}
		private async void UnsubscribeAsync(String marketId)
		{
			var http = new HttpClient();
			var url = $"http://{props.WebSocketsUrl}/api/market/unsubscribe";
			var payload = new { MarketId = marketId, };
			string json = JsonConvert.SerializeObject(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await http.PostAsync(url, content);
			string responseString = await response.Content.ReadAsStringAsync();
			Console.WriteLine(responseString);
		}
		private async void SubscribeAsync(String marketid)
		{
			var http = new HttpClient();
			var url = $"http://{props.WebSocketsUrl}/api/market/subscribe";
			var payload = new { MarketId = marketid, };
			string json = JsonConvert.SerializeObject(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await http.PostAsync(url, content);
			string responseString = await response.Content.ReadAsStringAsync();
			Console.WriteLine(responseString);
		}

		private void Connect()
		{
			if (hubConnection != null)
			{
				hubConnection.Start().ContinueWith(task =>
				{
					if (OnFail(task))
					{
						//result = "Failed to Connect";
						return;
					}
					//result = "Connected";
				}).Wait(1000);
			}
		}
		private void Disconnect()
		{
			hubConnection.Stop(new TimeSpan(2000));
		}
		private bool OnFail(Task task)
		{
			if (task.IsFaulted)
			{
				Debug.WriteLine("Exception:{0}", task.Exception.GetBaseException());
			}
			return task.IsFaulted;
		}
	}
}