using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Betfair.ESAClient;
using Betfair.ESAClient.Auth;
using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using System.Diagnostics;
using System.Timers;

namespace SpreadTrader
{
	class StreamingAPI
	{
		public static StreamUpdateDelegate Callback = null;
		private static String ConnectionId { get; set; }
		private static String MarketId { get; set; }
		private static AppKeyAndSessionProvider SessionProvider { get; set; }
		private static ClientCache _clientCache;
		private static string _host = "stream-api.betfair.com";
		private static int _port = 443;

		public List<LiveRunner> LiveRunners { get; set; }
		private static List<LiveRunner> _LiveRunners { get; set; }
		private static Properties.Settings props = Properties.Settings.Default;
		public StreamingAPI()
		{
			NewSessionProvider(
				"identitysso-cert.betfair.com",
				props.AppKey,
				props.BFUser,
				props.BFPassword);

			ClientCache.Client.ConnectionStatusChanged += (o, e) =>
			{
				if (!String.IsNullOrEmpty(e.ConnectionId))
				{
					ConnectionId = e.ConnectionId;
				}
			};
		}
		public void NewSessionProvider(string ssohost, string appkey, string username, string password)
		{
			AppKeyAndSessionProvider sessionProvider = new AppKeyAndSessionProvider(ssohost, appkey, username, password);
			SessionProvider = sessionProvider;
		}
		public ClientCache ClientCache
		{
			get
			{
				if (_clientCache == null)
				{
					Client client = new Client(_host, _port, SessionProvider);
					_clientCache = new ClientCache(client);
					_clientCache.MarketCache.MarketChanged += OnMarketChanged;
					_clientCache.OrderCache.OrderMarketChanged += OnOrderChanged;

				}
				return _clientCache;
			}
		}
		private static Tuple<Double, Double> LevelProfit(LiveRunner runner1, LiveRunner runner2)
		{
			Double G3 = runner1.ifWin;
			Double J3 = runner2.ifWin;

			Double G5 = G3 > 0 ? runner1.LayValues[0].price : runner1.BackValues[0].price;
			Double J5 = J3 > 0 ? runner2.LayValues[0].price : runner2.BackValues[0].price;

			Double D8 = (G3 - J3) / G5;
			Double J8 = (G3 - J3) / J5;

			Double G10 = G3 - (D8 * (G5 - 1));
			Double G13 = (G3 - J3) / J5;

			Double G18 = G3 - (D8 * (G5 - 1));
			Double J18 = J3 + (J8 * (J5 - 1));

			return new Tuple<double, double>(G18, J18);
		}
		private static Tuple<Double, Double, Double> LevelProfit(LiveRunner runner1, LiveRunner runner2, LiveRunner draw)
		{
			Double D5 = runner1.ifWin;
			Double E5 = runner2.ifWin;
			Double F5 = draw.ifWin;

			Double D8 = D5 > 0 ? runner1.LayValues[0].price : runner1.BackValues[0].price;
			Double E8 = E5 > 0 ? runner2.LayValues[0].price : runner2.BackValues[0].price;
			Double F8 = F5 > 0 ? draw.LayValues[0].price : draw.BackValues[0].price;

			Double F11 = (F5 - D5) / D8;
			Double F12 = (F5 - E5) / E8;

			Double D15 = F11 > 0 ? F11 * (D8 - 1) + D5 : F11 * (D8 - 1) + D5;
			Double E15 = F11 > 0 ? E5 - F11 : -F11 + E5;
			Double F15 = F11 > 0 ? -F11 + F5 : -F11 + F5;

			Double D16 = F12 > 0 ? D15 - F12 : -F12 + D15;
			Double E16 = F12 > 0 ? F12 * (E8 - 1) + E15 : F12 * (E8 - 1) + E15;
			Double F16 = F12 > 0 ? F15 - F12 : -F12 + F15;

			return new Tuple<double, double, double>(D16, E16, F16);
		}
		private static void OnMarketChanged(object sender, MarketChangedEventArgs e)
		{
			try
			{
				double tradedVolume = 0;
				_LiveRunners = new List<LiveRunner>();
				for (int i = 0; i < e.Snap.MarketRunners.Count; i++)
				{
					LiveRunner lr = new LiveRunner();
					lr.SetPrices(e.Snap.MarketRunners[i]);
					_LiveRunners.Add(lr);
					tradedVolume += e.Snap.MarketRunners[i].Prices.TradedVolume;
				}
				if (_LiveRunners.Count == 2)
				{
					Tuple<double, double> s2 = LevelProfit(_LiveRunners[0], _LiveRunners[1]);
					_LiveRunners[0].LevelProfit = s2.Item1;
					_LiveRunners[1].LevelProfit = s2.Item2;
				}
				else if (_LiveRunners.Count == 3)
				{
					Tuple<double, double, double> s2 = LevelProfit(_LiveRunners[0], _LiveRunners[1], _LiveRunners[2]);
					_LiveRunners[0].LevelProfit = s2.Item1;
					_LiveRunners[1].LevelProfit = s2.Item2;
					_LiveRunners[2].LevelProfit = s2.Item3;
				}
				Callback?.Invoke(e.Snap.MarketId, _LiveRunners, tradedVolume, !e.Market.IsClosed && e.Snap.MarketDefinition.InPlay==true);
			}
			catch (Exception xe)
			{
			}
		}
		private static void OnOrderChanged(object sender, OrderMarketChangedEventArgs e)
		{
			try
			{
			}
			catch (Exception xe)
			{
			}
		}
		public void Start(String marketID)
		{
			if (MarketId == marketID && ClientCache.Status == Betfair.ESAClient.Protocol.ConnectionStatus.SUBSCRIBED)
			{
				Debug.WriteLine("Already subscribed");
			}
			if (ClientCache.Status == Betfair.ESAClient.Protocol.ConnectionStatus.SUBSCRIBED)
			{
				ClientCache.Stop();
			}
			Console.WriteLine("Start " + MarketId);
			MarketFilter f = new MarketFilter()
			{
				//CountryCodes = new List<string>() { "GB" },
				BettingTypes = new List<MarketFilter.BettingTypesEnum?>() { MarketFilter.BettingTypesEnum.Odds },
				//MarketTypes = new List<string>() { "WIN" },
				//EventTypeIds = new List<string>() { "7" },
				MarketIds = new List<string>() { marketID }
			};

			MarketDataFilter mdf = new MarketDataFilter();
			mdf.LadderLevels = 3;
			mdf.Fields = new List<MarketDataFilter.FieldsEnum?>();
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExBestOffers);
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExTradedVol);
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExLtp);
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExMarketDef);

			MarketSubscriptionMessage msm = new MarketSubscriptionMessage()
			{
				MarketDataFilter = mdf,
				MarketFilter = f,
				SegmentationEnabled = true
			};
			ClientCache.SubscribeMarkets(msm);

			OrderSubscriptionMessage osm = new OrderSubscriptionMessage()
			{
				SegmentationEnabled = true
			};
			ClientCache.SubscribeOrders(osm);
		}
		public void Stop()
		{
			ClientCache.Stop();
		}
	}
}
