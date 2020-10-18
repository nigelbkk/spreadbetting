using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BetfairAPI
{
    public class BetfairAPI
    {
        public DateTime PlayBackDate {get;set;}
        private const String AppName = "Ivy";
        private String AppKey { get; set; }  //"uCmjo7RlVUJRCc0T";
        private String Token { get; set; }
        public String SessionToken { get { return Token; } }
        public BetfairAPI()
        {
        }
        public DateTime sysTime = DateTime.UtcNow;
        public static double ticksize(double odds)
        {
            if (odds < 2) return 0.01;
            if (odds < 3) return 0.02;
            if (odds < 4) return 0.05;
            if (odds < 6) return 0.1;
            if (odds < 10) return 0.2;
            if (odds < 20) return 0.5;
            if (odds < 30) return 1;
            if (odds < 50) return 2;
            if (odds < 100) return 5;
            return 10;
        }
        public static double snapTo(double _odds)
        {
            Double odds = Convert.ToDouble(_odds);
            Double tx = Convert.ToDouble(ticksize(_odds)) * 100;

            odds -= ((odds * 100) % tx) / 100;
            return Convert.ToDouble(odds.ToString("F"));
        }
        private String RPCRequestRaw(String Method, Dictionary<String, Object> Params)
        {
            Dictionary<String, Object> joe = new Dictionary<string, object>();
            joe["jsonrpc"] = "2.0";
            joe["id"] = "1";
            joe["method"] = "SportsAPING/v1.0/" + Method;
            joe["params"] = Params;

            String postData = "[" + JsonConvert.SerializeObject(joe) + "]";

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://api.betfair.com/exchange/betting/json-rpc/v1/");
            wr.Method = WebRequestMethods.Http.Post;
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            wr.Headers.Add("X-Application", AppKey);
            wr.Headers.Add("X-Authentication", Token);
            wr.Headers.Add(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8"); wr.Accept = "*/*";

            var bytes = Encoding.GetEncoding("UTF-8").GetBytes(postData);
            wr.ContentType = "application/json";
            wr.ContentLength = bytes.Length;

            using (Stream stream = wr.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse response = wr.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        private Object RPCRequestFile<T>(String Method, Dictionary<String, Object> Params)
        {
            string jsonResponse = System.IO.File.ReadAllText(String.Format(@"C:\development\data\{0}.json", Method));
            var err = JArray.Parse(jsonResponse)[0].SelectToken("error");
            if (err != null)
            {
                ErrorResponse oo = JsonConvert.DeserializeObject<ErrorResponse>(err.ToString());
                throw new Exception(oo.ToString(), new Exception(oo.data.exception.errorCode));
            }
            String res = JArray.Parse(jsonResponse)[0].SelectToken("result").ToString();
            return JsonConvert.DeserializeObject<T>(res);
        }
        private Object RPCRequest<T>(String Method, Dictionary<String, Object> Params)
        {
            String[] AccountCalls = new String[] { "getAccountFunds" };
            Dictionary<String, Object> joe = new Dictionary<string, object>();
            joe["jsonrpc"] = "2.0";
            joe["id"] = "1";
            joe["method"] = "SportsAPING/v1.0/" + Method;
            joe["params"] = Params;

            String url = "https://api.betfair.com/exchange/betting/json-rpc/v1/";    //URL(Params);

            if (AccountCalls.Contains(Method))
            {
                url = "https://api.betfair.com/exchange/account/json-rpc/v1/";
                joe["method"] = "AccountAPING/v1.0/" + Method;
            }
            String postData = "[" + JsonConvert.SerializeObject(joe) + "]";
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.Method = WebRequestMethods.Http.Post;
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            wr.Headers.Add("X-Application", AppKey);
            wr.Headers.Add("X-Authentication", Token);
            wr.Headers.Add(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8"); wr.Accept = "*/*";

            var bytes = Encoding.GetEncoding("UTF-8").GetBytes(postData);
            wr.ContentType = "application/json";
            wr.ContentLength = bytes.Length;

            using (Stream stream = wr.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse response = wr.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
               var jsonResponse = reader.ReadToEnd();
               var err = JArray.Parse(jsonResponse)[0].SelectToken("error");
               if (err != null)
                {
                    ErrorResponse oo = JsonConvert.DeserializeObject<ErrorResponse>(err.ToString());
                    throw new Exception(ErrorCodes.FaultCode(oo.message), new Exception(oo.message));
                }
                String res = JArray.Parse(jsonResponse)[0].SelectToken("result").ToString();
                return JsonConvert.DeserializeObject<T>(res);
            }
        }
        public List<T> FromString<T>(String jsonResponse)
        {
            var err = JArray.Parse(jsonResponse)[0].SelectToken("error");
            if (err != null)
            {
                ErrorResponse oo = JsonConvert.DeserializeObject<ErrorResponse>(err.ToString());
                throw new Exception(oo.ToString(), new Exception(oo.data.exception.errorCode));
            }
            String res = JArray.Parse(jsonResponse)[0].SelectToken("result").ToString();
            return JsonConvert.DeserializeObject<List<T>>(res);
        }
        public Dictionary<String, Object> TimeRange(DateTime from, Int32 days, Int32 hours)
        {
            Dictionary<String, Object> start = new Dictionary<string, object>();
            start["from"] = from;
            start["to"] = from + new TimeSpan(days, hours, 0, 0);
            return start;
        }
        private HashSet<String> GetHashSet<T>(UInt32 arg)
        {
            HashSet<String> set = new HashSet<string>();
            var values = Enum.GetValues(typeof(T));
            for (UInt32 i = 0, j = 1; i < values.Length; i++, j = j << 1)
            {
                if ((arg & j) != 0)
                {
                    set.Add(values.GetValue(i).ToString());
                }
            }
            return set;
        }
        public void SetToken(String Token)
        {
            this.Token = Token;
        }
        public void login(String CertFile, String CertPassword, String AppKey, String username, String password)
        {
            return; ///NH
            this.AppKey = AppKey;   
            Token = String.Empty;
            byte[] args = Encoding.UTF8.GetBytes(String.Format("username={0}&password={1}", username, password));

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(" https://identitysso-cert.betfair.com/api/certlogin");
			wr.Method = WebRequestMethods.Http.Post;
            wr.Headers.Add("X-Application", AppKey);
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.ContentLength = args.Length;

            var cert = new X509Certificate2(CertFile, CertPassword); 
            wr.ClientCertificates.Add(cert);

            using (Stream newStream = wr.GetRequestStream())
            {
                newStream.Write(args, 0, args.Length);
                newStream.Close();
            }
            using (WebResponse response = wr.GetResponse())
            {
                sysTime = DateTime.Parse(response.Headers["Date"]).ToUniversalTime();
                using (Stream ds = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(ds);
                    String rs = reader.ReadToEnd();

                    LoginResponse o = JsonConvert.DeserializeObject<LoginResponse>(rs);
                    if (!o.Status)
                    {
                        Console.WriteLine("Login failed. Are you running Fiddler?");
                        throw new Exception(o.ToString());
                    }
                    Token = o.sessionToken;
                }
            }
        }
        public List<VenueResult> ListVenues()
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["exchangeIds"] = new Int32[] { 1 };
            filter["eventTypeIds"] = new Int32[] { 7 };
            filter["marketCountries"] = new String[] { "GB", "IE" };
            p["filter"] = filter;
            return RPCRequest <List<VenueResult>>("listVenues", p) as List<VenueResult>;
        }
        public List<EventTypeResult> GetEventTypes() 
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> start = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            start["from"] = DateTime.UtcNow;
            start["to"] = DateTime.UtcNow + new TimeSpan(24, 0, 0);
            filter["exchangeIds"] = new Int32[] { 1 };
            //filter["eventTypeIds"] = new Int32[] { 7 };
            filter["marketCountries"] = new String[] { "GB", "USA" };
            p["filter"] = filter;
//            return RPCRequest<List<EventTypeResult>>("listEventTypes", p) as List<EventTypeResult>;
            return RPCRequestFile<List<EventTypeResult>>("listEventTypes", p) as List<EventTypeResult>;
        }
        public List<Event> GetEvents(Int32 competitionId)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["competitionIds"] = new Int32[] { competitionId };
            p["filter"] = filter;
//            return RPCRequest<List<Event>>("listEvents", p) as List<Event>;
            return RPCRequestFile<List<Event>>("listEvents", p) as List<Event>;
        }
        public List<CompetitionResult> GetCompetitions(Int32 event_type)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { event_type };
            p["filter"] = filter;
            //            return RPCRequest<List<CompetitionResult>>("listCompetitions", p) as List<Event>;
            return RPCRequestFile<List<CompetitionResult>>("listCompetitions", p) as List<CompetitionResult>;
        }
        public List<Market> GetMarkets(Int32 event_id)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventIds"] = new Int32[] { event_id };
            p["filter"] = filter;
            p["marketProjection"] = GetHashSet<marketProjectionEnum>((uint) marketProjectionEnum.EVENT);

            return RPCRequestFile<List<Market>>("listMarketCatalogue", p) as List<Market>;
//            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        public List<Market> GetMarkets(Int32 exchange, Int32 sportid, Int32 days, marketTypeEnum marketTypes, String[] Countries, marketProjectionEnum Projection)
        {
            HashSet<String> marketProjection = GetHashSet<marketProjectionEnum>((uint)Projection);
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["exchangeIds"] = new Int32[] { exchange };
            filter["eventTypeIds"] = new Int32[] { sportid };
            filter["marketCountries"] = Countries;
            filter["marketStartTime"] = TimeRange(DateTime.UtcNow.Date, days, 0);
            //            filter["marketStartTime"] = TimeRange(DateTime.UtcNow.Date - new TimeSpan(1,0,0,0), 0, 24);
            //          filter["venues"] = new String[] { "Bath" };
            filter["marketTypeCodes"] = GetHashSet<marketTypeEnum>((uint)marketTypes);

            p["filter"] = filter;
            p["sort"] = "FIRST_TO_START";
            p["marketProjection"] = GetHashSet<marketProjectionEnum>((uint)Projection);
            p["maxResults"] = 200;

            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        public List<Market> GetMarkets(Dictionary<String, Object> filter, marketProjectionEnum Projection)
        {
            HashSet<String> marketProjection = GetHashSet<marketProjectionEnum>((uint)Projection);
            Dictionary<String, Object> p = new Dictionary<string, object>();
            //Dictionary<String, Object> filter = new Dictionary<string, object>();

            //filter["exchangeIds"] = new Int32[] { exchange };
            //filter["eventTypeIds"] = new Int32[] { sportid };
            //filter["marketCountries"] = new String[] { "GB" };
            //filter["marketStartTime"] = TimeRange(DateTime.UtcNow.Date, 0, 24);
            ////          filter["venues"] = new String[] { "Bath" };
            //filter["marketTypeCodes"] = GetHashSet<marketTypeEnum>((uint)marketTypes);

            p["filter"] = filter;
            p["marketProjection"] = GetHashSet<marketProjectionEnum>((uint)Projection);
            p["maxResults"] = 1000;

            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        public List<Market> GetMarket(String marketId, marketProjectionEnum Projection)
        {
            HashSet<String> marketProjection = GetHashSet<marketProjectionEnum>((uint)Projection);
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["marketIds"] = new String[] { marketId };
            p["filter"] = filter;
//            p["exchangeIds"] = new Int32[] { Int32.Parse(marketId.Split('.')[0]) };
            p["marketProjection"] = GetHashSet<marketProjectionEnum>((uint)Projection);   
            p["maxResults"] = 100;
            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        private void CalculateProfitAndLoss(MarketBook book)
        {
            CurrentOrderSummaryReport Orders = listCurrentOrders(book.marketId);
            foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in Orders.currentOrders)
            {
                foreach (Runner r in book.Runners)
                {
                    if (r.selectionId == o.selectionId)
                    {
                        if (o.side == "BACK")
                        {
                            r.ifWin += Convert.ToDouble(o.sizeMatched * (o.averagePriceMatched - 1.0));
                        }
                        else
                        {
                            r.ifWin += Convert.ToDouble(o.sizeMatched);
                        }
                    }
                    else
                    {
                        if (o.side == "BACK")
                        {
                            r.ifWin -= Convert.ToDouble(o.sizeMatched);
                        }
                        else
                        {
                            r.ifWin -= Convert.ToDouble(o.sizeMatched * (o.averagePriceMatched - 1.0));
                        }
                    }
                }
            }
        }
        public MarketBook GetMarketBookNoPrices(Market m)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketIds"] = new String[] { m.marketId };
            List<MarketBook> books = RPCRequest<List<MarketBook>>("listMarketBook", p) as List<MarketBook>;
            return books.First();
        }
        public MarketBook GetMarketBook(Market m)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketIds"] = new String[] { m.marketId };
            p["priceProjection"] = new priceProjection()
            {
                priceData = GetHashSet<priceDataEnum>((uint)(priceDataEnum.SP_AVAILABLE | priceDataEnum.EX_BEST_OFFERS))
            };
            List<MarketBook> books = RPCRequestFile<List<MarketBook>>("listMarketBook", p) as List<MarketBook>;
            //CalculateProfitAndLoss(books.First());
            MarketBook book = books.First();

            if (m.runners == null)
            {
                m.runners = new List<Market.RunnerCatalog>();
                foreach (Runner runner in book.Runners)
                {
                    m.runners.Add(new Market.RunnerCatalog()
                    {
                        selectionId = runner.selectionId,
                    });
                }
            }
            foreach (Runner runner in book.Runners)
            {
                foreach (Market.RunnerCatalog cat in m.runners)
                {
                    if (cat.selectionId == runner.selectionId)
                    {
                        runner.Catalog = cat;
                    }
                }
            }
            return book;
        }
        public List<MarketBook> GetMarketBooks(String[] mid)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["priceProjection"] = new priceProjection()
            {
                priceData = GetHashSet<priceDataEnum>((uint)(priceDataEnum.SP_AVAILABLE | priceDataEnum.EX_BEST_OFFERS))
            };
            p["marketIds"] = mid;
//            p["exchangeIds"] = new Int32[] { Int32.Parse(mid[0].Split('.')[0]) };
            return RPCRequest<List<MarketBook>>("listMarketBook", p) as List<MarketBook>;
        }
        public List<ClearedOrder> GetClearedOrders(Int32 ExchangeId, String mid, betStatusEnum status)
        {
            List<ClearedOrder> os2 = new List<ClearedOrder>();
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketIds"] = new String[] { mid };
//            p["exchangeIds"] = new Int32[] { Int32.Parse(mid.Split('.')[0]) };
            p["betStatus"] = status.ToString();
            p["recordCount"] = 0;
            for (; ; )
            {
                OrderSummary os = RPCRequest<OrderSummary>("listClearedOrders", p) as OrderSummary;
                os2.AddRange(os.Orders);
                if (!os.moreAvailable)
                {
                    break;
                }
            }
            return os2;
        }
        public List<ClearedOrder> GetClearedOrders(Int32 ExchangeId, DateTime from, TimeSpan to, betStatusEnum status)
        {
            List<ClearedOrder> os2 = new List<ClearedOrder>();
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["betStatus"] = status.ToString();
            p["settledDateRange"] = TimeRange(from, to.Days, to.Hours);
            p["recordCount"] = 0;
            for (Int32 fromRecord = 0; ; fromRecord += 1000)
            {
                p["fromRecord"] = fromRecord;
                OrderSummary os = RPCRequest<OrderSummary>("listClearedOrders", p) as OrderSummary;
                os2.AddRange(os.Orders);
                if (!os.moreAvailable)
                {
                    break;
                }
            }
            return os2;
        }
        public String GetClearedOrdersRaw(Int32 ExchangeId, DateTime from, TimeSpan to, betStatusEnum status)
        {
            List<ClearedOrder> os2 = new List<ClearedOrder>();
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["betStatus"] = status.ToString();
            p["settledDateRange"] = TimeRange(from, to.Days, to.Hours);
            p["recordCount"] = 0;
            return RPCRequestRaw("listClearedOrders", p);
        }
        public List<ClearedOrder> GetClearedOrders(Int32 ExchangeId, HashSet<UInt64> betIds, betStatusEnum status)
        {
            List<ClearedOrder> os2 = new List<ClearedOrder>();
            if (betIds.Count > 0)
            {
                Dictionary<String, Object> p = new Dictionary<string, object>();
                p["betIds"] = betIds;
                p["betStatus"] = status.ToString();
                p["exchangeIds"] = new Int32[] { ExchangeId };
                p["fromRecord"] = os2.Count;
                p["recordCount"] = 500;
                p["includeItemDescription"] = false;
                for (; ; )
                {
                    OrderSummary os = RPCRequest<OrderSummary>("listClearedOrders", p) as OrderSummary;
                    os2.AddRange(os.Orders);
                    if (!os.moreAvailable)
                    {
                        break;
                    }
                }
            }
            return os2;
        }
        public PlaceExecutionReport placeOrders(String marketId, List<PlaceInstruction> instructions) 
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketId"] = marketId;
            p["instructions"] = instructions;
            return RPCRequest<PlaceExecutionReport>("placeOrders", p) as PlaceExecutionReport;
        }
        public CancelExecutionReport cancelOrders(String marketId, List<CancelInstruction> instructions)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketId"] = marketId;
            p["instructions"] = instructions;
            return RPCRequest<CancelExecutionReport>("cancelOrders", p) as CancelExecutionReport;
        }
        public PlaceExecutionReport placeOrder(String marketId, Int32 selectionId, sideEnum side, Double size, Double price, persistenceTypeEnum persistenceType)
        {
            List<PlaceInstruction> pis = new List<PlaceInstruction>();
            PlaceInstruction pi = new PlaceInstruction();
            pi.selectionId = selectionId;
            pi.sideEnum = side;
            pi.orderTypeEnum = orderTypeEnum.LIMIT;
            pi.limitOrder = new LimitOrder()
            {
                size = size,
                price = price,
                persistenceTypeEnum = persistenceTypeEnum.LAPSE,
            };
            pis.Add(pi);
            return placeOrders(marketId, pis);
        }
        public CancelExecutionReport cancelOrder(String marketId, UInt64 betId)
        {
            List<CancelInstruction> pis = new List<CancelInstruction>();
            CancelInstruction pi = new CancelInstruction(betId);
            pis.Add(pi);
            return cancelOrders(marketId, pis);
        }
        public CurrentOrderSummaryReport listCurrentOrders(String marketId)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            HashSet<String> marketIds = new HashSet<String>();
            marketIds.Add(marketId);
            p["marketIds"] = marketIds;
            p["betIds"] = new HashSet<UInt64>();
            p["orderProjection"] = orderProjectionEnum.ALL.ToString();
            p["dateRange"] = TimeRange(DateTime.UtcNow.Date, 0, 24);
            if (PlayBackDate.Ticks != 0)
            {
                p["dateRange"] = TimeRange(new DateTime(PlayBackDate.Ticks, DateTimeKind.Utc), 0, 24);
            }
            return RPCRequest<CurrentOrderSummaryReport>("listCurrentOrders", p) as CurrentOrderSummaryReport;
        }
        public AccountFundsResponse getAccountFunds(Int32 ExchangeId)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["exchangeIds"] = new Int32[] { ExchangeId };
            return RPCRequest<AccountFundsResponse>("getAccountFunds", p) as AccountFundsResponse;
        }
        public List<MarketProfitAndLoss> listMarketProfitAndLoss(String marketId)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketIds"] = new String[] { marketId };
//            p["exchangeIds"] = new Int32[] { Int32.Parse(marketId.Split('.')[0]) };
            p["includeSettledBets"] = false;
            p["includeBspBets"] = false;
            p["netOfCommission"] = true;
            return RPCRequest<List<MarketProfitAndLoss>>("listMarketProfitAndLoss", p) as List<MarketProfitAndLoss>;
        }
    }
}
