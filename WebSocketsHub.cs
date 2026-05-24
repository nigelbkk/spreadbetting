using Betfair.ESASwagger.Model;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using SpreadTrader.Simulator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
		private readonly object _handlersLock = new object();
		private readonly Dictionary<string, List<BetsManager>> _ordersHandlers = new Dictionary<string, List<BetsManager>>();
		private readonly Dictionary<string, List<RunnersControl>> _marketHandlers = new Dictionary<string, List<RunnersControl>>();

		public void Attach(string marketId, BetsManager manager)
		{
			if (string.IsNullOrEmpty(marketId) || manager == null)
				return;

			lock (_handlersLock)
			{
				if (!_ordersHandlers.TryGetValue(marketId, out var handlers))
				{
					handlers = new List<BetsManager>();
					_ordersHandlers[marketId] = handlers;
				}

				if (!handlers.Contains(manager))
					handlers.Add(manager);
			}
		}
		public void Detach(string marketId, BetsManager manager)
		{
			if (string.IsNullOrEmpty(marketId) || manager == null)
				return;

			lock (_handlersLock)
			{
				if (_ordersHandlers.TryGetValue(marketId, out var handlers))
				{
					handlers.Remove(manager);
					if (handlers.Count == 0)
						_ordersHandlers.Remove(marketId);
				}
			}
		}		
		public void Attach(string marketId, RunnersControl manager)
		{
			if (string.IsNullOrEmpty(marketId) || manager == null)
				return;

			bool shouldSubscribe = false;

			lock (_handlersLock)
			{
				if (!_marketHandlers.TryGetValue(marketId, out var handlers))
				{
					handlers = new List<RunnersControl>();
					_marketHandlers[marketId] = handlers;
					shouldSubscribe = true;
				}

				if (!handlers.Contains(manager))
					handlers.Add(manager);
			}

			if (shouldSubscribe)
				_ = SubscribeAsync(marketId);
		}
		public void Detach(string marketId, RunnersControl manager)
		{
			if (string.IsNullOrEmpty(marketId) || manager == null)
				return;

			bool shouldUnsubscribe = false;

			lock (_handlersLock)
			{
				if (_marketHandlers.TryGetValue(marketId, out var handlers))
				{
					handlers.Remove(manager);
					if (handlers.Count == 0)
					{
						_marketHandlers.Remove(marketId);
						shouldUnsubscribe = true;
					}
				}
			}

			if (shouldUnsubscribe)
				_ = UnsubscribeAsync(marketId);
		}

		private List<BetsManager> GetOrderHandlers(string marketId)
		{
			lock (_handlersLock)
			{
				return _ordersHandlers.TryGetValue(marketId, out var handlers) ? new List<BetsManager>(handlers) : new List<BetsManager>();
			}
		}

		private List<RunnersControl> GetMarketHandlers(string marketId)
		{
			lock (_handlersLock)
			{
				return _marketHandlers.TryGetValue(marketId, out var handlers) ? new List<RunnersControl>(handlers) : new List<RunnersControl>();
			}
		}

		private void OrderProcessingLoop()
		{
			foreach (var json in _orderQueue.GetConsumingEnumerable())
			{
				Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] Dequeue. Queue={_orderQueue.Count}");
				var change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

				if (change?.Id == null)
					continue;

				var managers = GetOrderHandlers(change.Id);
				if (managers.Count > 0)
				{
					foreach (var manager in managers)
					{
						var sw = Stopwatch.StartNew();

						Debug.WriteLine( $"[LOOP INJECT] T={Thread.CurrentThread.ManagedThreadId} " + $"UI={Application.Current.Dispatcher.CheckAccess()}");
						manager.OnOrderChanged(change);
						ControlMessenger.Send("");

						sw.Stop();

						if (sw.ElapsedMilliseconds > 50)
						{
							Debug.WriteLine($"SLOW OnOrderChanged {change.Id}: {sw.ElapsedMilliseconds} ms");
						}
					}
				}
				else
				{
					Debug.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} No handler for {change.Id}");
				}
			}
		}
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

				foreach (var manager in GetMarketHandlers(latest.MarketId))
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
			hubProxy.On<string>("ordersChanged", (json1) =>
			{
				Interlocked.Increment(ref OcmDiagnostics.MessagesReceived);
				Debug.WriteLine($"[HUB INJECT] T={Thread.CurrentThread.ManagedThreadId} " + $"UI={Application.Current.Dispatcher.CheckAccess()}");

				_orderQueue.Add(json1);
			});
			Connect();
		}
		public void Simulate(OrderMarketChange change)
		{
			foreach (var manager in GetOrderHandlers(change.Id))
			{
				var sw = Stopwatch.StartNew();
				Debug.WriteLine(
				  $"[SIM INJECT] T={Thread.CurrentThread.ManagedThreadId} " +
				  $"UI={Application.Current.Dispatcher.CheckAccess()}"); 
				  
				  manager.OnOrderChanged(change);
			}
		}

		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Reconnect")
			{
				dynamic d = data;
				String marketId = d.MarketId;
				Connect();
				_ = SubscribeAsync(marketId);
			}
		}
		private async Task UnsubscribeAsync(String marketId)
		{
			try
			{
				using (var http = new HttpClient())
				{
					var url = $"http://{props.WebSocketsUrl}/api/market/unsubscribe";
					var payload = new { MarketId = marketId, };
					string json = JsonConvert.SerializeObject(payload);
					var content = new StringContent(json, Encoding.UTF8, "application/json");

					HttpResponseMessage response = await http.PostAsync(url, content);
					string responseString = await response.Content.ReadAsStringAsync();
					Console.WriteLine(responseString);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Unsubscribe failed for {marketId}: {ex}");
			}
		}
		private async Task SubscribeAsync(String marketid)
		{
			try
			{
				using (var http = new HttpClient())
				{
					var url = $"http://{props.WebSocketsUrl}/api/market/subscribe";
					var payload = new { MarketId = marketid, };
					string json = JsonConvert.SerializeObject(payload);
					var content = new StringContent(json, Encoding.UTF8, "application/json");

					HttpResponseMessage response = await http.PostAsync(url, content);
					string responseString = await response.Content.ReadAsStringAsync();
					Console.WriteLine(responseString);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Subscribe failed for {marketid}: {ex}");
			}
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
