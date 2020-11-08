using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BetfairAPI;

namespace SpreadTrader
{
	public class Row : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public DateTime Time { get; set; }
		public Int64 SelectionID { get; set; }
		public UInt64 BetID { get; set; }
		public bool IP { get; set; }
		public bool SP { get; set; }
		public String Runner { get; set; }
		public String Side { get; set; }
		public double Stake { get; set; }
		public double Odds { get; set; }
		public double Profit { get; set; }
		public double Matched { get; set; }
		public Row()
		{
			Time = DateTime.Now;
		}
		public Row(CurrentOrderSummaryReport.CurrentOrderSummary o)
		{
			Time = o.placedDate;
			BetID = o.betId;
			SelectionID = o.selectionId;
			Side = o.side;
			Stake = o.priceSize.size;
			Odds = o.priceSize.price;
			Profit = o.side == "LAY" ? o.priceSize.size * (o.priceSize.price - 1) : o.priceSize.size;
			Profit = Math.Round(Profit, 2);
			Matched = o.sizeMatched;
		}
	}
	public partial class BetsManager : UserControl, INotifyPropertyChanged
	{
		public NodeViewModel MarketNode { get; set; }
		public List<Row> Rows { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		public BetsManager()
		{
			InitializeComponent();
		}
		private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (MarketNode != null)
			{
				BetfairAPI.BetfairAPI Betfair = new BetfairAPI.BetfairAPI();
				Rows = new List<Row>();

				CurrentOrderSummaryReport report = Betfair.listCurrentOrders(MarketNode.MarketID); // "1.168283812"

				foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
				{
					Rows.Add(new Row(o));
				}
				foreach (Row row in Rows)
				{
					foreach(LiveRunner r in MarketNode.LiveRunners)
					{
						if (r.selectionId == row.SelectionID)
						{
							row.Runner = r.name;
						}
					}
				}
				e.Handled = true;
				NotifyPropertyChanged("");
			}
		}
	}
}
