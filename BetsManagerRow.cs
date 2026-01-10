using Betfair.ESASwagger.Model;
using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using static System.Windows.Forms.AxHost;

namespace SpreadTrader
{
    public class BetsManagerRow : INotifyPropertyChanged
	{

		private Properties.Settings props = Properties.Settings.Default;
		public static Dictionary<long, String> RunnerNames = new Dictionary<long, string>();

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(String name)
		{
			if (_suspend > 0)
			{
				_dirty = true;
				return;
			}
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private int _suspend;
		private bool _dirty;

		public void BeginUpdate()
		{
			_suspend++;
		}

		public void EndUpdate()
		{
			if (--_suspend == 0 && _dirty)
			{
				_dirty = false;
				OnPropertyChanged("");
			}
		}
		private DateTime _Time;
		public DateTime Time { get => _Time.AddHours(props.TimeOffset); set { _Time = value; } }
		public long? UTCTime;
		public String MarketID;
		public long SelectionID;

		public UInt64 BetID { get; set; }
		private String _Runner;
		public String Runner { get => _Runner; set { if (_Runner != value) { _Runner = value; OnPropertyChanged(nameof(Runner)); } } }
		private String _Side;
		public String Side { get => _Side; set { if (_Side != value) { _Side = value; OnPropertyChanged(nameof(Side)); } } }
		private double _Stake;
		public double Stake { get => _Stake; set { if (_Stake != value) { _Stake = value; OnPropertyChanged(nameof(Stake)); } } }

		private double _Odds;
		public double Odds { get => _Odds; set { if (_Odds != value) { _Odds = value; OnPropertyChanged(nameof(Odds)); RaiseDerived(); } } }
		public bool IsBack { get => Side.ToUpper() == "BACK"; }

		private bool _Hidden = false;
		public bool Hidden { get => _Hidden; set { if (_Hidden != value) { _Hidden = value; OnPropertyChanged(nameof(Odds)); RaiseDerived(); } } }

		private bool _Override;
		public bool Override { get => _Override; set { _Override = value; OnPropertyChanged(nameof(Override)); RaiseDerived(); } }
		private bool _NoCancel { get; set; }
		public bool NoCancel { get => _NoCancel; set { _NoCancel = value; OnPropertyChanged(nameof(Hidden)); RaiseDerived(); } }
		public String IsMatchedString { get => SizeMatched > 0 ? "F" : "U"; }
		public double DisplayOdds { get => SizeMatched > 0 ? Math.Round(AvgPriceMatched, 2) : Odds; }
		public bool IsMatched { get => SizeMatched > 0; }
		public double Profit { get => Math.Round(DisplayStake * (DisplayOdds - 1), 5); }

		public double DisplayStake => Stake;

		private double _AvgPriceMatched;
		public double AvgPriceMatched { get => _AvgPriceMatched; set { if (_AvgPriceMatched != value) { _AvgPriceMatched = value; RaiseDerived(); } } }

		private double _SizeMatched;
		public double SizeMatched { get => _SizeMatched; set { if (_SizeMatched != value) { _SizeMatched = value; RaiseDerived(); } } }

		private void RaiseDerived()
		{
			OnPropertyChanged(nameof(IsMatched));
			OnPropertyChanged(nameof(IsMatchedString));
			OnPropertyChanged(nameof(DisplayOdds));
			OnPropertyChanged(nameof(Profit));
		}

		public BetsManagerRow(BetsManagerRow r)
		{
			BetID = r.BetID;
			Stake = r.Stake;
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
