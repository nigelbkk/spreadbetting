using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNet.SignalR.Client;
using BetfairAPI;
using System.Threading.Tasks;
using Betfair.ESAClient.Cache;
using Newtonsoft.Json;

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
		private String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; NotifyPropertyChanged(""); } }
		private bool _SubscribedOrders = false;
		private bool _Connected { get { return !String.IsNullOrEmpty(hubConnection.ConnectionId); } }
		public bool IsConnected { get { return _Connected; } }
		public Brush StreamingColor { get { return IsConnected ? Brushes.LightGreen : Brushes.Ivory; } }
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		public BetsManager()
		{
			string url = "http://88.202.183.202:8088";
			//			url = "http://192.168.1.6:8088";
			//			url = "http://127.0.0.1:8088";
			hubConnection = new HubConnection(url);
			hubConnection.TraceLevel = TraceLevels.None;
			hubConnection.TraceWriter = Console.Error;
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubProxy.On<string>("ordersChanged", (json) =>
			{
				Debug.WriteLine(json);
				Task.Run(() =>
				{
					OrderMarketSnap Snap = JsonConvert.DeserializeObject<OrderMarketSnap>(json);
					Debug.WriteLine(Snap.MarketId);
				});
			});

			Rows = new ObservableCollection<Row>();

			Rows.Add(new Row() { Runner = "George Baker 1" });
			Rows.Add(new Row() { Runner = "George Baker 2" });
			Rows.Add(new Row() { Runner = "George Baker 3" });
			Rows.Add(new Row() { Runner = "George Baker 4" });
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
		private void Connect()
		{
			hubConnection.Start().ContinueWith(task =>
			{
				if (OnFail(task))
				{
					Status = "Failed to Connect";
					return;
				}
				Status = "Connected";
			}).Wait(2000);
		}
		private void Disconnect()
		{
			hubConnection.Stop(new TimeSpan(1000));
			Status = "Disconnected";
		}
		private bool OnFail(Task task)
		{
			if (task.IsFaulted)
			{
				Debug.WriteLine("Exception:{0}", task.Exception.GetBaseException());
			}
			return task.IsFaulted;
		}
		private void SendMessage(String msg)
		{
			hubProxy.Invoke<String>("Send", msg).ContinueWith(task =>
			{
				Debug.Assert(!task.IsFaulted);
			}).Wait();
		}
		private bool RemoteCall(String name)
		{
			bool retval = true;
			hubProxy.Invoke<String>(name).ContinueWith(task => { retval = OnFail(task); }).Wait();
			return retval;
		}
		private bool RemoteCall(String name, String arg)
		{
			bool retval = true;
			hubProxy.Invoke<String>(name, arg).ContinueWith(task => { retval = OnFail(task); }).Wait();
			return retval;
		}
		private void SubscribeOrders()
		{
			bool success = !RemoteCall(_SubscribedOrders ? "UnsubscribeOrders" : "SubscribeOrders");
			if (success)
			{
				_SubscribedOrders = !_SubscribedOrders;
				Status = _SubscribedOrders ? "Subscribed" : "Unsubscribed";
			}
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
				case "Stream": if (IsConnected) Disconnect(); else Connect(); break;
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
						if (MarketNode != null)
						{
							Debug.WriteLine("cancel all for {0} {1}", MarketNode.MarketID, MarketNode.FullName);
							//Betfair.cancelOrders(MarketNode.MarketID, instructions); 
						}
					}
					break;
			}
		}
	}
}
