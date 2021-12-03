using Betfair.ESAClient;
using Betfair.ESAClient.Auth;
using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
				Callback?.Invoke(e.Snap.MarketId, _LiveRunners, tradedVolume, !e.Market.IsClosed && e.Snap.MarketDefinition.InPlay == true);
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private static void OnOrderChanged(object sender, OrderMarketChangedEventArgs e)
		{
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
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExBestOffersDisp);
			mdf.Fields.Add(MarketDataFilter.FieldsEnum.ExAllOffers);
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
