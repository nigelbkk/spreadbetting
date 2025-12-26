using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Betfair.ESASwagger.Model;

namespace SpreadTrader
{
    public class BetsManagerRow : INotifyPropertyChanged
	{
		private Properties.Settings props = Properties.Settings.Default;
		public static Dictionary<long, String> RunnerNames = new Dictionary<long, string>();

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		
		// properties

		private DateTime _Time;
		public DateTime Time { get => _Time.AddHours(props.TimeOffset); set { _Time = value; } }
		public long? UTCTime;
		public String MarketID;
		public long SelectionID;


		public UInt64 BetID { get; set; }
		public String Runner { get; set; }
		public String Side { get; set; }
		public double Stake { get; set; }
		public double Odds;// { get; set; }

		// properties

		public bool IsBack { get => Side.ToUpper() == "BACK"; }

		private bool _Hidden = false;
		public bool Hidden
		{
			get => _Hidden; set { _Hidden = value; OnPropertyChanged(""); }
		}

		private bool _Override;
		public bool Override { get => _Override; set { _Override = value; OnPropertyChanged(nameof(Override)); } }
		private bool _NoCancel { get; set; }
		public bool NoCancel { get => _NoCancel; set { _NoCancel = value; OnPropertyChanged(nameof(NoCancel)); } }
		public String IsMatchedString { get => SizeMatched > 0 ? "F" : "U"; }
		public double DisplayOdds { get => SizeMatched > 0 ? Math.Round(AvgPriceMatched, 2) : Odds; }
		public bool IsMatched { get => SizeMatched > 0; }
		public double Profit { get => Math.Round(DisplayStake * (DisplayOdds - 1), 5); }

		// publicly accessible

		public double DisplayStake => Stake;

		public double AvgPriceMatched;

		public double SizeMatched;// { get; set; }
		
		
		
		public BetsManagerRow(BetsManagerRow r)
		{
			BetID = r.BetID;
			Stake = r.Stake;
			//OriginalStake = r.OriginalStake;
			SelectionID = r.SelectionID;
			SizeMatched = r.SizeMatched;
			MarketID = r.MarketID;
			Side = r.Side;
			Runner = r.Runner;
			Time = DateTime.Now;
		}
		public BetsManagerRow(Order o)         // new bet
		{
			Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
			UTCTime = o.Pd.Value;
			Odds = o.P.Value;
			Stake = (Int32)o.S.Value;
			//OriginalStake = Stake;
			Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
			BetID = Convert.ToUInt64(o.Id);
		}
		public BetsManagerRow(CurrentOrderSummaryReport.CurrentOrderSummary o)
		{
			Time = o.placedDate;
			BetID = o.betId;
			SelectionID = o.selectionId;
			Side = o.side;
			Stake = (Int32)o.priceSize.size;
			Odds = o.priceSize.price;
			AvgPriceMatched = o.averagePriceMatched;
			SizeMatched = o.sizeMatched;
			MarketID = o.marketId;
		}
		public override string ToString()
		{
			return String.Format("{0},{1},{2},{3},{4}", Runner, SelectionID, Odds, SizeMatched, BetID.ToString());
		}
	}
}
