using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace BetfairAPI
{
	public static class ErrorCodes
	{
		static String[,] _ErrorCodes = new string[,] {
			{"DSC-0008", "JSONDeserialisationParseFailure"},
			{"DSC-0009", "ClassConversionFailure"},
			{"DSC-0015", "SecurityException"},
			{"DSC-0018", "MandatoryNotDefined"},
			{"DSC-0019", "Timeout"},
			{"DSC-0021", "NoSuchOperation"},
			{"DSC-0023", "NoSuchService"},
			{"DSC-0024", "RescriptDeserialisationFailure"},
			{"DSC-0034", "UnknownCaller"},
			{"DSC-0035", "UnrecognisedCredentials"},
			{"DSC-0036", "InvalidCredentials"},
			{"DSC-0037", "SubscriptionRequired"},
			{"DSC-0038", "OperationForbidden"},
			{"ANGX-0003", "Invalid Session Information"}
		};
		static public String FaultCode(String code)
		{
			for (int i = 0; i < _ErrorCodes.Length / 2; i++)
			{
				if (_ErrorCodes[i, 0] == code)
					return _ErrorCodes[i, 1];
			}
			return "Error code not found " + code;
		}
	}
	public enum MarketSortEnum
	{
		MINIMUM_TRADED,
		MAXIMUM_TRADED,
		MINIMUM_AVAILABLE,
		MAXIMUM_AVAILABLE,
		FIRST_TO_START,
		LAST_TO_START
	}
	public enum ExecutionReportStatusEnum
	{
		SUCCESS, FAILURE, PROCESSED_WITH_ERRORS, TIMEOUT
	}
	public enum ExecutionReportErrorCodeEnum
	{
		ERROR_IN_MATCHER,
		PROCESSED_WITH_ERRORS,
		BET_ACTION_ERROR,
		INVALID_ACCOUNT_STATE,
		INVALID_WALLET_STATUS,
		INSUFFICIENT_FUNDS,
		LOSS_LIMIT_EXCEEDED,
		MARKET_SUSPENDED,
		MARKET_NOT_OPEN_FOR_BETTING,
		DUPLICATE_TRANSACTION,
		INVALID_ORDER,
		INVALID_MARKET_ID,
		PERMISSION_DENIED,
		DUPLICATE_BETIDS,
		NO_ACTION_REQUIRED,
		SERVICE_UNAVAILABLE,
		REJECTED_BY_REGULATOR,
		NO_CHASING,
		REGULATOR_IS_NOT_AVAILABLE ,
		TOO_MANY_INSTRUCTIONS ,
		INVALID_MARKET_VERSION,
		INVALID_PROFIT_RATIO
	}
	public enum InstructionReportStatusEnum
	{
		SUCCESS,
		FAILURE,
		TIMEOUT
	}
	public enum InstructionReportErrorCodeEnum
	{
		INVALID_BET_SIZE,
		INVALID_RUNNER,
		BET_TAKEN_OR_LAPSED,
		BET_IN_PROGRESS,
		RUNNER_REMOVED,
		MARKET_NOT_OPEN_FOR_BETTING,
		LOSS_LIMIT_EXCEEDED,
		MARKET_NOT_OPEN_FOR_BSP_BETTING,
		INVALID_PRICE_EDIT,
		INVALID_ODDS,
		INSUFFICIENT_FUNDS,
		INVALID_PERSISTENCE_TYPE,
		ERROR_IN_MATCHER,
		INVALID_BACK_LAY_COMBINATION,
		ERROR_IN_ORDER,
		INVALID_BID_TYPE,
		INVALID_BET_ID,
		CANCELLED_NOT_PLACED,
		RELATED_ACTION_FAILED,
		NO_ACTION_REQUIRED,
		TIME_IN_FORCE_CONFLICT,
		UNEXPECTED_PERSISTENCE_TYPE,
		INVALID_ORDER_TYPE,
		UNEXPECTED_MIN_FILL_SIZE,
		INVALID_CUSTOMER_ORDER_REF,
		INVALID_MIN_FILL_SIZE,
		BET_LAPSED_PRICE_IMPROVEMENT_TOO_LARGE
	}
	public enum OrderStatusEnum
	{
		PENDING,
		EXECUTION_COMPLETE,
		EXECUTABLE,
		EXPIRED,
	}
	public enum TimeZoneEnum
	{
		System,
		UK,
		UTC,
	}
	public enum marketProjectionEnum
	{
		COMPETITION = 0x01,
		EVENT = 0x02,
		EVENT_TYPE = 0x04,
		MARKET_START_TIME = 0x08,
		MARKET_DESCRIPTION = 0x10,
		RUNNER_DESCRIPTION = 0x20,
		RUNNER_METADATA = 0x40,
		ALL = COMPETITION | EVENT | EVENT_TYPE | MARKET_START_TIME | MARKET_DESCRIPTION | RUNNER_DESCRIPTION | RUNNER_METADATA,



		//RUNNER_DESCRIPTION = 0x01,
		//RUNNER_METADATA = 0x02,
		//MARKET_START_TIME = 0x04,
		//EVENT_TYPE = 0x08,
		//MARKET_DESCRIPTION = 0x10,
		//EVENT = 0x20,
		//DETAILS = 0x20,
		//ALL = RUNNER_DESCRIPTION | RUNNER_METADATA | MARKET_START_TIME | EVENT_TYPE | MARKET_DESCRIPTION | DETAILS,
	}
	public enum marketTypeEnum
	{
		WIN = 0x01,
		PLACE = 0x02,
		MATCH_ODDS = 0x04,
		HALF_TIME_SCORE = 0x08,
	}
	public enum betStatusEnum
	{
		SETTLED = 0x01,
		LAPSED = 0x02,
		VOIDED = 0x04,
		CANCELLED = 0x08,
	}
	public enum marketStatusEnum
	{
		INACTIVE,
		OPEN,
		SUSPENDED,
		CLOSED,
	}
	public enum timeZoneEnum
	{
		UTC,
		GMT,
		LOCAL,
	}
	public enum priceDataEnum
	{
		SP_AVAILABLE = 0x01,
		SP_TRADED = 0x02,
		EX_BEST_OFFERS = 0x04,
		EX_ALL_OFFERS = 0x08,
		EX_TRADED = 0x10,
	}
	public enum exBestOffersOverridesEnum
	{
		STAKE = 0x01,
		PAYOUT = 0x02,
		MANAGED_LIABILITY = 0x04,
		NONE = 0x08,
	}
	public enum orderTypeEnum
	{
		LIMIT,
		LIMIT_ON_CLOSE,
		MARKET_ON_CLOSE,
	}
	public enum sideEnum
	{
		BACK,
		LAY,
	}
	public enum animalTypeEnum
	{
		Thoroughbred,
		Greyhound,
		Harness,
		Other,
	}
	public enum persistenceTypeEnum
	{
		LAPSE,
		PERSIST,
		MARKET_ON_CLOSE,
	}
	public enum orderProjectionEnum
	{
		ALL = 0x01,
		EXECUTABLE = 0x02,
		EXECUTION_COMPLETE = 0x04,
	}
	public class LoginResponse
	{
		[JsonProperty(PropertyName = "loginStatus")]
		private string loginStatus { get; set; }
		public string sessionToken { get; set; }
		public Boolean Status { get { return loginStatus == "SUCCESS"; } }
		public override string ToString()
		{
			return loginStatus;
		}
	}
	public class ErrorResponse
	{
		public struct _data
		{
			public struct APINGException
			{
				public String errorCode { get; set; }
				public String errorDetails { get; set; }
				public String requestUUID { get; set; }
			}
			public struct AccountAPINGException
			{
				public String errorCode { get; set; }
				public String errorDetails { get; set; }
				public String requestUUID { get; set; }
			}
			[JsonProperty(PropertyName = "APINGException")]
			public APINGException exception { get; set; }
			[JsonProperty(PropertyName = "AccountAPINGException")]
			public APINGException AccountException { get; set; }
			public String exceptionName { get; set; }
		}
		public Int32 code { get; set; }
		public _data data { get; set; }
		public String message { get; set; }
		public override string ToString()
		{
			return data.AccountException.errorDetails != null ? data.AccountException.errorDetails : data.exception.errorDetails;
		}
	}
	public class VenueResult
	{
		public string venue { get; set; }
		public Int32 marketCount { get; set; }
		public override string ToString()
		{
			return venue;
		}
	}
	public class priceProjection
	{
		public class ExBestOffersOverrides
		{
			public int bestPricesDepth { get; set; }
			public String rollupModel { get; set; }
			public int rollupLimit { get; set; }
			public Double rollupLiabilityThreshold { get; set; }
			public int rollupLiabilityFactor { get; set; }
		}
		public HashSet<String> priceData { get; set; }
		public List<ExBestOffersOverrides> exBestOffersOverrides { get; set; }
		public Boolean virtualise { get; set; }
		public Boolean rolloverStakes { get; set; }
	}
	public class EventType
	{
		public String name { get; set; }
		public Int32 id { get; set; }
		public bool IsChecked { get; set; }
	}
	public class PriceSize
	{
		public PriceSize(double price, double size) { this.price = price; this.size = size; }
		public Double price { get; set; }
		public Double size { get; set; }
		public override string ToString()
		{
			return String.Format("{0}:{1}", price, size);
		}
	}
	public class Runner
	{
		public class Order
		{
			public UInt64 betId { get; set; }
			public String orderType { get; set; }
			public String orderStatus { get; set; }
			public String persistenceType { get; set; }
			public String side { get; set; }
			public Double price { get; set; }
			public Double size { get; set; }
			public Double bspLiability { get; set; }
			public DateTime placedDate { get; set; }
			public Double avgPriceMatched { get; set; }
			public Double sizeMatched { get; set; }
			public Double sizeRemaining { get; set; }
			public Double sizeLapsed { get; set; }
			public Double profit { get; set; }
			public override string ToString()
			{
				return String.Format("{0} : {1} : {2}", betId, side, price);
			}
		}
		public class Match
		{
			public UInt64 betId { get; set; }
			public String matchId { get; set; }
			public String side { get; set; }
			public Double price { get; set; }
			public Double size { get; set; }
			public DateTime matchedDate { get; set; }
		}
		public class ExchangePrices
		{
			public List<PriceSize> availableToBack { get; set; }
			public List<PriceSize> availableToLay { get; set; }
			public List<PriceSize> tradedVolume { get; set; }
		}
		public class StartingPrices
		{
			public Double nearPrice { get; set; }
			public Double farPrice { get; set; }
			public List<PriceSize> backStakeTaken { get; set; }
			public List<PriceSize> layStakeTaken { get; set; }
			public Double actualSP { get; set; }
		}
		public Market.RunnerCatalog Catalog { set; get; }
		public Int32 selectionId { get; set; }
		public Double handicap { get; set; }
		public String status { get; set; }
		public Double adjustmentFactor { get; set; }
		public Double lastPriceTraded { get; set; }
		public Double totalMatched { get; set; }
		public DateTime removalDate { get; set; }
		public StartingPrices sp { get; set; }
		public ExchangePrices ex { get; set; }
		public List<Order> orders { get; set; }
		public List<Match> matches { get; set; }
		public Double ifWin { get; set; }
		public String Name { get; set; }
		//public Object Tag { get; set; }
		public double BackLayRatio
		{
			get
			{
				if (ex != null && ex.availableToLay.Count > 0 && ex.availableToBack.Count > 0)
				{
					double BackLayRatio = Math.Abs(ex.availableToBack[0].price - ex.availableToLay[0].price);

					BackLayRatio /= ex.availableToBack[0].price;
					return BackLayRatio *= 100;
				}
				return 0;
			}
		}
	}
	public class MarketBook
	{
		public String marketId { get; set; }
		public Boolean isMarketDataDelayed { get; set; }
		public marketStatusEnum status { get; set; }
		public Int32 betDelay { get; set; }
		public Boolean bspReconciled { get; set; }
		public Boolean complete { get; set; }
		public Boolean inplay { get; set; }
		public Int32 numberOfWinners { get; set; }
		public Int32 numberOfRunners { get; set; }
		public Int32 numberOfActiveRunners { get; set; }
		public DateTime lastMatchTime { get; set; }
		public Double totalMatched { get; set; }
		public Double totalAvailable { get; set; }
		public Boolean crossMatching { get; set; }
		public Boolean runnersVoidable { get; set; }
		public UInt32 version { get; set; }
		public List<Runner> Runners { get; set; }
		public double BackBook
		{
			get
			{
				double book = 0;
				foreach (Runner r in Runners)
				{
					if (r.ex.availableToBack.Count > 0)
						book += 1 / r.ex.availableToBack[0].price;
				}
				return book * 100;
			}
		}
		public double LayBook
		{
			get
			{
				double book = 0;
				foreach (Runner r in Runners)
				{
					if (r.ex.availableToLay.Count > 0)
						book += 1 / r.ex.availableToLay[0].price;
				}
				return book * 100;
			}
		}
	}
	public struct ClearedOrder
	{
		public Int32 eventTypeId { get; set; }
		public Int32 eventId { get; set; }
		public String marketId { get; set; }
		public Int32 selectionId { get; set; }
		public Double handicap { get; set; }
		public UInt64 betId { get; set; }
		public DateTime placedDate { get; set; }
		public String persistenceType { get; set; }
		public String orderType { get; set; }
		public String side { get; set; }
		public Double priceRequested { get; set; }
		public DateTime settledDate { get; set; }
		public Int32 betCount { get; set; }
		public Double priceMatched { get; set; }
		public Double priceReduced { get; set; }
		public Double sizeSettled { get; set; }
		public Double profit { get; set; }
		public override string ToString()
		{
			return String.Format("{0} : {1} : {2}", betId, side, profit);
		}
	}
	public class OrderSummary
	{
		[JsonProperty(PropertyName = "clearedOrders")]
		private List<ClearedOrder> clearedOrders { get; set; }
		public Boolean moreAvailable { get; set; }
		public List<ClearedOrder> Orders { get { return clearedOrders; } }
	}
	public class CurrentOrderSummaryReport
	{
		public class CurrentOrderSummary
		{
			public UInt64 betId { get; set; }
			public String marketId { get; set; }
			public Int32 selectionId { get; set; }
			public Double handicap { get; set; }
			public PriceSize priceSize { get; set; }
			public Double bspLiability { get; set; }
			public String side { get; set; }
			public String status { get; set; }
			public String persistenceType { get; set; }
			public String orderType { get; set; }
			public DateTime placedDate { get; set; }
			public Double averagePriceMatched { get; set; }
			public Double sizeMatched { get; set; }
			public Double sizeRemaining { get; set; }
			public Double sizeLapsed { get; set; }
			public Double sizeCancelled { get; set; }
			public Double sizeVoided { get; set; }
		}
		public Boolean moreAvailable { get; set; }
		public List<CurrentOrderSummary> currentOrders { get; set; }
	}
	public class CountryCodeResult
	{
		public String countryCode { get; set; }
		public Int32 marketCount { get; set; }

	}
	public class Market
	{
		public struct Description
		{
			public String bettingType { get; set; }
			public Boolean bspMarket { get; set; }
			public Boolean discountAllowed { get; set; }
			public Boolean persistenceEnabled { get; set; }
			public Boolean rulesHasDate { get; set; }
			public Boolean turnInPlayEnabled { get; set; }
			public String clarifications { get; set; }
			public String rules { get; set; }
			public String wallet { get; set; }
			public String marketType { get; set; }
			public Double marketBaseRate { get; set; }
			public DateTime marketTime { get; set; }
			public DateTime suspendTime { get; set; }
		}
		public TimeZoneEnum DisplayTimeZone { get; set; }
		public String ToolTip { get; set; }
		public Object Tag { get; set; }
		public Boolean Selected { get; set; }
		public Market PlaceMarket { get; set; }
		public class RunnerCatalog
		{
			[JsonProperty(PropertyName = "runnerName")]
			public String name { get; set; }
			public Double handicap { get; set; }
			public Dictionary<String, String> metadata { get; set; }
			public Int32 selectionId { get; set; }
			public Int32 sortPriority { get; set; }
			public String MetaData(String what)
			{
				if (metadata != null && metadata.ContainsKey(what) && metadata[what] != null)
				{
					return metadata[what];
				}
				return "";
			}
			public Int32 MetaDataInt(String what)
			{
				if (MetaData(what) != "")
				{
					return Int32.Parse(MetaData(what));
				}
				return 0;
			}
			public override string ToString()
			{
				return new StringBuilder()
							.AppendFormat("{0}", name)
							.AppendFormat(" : {0}", selectionId)
							.ToString();
			}
			public Int32 colorsId { get; set; }
		}
		public List<RunnerCatalog> runners { get; set; }
		public String marketId { get; set; }
		public String marketName { get; set; }
		public DateTime marketStartTime { get; set; }
		public String MarketDescription { get; set; }
		public Double totalMatched { get; set; }
		[JsonProperty(PropertyName = "event")]
		public Event.Details details { get; set; }
		public EventType eventType { get; set; }
		public Description description { get; set; }
		public override string ToString()
		{
			return String.Format("{0:HH:mm} {1}", description.marketTime, marketName);
		}
		public MarketBook MarketBook { get; set; }
		public string ToString(TimeZoneEnum Zone)
		{
			DateTime t = new DateTime();
			switch (DisplayTimeZone)
			{
				case TimeZoneEnum.System: t = startTimeLocal; break;
				case TimeZoneEnum.UK: t = startTimeGmt; break;
				case TimeZoneEnum.UTC: t = startTimeUtc; break;
			}
			return String.Format("{0} {1:HH:mm} {2}", details == null ? "##" : details.Venue, t, marketName);
		}
		public DateTime startTimeGmt
		{
			get
			{
				marketStartTime = DateTime.SpecifyKind(marketStartTime, DateTimeKind.Utc);
				TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
				return TimeZoneInfo.ConvertTimeFromUtc(marketStartTime, tzi);
			}
		}
		public DateTime startTimeUtc
		{
			get
			{
				return marketStartTime;
			}
		}
		public DateTime startTimeLocal
		{
			get
			{
				return TimeZone.CurrentTimeZone.ToLocalTime(DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc));
			}
		}
		public DateTime startTimeZone
		{
			get
			{
				switch (DisplayTimeZone)
				{
					case TimeZoneEnum.System: return startTimeLocal;
					case TimeZoneEnum.UK: return startTimeGmt;
				}
				return startTimeUtc;
			}
		}
		public animalTypeEnum animalType
		{
			get
			{
				switch (eventType.id)
				{
					case 4339: return animalTypeEnum.Greyhound;
					case 7:
						if (marketName.Contains(" Trot") || marketName.Contains(" Pace"))
						{
							return animalTypeEnum.Harness;
						}
						return animalTypeEnum.Thoroughbred;
					default: return animalTypeEnum.Other;
				}
			}
		}
		public String Class
		{
			get
			{
				if (marketName == "To Be Placed" || description.marketType != "WIN") //marketTypeEnum.WIN)
				{
					return "";
				}
				if (eventType.id == 4339)
				{
					return "";
				}
				String[] cs = marketName.Split(new Char[] { ' ' });

				if (cs.Count() > 3 && marketName.Contains(" Pace ") || marketName.Contains(" Trot "))
					return cs[2] + " " + cs[3];

				if (cs.Count() == 3 && cs[0].StartsWith("R") && Char.IsDigit(cs[0][1]))
				{
					return cs.Last().Trim();
				}
				return marketName.Replace(cs.First(), "").Trim();
			}
		}
		public int RaceNumber
		{
			get
			{
				if (marketName == "To Be Placed" || description.marketType != "WIN") //)
				{
					return 0;
				}
				String[] parts = marketName.Split(new Char[] { ' ' });
				String part = parts[0];

				if (details.countryCode == "GB" || details.countryCode == "IE")
				{
					part = parts[1];
					if (eventType.id == 4339)
						return 0;// Convert.ToInt32(parts[0].Replace("A", ""));
				}
				if (!part.Contains("R"))
				{
					return 0;
				}
				return Convert.ToInt32(part.Replace("R", ""));
			}
		}
		public Int32 Distance
		{
			get
			{
				try
				{
					if (marketName == "To Be Placed")
					{
						return 0;
					}
					String[] parts = marketName.Split(new Char[] { ' ' });
					String part = parts[1];

					if (details.countryCode == "GB" || details.countryCode == "IE")
					{
						part = parts[0];
						if (eventType.id == 4339)
							return Convert.ToInt32(parts[1].ToLower().Replace("m", ""));
					}
					else if (details.countryCode == "US")
					{
						part = parts[1];
					}
					else if (details.countryCode == "AU" || details.countryCode == "NZ" || details.countryCode == "ZA")
					{
						if (part.EndsWith("m"))
							return Convert.ToInt32(part.Replace("m", ""));
					}
					else // assume meters
					{
						return Convert.ToInt32(parts[0].Replace("m", ""));
					}
					Int32 miles = 0;
					Int32 furlongs = 0;

					if (part.EndsWith("m"))
					{
						miles = Convert.ToInt32(part.Replace("m", ""));
					}
					if (part.EndsWith("f"))
					{
						if (part.Contains("m"))
						{
							String[] parts2 = part.Split(new Char[] { 'm' });
							miles = Convert.ToInt32(parts2[0].Replace("m", ""));
							furlongs = Convert.ToInt32(parts2[1].Replace("f", ""));
						}
						else
						{
							furlongs = Convert.ToInt32(part.Replace("f", ""));
						}
					}
					return Convert.ToInt32(1609.344 * miles + 201.168 * furlongs);
				}
				catch (Exception) { return 0; }
			}
		}
	}
	public class EventTypeResult
	{
		public EventType eventType { get; set; }
		public Int32 marketCount { get; set; }
		public override string ToString()
		{
			return eventType.name;
		}
	}
	public class Competition
	{
		public Int32 id { get; set; }
		public String name { get; set; }
	}
	public class CompetitionResult
	{
		public Competition competition { get; set; }
		public Int32 marketCount { get; set; }
		public String competitionRegion { get; set; }
		public override string ToString()
		{
			return competition.name;
		}
	}
	public class Event
	{
		public class Details
		{
			public String distance { get; set; }
			public Int32 id { get; set; }
			public string name { get; set; }
			public String countryCode { get; set; }
			public string timezone { get; set; }
			[JsonProperty(PropertyName = "venue")]
			private string venue { get; set; }
			public string Venue { get { return venue == "Clairfontaine" ? "Clairefontaine" : venue; } set { venue = value; } }
			public DateTime openDate { get; set; }
			public override string ToString()
			{
				return String.Format("{0} {1:HH:mm} {2}", venue, openDate, name);
			}
		}
		[JsonProperty(PropertyName = "event")]
		public Details details { get; set; }
		public EventType eventType { get; set; }
		public Int32 marketCount { get; set; }
		public override string ToString()
		{
			return details.ToString();
		}
	}
	public class instructionReport
	{
		public class Instruction
		{
			public class LimitOrder
			{
				public Double size { get; set; }
				public Double price { get; set; }
				public String persistenceType { get; set; }
			}
			public Double handicap { get; set; }
			public LimitOrder limitOrder { get; set; }
			public String orderType { get; set; }
			public Int32 selectionId { get; set; }
			public String side { get; set; }
		}
		public Double averagePriceMatched { get; set; }
		public UInt64 betId { get; set; }
		public Instruction instruction { get; set; }
		public DateTime placedDate { get; set; }
		public Double sizeMatched { get; set; }
		public String status { get; set; }
		public String errorCode { get; set; }
	}
	public class PlaceExecutionReport
	{
		public String status { get; set; }
		public String errorCode { get; set; }
		public String marketId { get; set; }
		public List<instructionReport> instructionReports { get; set; }
	}
	public class ReplaceExecutionReport
	{
		public String customerRef { get; set; }
		public ExecutionReportStatusEnum status { get; set; }
		public ExecutionReportErrorCodeEnum ErrorCode { get; set; }
		public String marketID { get; set; }
		public List<ReplaceInstructionReport> instructionReports { get; set; }
	}
	public class CancelInstructionReport
	{
		public InstructionReportStatusEnum status { get; set; }
		public InstructionReportErrorCodeEnum errorCode { get; set; }
		public CancelInstruction instruction { get; set; }
		public double sizeCancelled { get; set; }
		public DateTime cancelledDate { get; set; }
	}

	public class PlaceInstructionReport
	{
		public InstructionReportStatusEnum status { get; set; }
		public InstructionReportErrorCodeEnum errorCode { get; set; }
		public OrderStatusEnum orderStatus { get; set; }
		public PlaceInstruction instruction { get; set; }
		public String betId { get; set; }
		public DateTime placedDate { get; set; }
		public double averagePriceMatched { get; set; }
		public double sizeMatched { get; set; }
	}

	public class ReplaceInstructionReport
	{
		public InstructionReportStatusEnum status { get; set; }
		public InstructionReportErrorCodeEnum errorCode { get; set; }
		public CancelInstructionReport cancelInstructionReport { get; set; }
		public PlaceInstructionReport placeInstructionReport { get; set; }
	}
	public class UpdateInstruction
	{
		public String BetID { get; set; }
		public double newPersistenceType { get; set; }
	}
	public class ReplaceInstruction
	{
		public String betId { get; set; }
		public double newPrice { get; set; }
		public double newSize { get; set; }
	}
	public class PlaceInstruction
	{
		[JsonIgnore]
		public orderTypeEnum orderTypeEnum { get; set; }
		[JsonIgnore]
		public sideEnum sideEnum { get; set; }
		[JsonIgnore]
		public String Runner { get; set; }
		[JsonIgnore]
		public marketTypeEnum marketTypeEnum { get; set; }
		[JsonIgnore]
		public Object Tag { get; set; }
		[JsonIgnore]
		public Double bestBack { get; set; }
		[JsonIgnore]
		public Double bestLay { get; set; }

		public String side { get { return sideEnum.ToString(); } set { } }
		public String marketType { get { return marketTypeEnum.ToString(); } set { } }
		public String orderType { get { return orderTypeEnum.ToString(); } set { } }
		public Int64 selectionId { get; set; }
		public LimitOrder limitOrder { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public LimitOnCloseOrder limitOnCloseOrder { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public MarketOnCloseOrder marketOnCloseOrder { get; set; }

		public override string ToString()
		{
			if (limitOrder != null)
			{
				return String.Format("{1},{2},{3},{4:0},{5:0.00},{6}", orderType, marketType, side, Runner, limitOrder.size, limitOrder.price, DateTime.UtcNow.Millisecond);
			}
			if (marketOnCloseOrder != null)
			{
				return String.Format("{1},{2},{3},{4:0.00}", orderType, marketType.ToString(), side, Runner, marketOnCloseOrder.liability);
			}
			return base.ToString();
		}
	}
	public class CancelExecutionReport
	{
		public String customerRef { get; set; }
		public String status { get; set; }
		public String errorCode { get; set; }
		public String marketId { get; set; }
	}
	public class CancelInstruction
	{
		public CancelInstruction(UInt64 betId)
		{
			this.betId = betId;
		}
		public UInt64 betId { get; set; }
	}
	public class LimitOrder
	{
		public Double size { get; set; }
		public Double price { get; set; }
		public String persistenceType { get { return persistenceTypeEnum.ToString(); } set { } }
		[JsonIgnore]
		public persistenceTypeEnum persistenceTypeEnum { get; set; }
	}
	public class LimitOnCloseOrder
	{
		public Double liability { get; set; }
		public Double price { get; set; }
	}
	public class MarketOnCloseOrder
	{
		public Double liability { get; set; }
	}
	public class AccountFundsResponse
	{
		public Double availableToBetBalance { get; set; }
		public Double exposure { get; set; }
		public Double retainedCommission { get; set; }
		public Double exposureLimit { get; set; }
		public Double discountRate { get; set; }
		public Int32 pointsBalance { get; set; }
	}
	public class MarketProfitAndLoss
	{
		public class RunnerProfitAndLoss
		{
			public long selectionId { get; set; }
			public Double ifWin { get; set; }
			public Double ifLose { get; set; }
		}
		public String marketId { get; set; }
		public Double commissionApplied { get; set; }
		public List<RunnerProfitAndLoss> profitAndLosses { get; set; }
	}
}