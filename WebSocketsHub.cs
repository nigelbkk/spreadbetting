using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SpreadTrader
{
	internal class WebSocketsHub
	{
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private readonly BlockingCollection<string> _orderQueue = new BlockingCollection<string>();
		private readonly BlockingCollection<MarketSnapDto> _marketChangeQueue = new BlockingCollection<MarketSnapDto>();
		private object lockObj1 = new object();
		private object lockObj2 = new object();
		private Task _orderProcessor;
		private Task _marketChangeProcessor;
		private Properties.Settings props = Properties.Settings.Default;

		private void OrderProcessingLoop()
		{
			foreach (var json in _orderQueue?.GetConsumingEnumerable())
			{
				lock (lockObj1)
				{
					ControlMessenger.Send("Orders Changed", new { String = json });
					//ProcessOrder(json);
				}
			}
		}
		private void MarketChangerProcessingLoop()
		{
			foreach (var snap in _marketChangeQueue?.GetConsumingEnumerable())
			{
				lock (lockObj2)
				{
					ControlMessenger.Send("Market Changed", new { MarketSnapDto = snap});
				}
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
			hubConnection = new HubConnection("http://" + props.StreamUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			Start();

			hubProxy.On<MarketSnapDto>("marketChanged", (snap) =>
			{
				_marketChangeQueue.Add(snap);
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
			//if (messageName == "Tab Selected")
			//{
			//	dynamic d = data;
			//	Debug.WriteLine($"WebSocketsHub: {messageName} :{d.MarketId}");
			//	//RequestMarketSelectedAsync(d.MarketId);
			//}
			if (messageName == "Reconnect requested")
			{
				Connect();
			}
		}
		private async void RequestMarketSelectedAsync(String marketid)
		{
			var http = new HttpClient();
			var url = $"http://{props.StreamUrl}/api/market/subscribe";
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