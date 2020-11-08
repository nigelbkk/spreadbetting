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
			Runner = o.selectionId.ToString();
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
		public List<Row> Items { get; set; }
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
			BetfairAPI.BetfairAPI Betfair = new BetfairAPI.BetfairAPI();
			Items = new List<Row>();

			CurrentOrderSummaryReport report = Betfair.listCurrentOrders("1.168283812");

			foreach(CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
			{
				Items.Add(new Row(o));
			}
			InitializeComponent();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (MarketNode != null)
			{
			}
		}
	}
}
