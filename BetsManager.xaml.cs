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
using Newtonsoft.Json;
using Betfair.ESASwagger.Model;
using System.Media;

namespace SpreadTrader
{
	public class Row : INotifyPropertyChanged
	{
		public static Dictionary<long, String> RunnerNames = new Dictionary<long, string>();

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public DateTime Time { get; set; }
		public long SelectionID { get; set; }
		public UInt64 BetID { get; set; }
		public bool IP { get; set; }
		public bool SP { get; set; }
		public String Runner { get; set; }
		public String Side { get; set; }
		public double Stake { get; set; }
		public double Odds { get; set; }
		public double Profit { get {
				double p = Side == "Lay" ? Stake * (Odds - 1) : Stake;
				return Math.Round(p, 2);
			}
		}
		public double _Matched { get; set; }
		public double Matched { get { return _Matched; } set { _Matched = value; NotifyPropertyChanged("Matched"); } }
		private bool _Hidden = false;
		public bool Hidden { get { return _Hidden; } set { _Hidden = value; NotifyPropertyChanged(""); } }
		public bool Override { get; set; }
		public Row()
		{
			Time = DateTime.Now;
		}
		public Row(KeyValuePair<string, Order> kvp)
		{
			Order o = kvp.Value;
			BetID = Convert.ToUInt64(o.Id);
//			SelectionID = o.Md.HasValue ? o.Md.Value : 0; 
			Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
			Stake = o.Sr.HasValue ? o.Sr.Value : 0;
			Odds = o.P.HasValue ? o.P.Value : 0;
			//Profit = o.Side == Order.SideEnum.L ? o.S.Value * (o.P.Value - 1) : o.S.Value;
			//Profit = Math.Round(Profit, 2);
			Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
		}
		public Row(CurrentOrderSummaryReport.CurrentOrderSummary o)
		{
			Time = o.placedDate;
			BetID = o.betId;
			SelectionID = o.selectionId;
			Side = o.side;
			Stake = o.priceSize.size;
			Odds = o.priceSize.price;
			//Profit = o.side == "LAY" ? o.priceSize.size * (o.priceSize.price - 1) : o.priceSize.size;
			//Profit = Math.Round(Profit, 2);
			Matched = o.sizeMatched;
		}
		public override string ToString()
		{
			return Runner;
		}
	}
	public partial class BetsManager : UserControl, INotifyPropertyChanged
	{
		public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
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
		public String StreamingButtonText { get { return IsConnected ? "Streaming Connected" : "Streaming Disconnected"; } }
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private Row FindRow(String id)
		{
			foreach (Row r in Rows)
			{
				if (r.BetID == Convert.ToUInt64(id))
				{
					return r;
				}
			}
			return null;
		}
		public BetsManager()
		{
			string url = "http://88.202.183.202:8088";
			//			url = "http://192.168.1.6:8088";
			//			url = "http://127.0.0.1:8088";
			hubConnection = new HubConnection(url);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				Task.Run(() =>
				{
					OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json1);
					Betfair.ESAClient.Cache.OrderMarket market = JsonConvert.DeserializeObject<Betfair.ESAClient.Cache.OrderMarket> (json2);
					OrderMarketSnap snap = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
					Debug.WriteLine(snap.MarketId);
					if (MarketNode != null && snap.MarketId == MarketNode.MarketID)
					{
						Dispatcher.BeginInvoke(new Action(() => {
							try
							{
								if (change.Orc != null)
								{
									foreach (OrderRunnerChange orc in change.Orc)
									{
										if (orc.Uo != null)
										{
											foreach (Order o in orc.Uo)
											{
												Row row = FindRow(o.Id);
												if (o.Status == Order.StatusEnum.Ec && o.Sc > 0 && o.Sm == 0) // it was cancelled
												{
													Rows.Remove(row);
													Debug.WriteLine("canceled");
												}
												else if (row == null)
												{
													row = new Row();
													row.SelectionID = orc.Id.Value;
													row.Runner = MarketNode.GetRunnerName(row.SelectionID);
													row.BetID = Convert.ToUInt64(o.Id);
													row.Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
													row.Odds = o.P.Value;
													row.Stake = o.S.Value;
													row.Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
													Rows.Add(row);
													Debug.WriteLine("new row");
												}
												else if (o.Sm > 0)      // it was matched
												{
													Debug.WriteLine("matched");
													(new SoundPlayer(Properties.Settings.Default.MatchedBetAlert)).PlaySync();
												}
											}
										}
									}
								}
								MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
								foreach (OrderMarketRunnerSnap runner in snap.OrderMarketRunners)
								{
									Row row = null;
									foreach (KeyValuePair<String, Order> kvp in runner.UnmatchedOrders)
									{
										Order o = kvp.Value;
										row = FindRow(o.Id);

										if (row != null)
										{
											foreach (PriceSize mb in runner.MatchedBack)
											{
												if (row != null && row.Side.ToUpper() == "BACK")
												{
													row.Matched += mb.size;
													if (mw != null) mw.UpdateAccountInformation();
												}
											}
											foreach (PriceSize ml in runner.MatchedLay)
											{
												if (row != null && row.Side.ToUpper() == "LAY")
												{
													row.Matched += ml.size;
													if (mw != null) mw.UpdateAccountInformation();
												}
											}
										}
									}
								}
							}
							catch(Exception xe)
							{
							}
						}));
					}
				});
			});
			Rows = new ObservableCollection<Row>();
			InitializeComponent();
			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					PopulateDataGrid();
				}
			};
			Connect();
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
						OrdersStatic.BetID2SelectionID[o.betId] = o.selectionId;
						Row.RunnerNames[o.selectionId] = RunnersControl.GetRunnerName(o.selectionId);
						Rows.Add(new Row(o)
						{
							Runner = RunnersControl.GetRunnerName(o.selectionId),
						});
					}
					NotifyPropertyChanged("");
				}
			}
		}
		private void RowButton_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			Row row = b.DataContext as Row;
			if (Betfair == null)
			{
				Betfair = new BetfairAPI.BetfairAPI();
			}
			if (row.Matched >= row.Stake )
			{
				Status = "Bet is already fully matched";
				return;
			}
			Status = "Bet canceled";
			Debug.WriteLine("cancel {0} for {1} {2}", MarketNode.MarketID, row.BetID, row.Runner);
			Betfair.cancelOrder(MarketNode.MarketID, row.BetID);
			Rows.Remove(row);
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
				case "CancelAll":
					List<CancelInstruction> instructions = new List<CancelInstruction>();
					foreach(Row row in Rows)
					{
						if (!row.Override && row.BetID > 0)
							instructions.Add(new CancelInstruction(row.BetID));
					}
					if (instructions.Count > 0)
					{
						if (MarketNode != null)
						{
							Debug.WriteLine("cancel all for {0} {1}", MarketNode.MarketID, MarketNode.FullName);
							Betfair.cancelOrders(MarketNode.MarketID, instructions);
							Status = "Canceled all unmatched";
						}
					}
					break;
			}
		}
		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			foreach(Row row in Rows)
			{
				row.Hidden = cb.IsChecked == true && row.Matched > 0;
			}
		}
		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			CheckBox_Checked(sender, e);
		}
	}
	public class OrderMarketSnap
	{
		public string MarketId { get; set; }
		public bool IsClosed { get; set; }
		public IEnumerable<OrderMarketRunnerSnap> OrderMarketRunners { get; set; }
	}
	public class OrderMarketRunnerSnap
	{
		public IList<PriceSize> MatchedLay { get; set; }
		public IList<PriceSize> MatchedBack { get; set; }
		public Dictionary<string, Order> UnmatchedOrders { get; set; }
	}
}
