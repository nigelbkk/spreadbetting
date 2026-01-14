using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Betfair.ESASwagger.Model;

namespace SpreadTrader
{
	internal class WebSocketsHub
	{
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private readonly BlockingCollection<string> _orderQueue = new BlockingCollection<string>();
		private readonly BlockingCollection<MarketChangeDto> _marketChangeQueue = new BlockingCollection<MarketChangeDto>();
		private Task _orderProcessor;
		private Task _marketChangeProcessor;
		private Properties.Settings props = Properties.Settings.Default;

		private void OrderProcessingLoop()
		{
			foreach (var json in _orderQueue?.GetConsumingEnumerable())
			{
				ControlMessenger.Send("Orders Changed", new { String = json });
			}
		}
		private void MarketChangerProcessingLoop()
		{
			foreach (var change in _marketChangeQueue?.GetConsumingEnumerable())
			{
				ControlMessenger.Send("Market Changed", new { MarketChangeDto = change });
			}
		}
		public void Start()
		{
			_orderProcessor = Task.Run(() => OrderProcessingLoop());
			_marketChangeProcessor = Task.Run(() => MarketChangerProcessingLoop());
		}
		public WebSocketsHub() 
		{
			ControlMessenger.MessageSent += OnMessageReceived;
			hubConnection = new HubConnection("http://" + props.WebSocketsUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubConnection.Closed += () =>
			{
				Debug.WriteLine($"SignalR Closed...");
			};

			hubConnection.Error += (err) =>
			{
				Debug.WriteLine($"SignalR Error...{err.Message}");
			};

			hubConnection.ConnectionSlow += () =>
			{
				Debug.WriteLine($"SignalR ConnectionSlow...");
			};

			hubConnection.StateChanged += (state) =>
			{
				Debug.WriteLine($"SignalR StateChanged...{state.OldState} => {state.NewState} ");
			};

			hubConnection.Reconnecting += () =>
			{
				Debug.WriteLine("SignalR reconnecting...");
			};
			hubConnection.Reconnected += () =>
			{
				Debug.WriteLine("SignalR reconnected...");
			};

			Start();

			hubProxy.On<MarketChangeDto>("marketChanged", (change) =>
			{
				_marketChangeQueue.Add(change);
			});
			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				_orderQueue.Add(json1);
			});
			Connect();
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Selected")
			{
				dynamic d = data;
				NodeViewModel d2 = d.NodeViewModel;
				String marketId = d2.MarketID;
				Debug.WriteLine($"WebSocketsHub: {messageName} : {d2.FullName}");
				RequestMarketSelectedAsync(d2.MarketID);
			}
			if (messageName == "Reconnect")
			{
				dynamic d = data;
				String marketId = d.MarketId;
				Connect();
				RequestMarketSelectedAsync(marketId);
			}
			if (messageName == "Unsubscribe")
			{
				dynamic d = data;
				String marketId = d.MarketId;
				UnsubscribeAsync(marketId);
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
		private async void RequestMarketSelectedAsync(String marketid)
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
		private void Closed()
		{

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