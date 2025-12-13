using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
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
		private Properties.Settings props = Properties.Settings.Default;

		public WebSocketsHub() 
		{
			ControlMessenger.MessageSent += OnMessageReceived;
			hubConnection = new HubConnection("http://" + props.StreamUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubProxy.On<MarketChange, MarketSnapDto>("marketChanged", (mc, snap) =>
			{
				//Debug.WriteLine("Hub marketChanged");
				//MarketChangedEventArgs

				//MarketChange change = JsonConvert.DeserializeObject<MarketChange>(json1);
				//Betfair.ESAClient.Cache.Market market = JsonConvert.DeserializeObject<Betfair.ESAClient.Cache.Market>(json2);
				//MarketSnap snapshot = JsonConvert.DeserializeObject<MarketSnap>(json3);

				ControlMessenger.Send("Market Changed", new { MarketChange = mc, MarketSnapDto = snap});
				//Debug.WriteLine(snap.Runners[0].Prices.Back[0].Price);
			});
			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json1);
				OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
				//if (MarketNode != null && snapshot.MarketId == MarketNode.MarketID)
				//{
				//	lock (incomingOrdersQueue)
				//	{
				//		Debug.WriteLine("Add to queue");
				//		incomingOrdersQueue.Enqueue(json1);
				//	}
				//}
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
				//				MarketNode = d.MarketNode;
				RequestMarketSelectedAsync(d2.MarketID);
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