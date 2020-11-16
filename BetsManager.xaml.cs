using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
		public bool Override { get; set; }
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
		public override string ToString()
		{
			return Runner;
		}
	}
	public partial class BetsManager : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public RunnersControl RunnersControl { get; set; }
		public ObservableCollection<Row> Rows { get; set; }
		private NodeViewModel MarketNode { get; set; }
		private DateTime _LastUpdated { get; set; }
		private BetfairAPI.BetfairAPI Betfair { get; set; }
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		public bool UnmatchedOnly { get; set; }
		public String LastUpdated { get { return String.Format("Bets last updated {0}", _LastUpdated.ToShortTimeString());	}  }
		public event PropertyChangedEventHandler PropertyChanged;
		public BetsManager()
		{
			Rows = new ObservableCollection<Row>();

			Rows.Add(new Row() { Runner = "George Baker 1" });
			Rows.Add(new Row() { Runner = "George Baker 2" });
			Rows.Add(new Row() { Runner = "George Baker 3" });
			Rows.Add(new Row() { Runner = "George Baker 4" });
			InitializeComponent();
			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					PopulateDataGrid();
					Debug.WriteLine("BetsManager");
				}
			};
		}
		private void PopulateDataGrid()
		{
			_LastUpdated = DateTime.Now;
			if (MarketNode != null)
			{
				if (Betfair == null)
				{
					Betfair = new BetfairAPI.BetfairAPI();
				}
				Rows = new ObservableCollection<Row>();

				if (MarketNode.MarketID != null)
				{
					CurrentOrderSummaryReport report = Betfair.listCurrentOrders(MarketNode.MarketID); // "1.168283812"

					foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
					{
						Rows.Add(new Row(o) {
							Runner = RunnersControl.GetRunnerName(o.selectionId)
						});
					}
					NotifyPropertyChanged("");
				}
			}
		}
		private void RowButton_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			Int32 Tag = Convert.ToInt32(b.Tag)-1;
			if (Betfair == null)
			{
				Betfair = new BetfairAPI.BetfairAPI();
			}
			Debug.WriteLine("cancel {0} for {1} {2}", MarketNode.MarketID, Rows[Tag].BetID, Rows[Tag].Runner);
			//Betfair.cancelOrder(MarketNode.MarketID, Rows[Tag].BetID);
			ObservableCollection<Row> rows = new ObservableCollection<Row>();
			for(int i=0;i< Rows.Count;i++)
			{
				if (i != Tag)
					rows.Add(Rows[i]);
			}
			Rows = rows;
			NotifyPropertyChanged("");
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (Betfair == null)
			{
				Betfair = new BetfairAPI.BetfairAPI();
			}
			Button b = sender as Button;
			switch(b.Tag)
			{
				case "Refresh": PopulateDataGrid(); break;
				case "CancelAll":
					List<CancelInstruction> instructions = new List<CancelInstruction>();
					foreach(Row row in Rows)
					{
						if (!row.Override)
							instructions.Add(new CancelInstruction(row.BetID));
					}
					if (instructions.Count > 0)
					{
						Debug.WriteLine("cancel all for {0} {1}", MarketNode.MarketID, MarketNode.FullName);
						//Betfair.cancelOrders(MarketNode.MarketID, instructions); 
					}
					break;
			}
		}
	}
}
