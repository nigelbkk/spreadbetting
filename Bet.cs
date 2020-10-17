using System;
using BetfairAPI;

namespace SpreadTrader
{
	public class Bet
	{
		public String Date { get; set; }
		public String Venue { get; set; }
		public String Start { get; set; }
		public String Horse { get; set; }
		public String MarketID { get; set; }
		public Int32 SelectionID { get; set; }
		public marketTypeEnum MarketType { get; set; }
		public sideEnum Side { get; set; }
		public String Exchange { get; set; }
		public Double Stake { get; set; }
		public Double Price { get; set; }
		public orderTypeEnum OrderType { get; set; }
		public UInt64 BetID { get; set; }
		public String Status { get; set; }
		public Double AmtMatched { get; set; }
		public Double AvgPriceMatched { get; set; }
		public bool Resubmitted { get; set; }
		public Bet(Exception xe)
		{
			Status = xe.Message;
		}
		public Bet(String line)
		{
			String[] s = line.Split(',');
			try
			{
				Date = s[0].Trim();
				Venue = s[1].Trim();
				Start = s[2].Trim();
				MarketID = s[3].Trim();
				SelectionID = Convert.ToInt32(s[4].Trim());
				Horse = s[5].Trim();
				MarketType = (marketTypeEnum)Enum.Parse(typeof(marketTypeEnum), s[6].Trim(), true);
				Side = (sideEnum)Enum.Parse(typeof(sideEnum), s[7].Trim(), true);
				Exchange = s[8].Trim();
				Stake = Convert.ToDouble(s[9].Trim());
				Price = Convert.ToDouble(s[10].Trim());
				OrderType = (orderTypeEnum)Enum.Parse(typeof(orderTypeEnum), s[11].Trim(), true);
			}
			catch (Exception xe)
			{
				Console.WriteLine(xe.Message);
			}
		}
		static public String Headings()
		{
			return "Date,Venue,Start,MarketID,SelectionID,MarketType,Side,Account,Resubmitted,OrderType,Price)";
		}
		public override string ToString()
		{
			return String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}",
				Date,
				Venue,
				Start,
				MarketID,
				SelectionID,
				MarketType,
				Side,
				Exchange,
				Resubmitted,
				OrderType,
				Price);
		}
	}
}
