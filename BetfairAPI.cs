using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace BetfairAPI
{
    public class BetfairAPI
    {
        private String AppKey { get; set; }  //"uCmjo7RlVUJRCc0T";
        private String Token { get; set; }
        public String SessionToken { get { return Token; } }
        public BetfairAPI()
        {
        }
        public DateTime sysTime = DateTime.UtcNow;
        private Object RPCRequest<T>(String Method, Dictionary<String, Object> Params)
        {
            String[] AccountCalls = new String[] { "getAccountFunds" };
            Dictionary<String, Object> joe = new Dictionary<string, object>();
            joe["jsonrpc"] = "2.0";
            joe["id"] = "1";
            joe["method"] = "SportsAPING/v1.0/" + Method;
            joe["params"] = Params;

            String url = "http://" + SpreadTrader.Properties.Settings.Default.Proxy; // "http://127.0.0.1:5055"; 

            if (AccountCalls.Contains(Method))
            {
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
        public Dictionary<String, Object> Today()
        {
            Dictionary<String, Object> start = new Dictionary<string, object>();
            start["from"] = DateTime.UtcNow;
            start["to"] = DateTime.UtcNow.Date.AddDays(1);
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
        public List<VenueResult> ListVenues()
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["exchangeIds"] = new Int32[] { 1 };
            filter["eventTypeIds"] = new Int32[] { 7 };
            filter["marketCountries"] = new String[] { "GB", "IE" };
            filter["marketStartTime"] = Today();
            p["filter"] = filter;
            return RPCRequest <List<VenueResult>>("listVenues", p) as List<VenueResult>;
        }
        public List<EventTypeResult> GetEventTypes() 
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

			//filter["marketStartTime"] = Today();
			//filter["marketCountries"] = new String[] { "GB" };
			p["filter"] = filter;
            return RPCRequest<List<EventTypeResult>>("listEventTypes", p) as List<EventTypeResult>;
        }
        public List<CompetitionResult> GetCompetitions(Int32 event_type)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { event_type };
            p["filter"] = filter;
            return RPCRequest<List<CompetitionResult>>("listCompetitions", p) as List<CompetitionResult>;
        }
        public List<VenueResult> GetVenues(Int32 event_type, String country)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { event_type };
            filter["marketStartTime"] = Today();
            filter["marketCountries"] = new String[] { country };
            p["filter"] = filter;
            return RPCRequest<List<VenueResult>>("listVenues", p) as List<VenueResult>;
        }
        public List<Event> GetEventsForCompetition(Int32 competition_id)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

			filter["competitionIds"] = new Int32[] { competition_id };
			p["filter"] = filter;
            return RPCRequest<List<Event>>("listEvents", p) as List<Event>;
        }
        public List<Event> GetEvents(Int32 eventType_id)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { eventType_id };
            p["filter"] = filter;
            return RPCRequest<List<Event>>("listEvents", p) as List<Event>;
        }
        public List<Event> GetEvents(Int32 event_type, String country_or_venue)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { event_type };
            //filter["marketCountries"] = new String[] { country_code };
            filter["venues"] = new String[] { country_or_venue };
            filter["marketStartTime"] = Today();
            p["filter"] = filter;
            return RPCRequest<List<Event>>("listEvents", p) as List<Event>;
        }
        public List<Market> GetMarkets(Int32 event_type_id, String venue)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventIds"] = new Int32[] { event_type_id };
            //filter["eventTypeIds"] = new Int32[] { event_type_id };
            //filter["venues"] = new String[] { venue };
            //filter["marketStartTime"] = Today();
            filter["marketBettingTypes"] = new String[] { "ODDS" };
            filter["marketTypeCodes"] = new String[] { "HALF_TIME_SCORE", "MATCH_ODDS", "WIN", };
            p["marketProjection"] = new String[] { "MARKET_DESCRIPTION", "RUNNER_DESCRIPTION" };
            p["filter"] = filter;
            p["maxResults"] = 200;

            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        public List<Market> GetMarkets(Int32 event_type, Int32 event_id)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventIds"] = new Int32[] { event_id };
            //filter["eventTypeIds"] = new Int32[] { event_type_id };
            //filter["marketStartTime"] = Today();
            //            filter["marketBettingTypes"] = new String[] { "ODDS" };
            //            filter["marketTypeCodes"] = new String[] { "HALF_TIME_SCORE", "MATCH_ODDS", "WIN" };
            if (event_type == 7)
            {
                filter["marketTypeCodes"] = new String[] { "MATCH_ODDS", "WIN" };
            }
            p["marketProjection"] = new String[] { "MARKET_DESCRIPTION", "RUNNER_DESCRIPTION", "EVENT" };
            p["filter"] = filter;
            p["maxResults"] = 200;

            return RPCRequest<List<Market>>("listMarketCatalogue", p) as List<Market>;
        }
        public List<CountryCodeResult> GetCountries(Int32 event_type_id)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            Dictionary<String, Object> filter = new Dictionary<string, object>();

            filter["eventTypeIds"] = new Int32[] { event_type_id };
            filter["marketStartTime"] = Today();
            p["filter"] = filter;
            p["maxResults"] = 17;

            return RPCRequest<List<CountryCodeResult>>("listCountries", p) as List<CountryCodeResult>;
        }
        private void CalculateProfitAndLoss(MarketBook book)
        {
            CurrentOrderSummaryReport Orders = listCurrentOrders(book.marketId);
            foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in Orders.currentOrders)
            {
#if DEBUG
                //o.sizeMatched = o.priceSize.size;
                //o.averagePriceMatched = o.priceSize.price;
#endif

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
        public MarketBook GetMarketBook(Market m)
        {
            Dictionary<String, Object> p = new Dictionary<string, object>();
            p["marketIds"] = new String[] { m.marketId };
            p["priceProjection"] = new priceProjection()
            {
                priceData = GetHashSet<priceDataEnum>((uint)(priceDataEnum.EX_BEST_OFFERS))
            };
            p["orderProjection"] = orderProjectionEnum.ALL.ToString();
            List<MarketBook> books = RPCRequest<List<MarketBook>>("listMarketBook", p) as List<MarketBook>;
            if (books.Count>0)
                CalculateProfitAndLoss(books.First());
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
        public List<MarketBook> GetRunnerBook()
        {
            List<MarketBook> books = new List<MarketBook>();
            return books;
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
        public PlaceExecutionReport placeOrder(String marketId, Int64 selectionId, sideEnum side, Double size, Double price)
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
            p["marketIds"] = new String[] { marketId }; ;
            //p["betIds"] = new HashSet<UInt64>();
            p["orderProjection"] = orderProjectionEnum.ALL.ToString();
            //p["dateRange"] = TimeRange(DateTime.UtcNow.Date, 0, 24);
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
            p["includeSettledBets"] = true;
            p["includeBspBets"] = true;
            p["netOfCommission"] = true;
            return RPCRequest<List<MarketProfitAndLoss>>("listMarketProfitAndLoss", p) as List<MarketProfitAndLoss>;
        }
        public static double BetfairPrice(double _v)
        {
            Decimal v = (Decimal) _v;
            if (v <= 1.01M) return 1.01;
            if (v >= 1000) return 1000;

            Decimal[] MinValue = { 1.01M, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
            Decimal[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
            Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

            Int32 idx = 0;
            for (; idx < MinValue.Length; idx++)
            {
                if (v >= MinValue[idx] && v <= MaxValue[idx])
                {
                    break;
                }
            }
            Decimal lo = (Int32)(v / Increment[idx]) * Increment[idx];
            lo = Math.Round(lo, 2);

            if (lo == v)
            {
                return (double) v;
            }
            Decimal hi = lo + Increment[idx];
            return (double) (Math.Abs(lo - v) < Math.Abs(hi - v) ? lo : hi);
        }
    }
}
