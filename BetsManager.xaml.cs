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
using System.IO;
using System.Windows.Media;
using System.Timers;

namespace SpreadTrader
{
	public class Row : INotifyPropertyChanged
	{
		private Properties.Settings props = Properties.Settings.Default;
		public static Dictionary<long, String> RunnerNames = new Dictionary<long, string>();

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public DateTime _Time { get; set; }
		public DateTime Time { get { return _Time.AddHours(props.TimeOffset); } set { _Time = value;  } }
		public String MarketID { get; set; }
		public long SelectionID { get; set; }
		public UInt64 BetID { get; set; }
		public bool IP { get; set; }
		public bool SP { get; set; }
		public String Runner { get; set; }
		public String Side { get; set; }
		public double _Stake { get; set; }
		public double Stake { get { return _Stake; } set { _Stake = value; NotifyPropertyChanged(""); } }
		public double Odds { get; set; }
		public double DisplayOdds { get { return IsMatched ? AvgPriceMatched : Odds; } }
		public double DisplayStake { get { return IsMatched ? Matched : Stake-Matched; } }
		public double AvgPriceMatched { get; set; }
		public double Profit { get { return Math.Round(DisplayStake * (DisplayOdds - 1), 5); } }
		public double _Matched { get; set; }
		public double Matched { get { return _Matched; } set { _Matched = value; NotifyPropertyChanged(""); } }
		public bool IsMatched { get { return _Matched > 0; } }
		public String IsMatchedString { get { return IsMatched ? "F" : "U"; } }
		public bool IsBack { get { return Side.ToUpper() == "BACK"; } }
		private bool _Hidden = false;
		public bool Hidden { get { return _Hidden; } set { _Hidden = value; NotifyPropertyChanged(""); } }
		public bool Override { get; set; }
		public Row(String id)
		{
			BetID = Convert.ToUInt64(id);
			Time = DateTime.Now;
		}
		public Row(Order o)
		{
			Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
			Odds = o.P.Value;
			Stake = (Int32) o.S.Value;
			Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
			BetID = Convert.ToUInt64(o.Id);
		}
		public Row(CurrentOrderSummaryReport.CurrentOrderSummary o)
		{
			Time = o.placedDate;
			BetID = o.betId;
			SelectionID = o.selectionId;
			Side = o.side;
			Stake = (Int32) o.priceSize.size;
			Odds = o.priceSize.price;
			AvgPriceMatched = o.averagePriceMatched;
			Matched = o.sizeMatched;
			MarketID = o.marketId;
		}
		public override string ToString()
		{
			return String.Format("{0},{1},{2},{3},{4}", Runner, SelectionID, Odds, Matched, BetID.ToString());
		}
	}
	public partial class BetsManager : UserControl, INotifyPropertyChanged
	{
		private Properties.Settings props = Properties.Settings.Default;
		public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public RunnersControl RunnersControl { get; set; }
		public ObservableCollection<Row> Rows { get; set; }
		private NodeViewModel MarketNode { get; set; }
		private DateTime _LastUpdated { get; set; }
		private BetfairAPI.BetfairAPI Betfair { get; set; }
		private bool _StreamActive { get; set; }
		public bool StreamActive { get { return _StreamActive; } set { _StreamActive = value; NotifyPropertyChanged(""); } }
		private Timer timer = new Timer();
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		public Int32 DebugID { get; set; }
		public double MatchAmount { get; set; }
		public bool UnmatchedOnly { get; set; }
		public String LastUpdated { get { return String.Format("Orders last updated {0}", _LastUpdated.AddHours(props.TimeOffset).ToString("HH:mm:ss"));	}  }
		public event PropertyChangedEventHandler PropertyChanged;
		private String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; NotifyPropertyChanged(""); } }
		private bool _SubscribedOrders = false;
		private bool _Connected { get { return !String.IsNullOrEmpty(hubConnection.ConnectionId); } }
		public bool IsConnected { get { return _Connected; } }
		public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
		public String StreamingButtonText { get { return IsConnected ? "Streaming Connected" : "Streaming Disconnected"; } }
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private Row FindRow(String id, bool matched)
		{
			if (Rows.Count > 0) foreach (Row r in Rows)
			{
				if (r.BetID == Convert.ToUInt64(id))
				{
					//if (r.IsMatched == matched)
					return r;
				}
			}
			return null;
		}
		public BetsManager()
		{
			hubConnection = new HubConnection("http://"+props.StreamUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				BackgroundWorker bw = new BackgroundWorker();
				bw.DoWork += (o, e) =>
				{
					OrderMarketSnap snap = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
					if (MarketNode != null && snap.MarketId == MarketNode.MarketID)
					{
						Dispatcher.BeginInvoke(new Action(() => OnOrderChanged(json1)));
					}
				};
				bw.RunWorkerAsync();
			});
			timer.Elapsed += (o, e) =>
			{
				StreamActive = false;
				timer.Stop();
			};
			timer.Interval = 10000;
			timer.Enabled = true;
			timer.Start();

			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				StreamActive = true;
				timer.Start();
			};

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
		private void OnOrderChanged(String json)
		{
			if (String.IsNullOrEmpty(json))
				return;
			
			OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

			if (change.Orc == null)
				return;

			_LastUpdated = DateTime.UtcNow;
			try
			{
				if (change.Closed == true)
				{
					Debug.WriteLine("market closed");
					Rows.Clear();
				}

				if (change.Orc.Count > 0) foreach (OrderRunnerChange orc in change.Orc)
				{
					if (orc.Uo == null)
						continue;

					if (orc.Uo.Count > 0) foreach (Order o in orc.Uo)
					{
						Debug.Assert(o.Status == Order.StatusEnum.E || o.Status == Order.StatusEnum.Ec);
						Row row = FindRow(o.Id, false);
						if (row == null)
						{
							row = new Row(o) { MarketID = MarketNode.MarketID, SelectionID = orc.Id.Value };
							Rows.Insert(0, row);
						}
						row.Runner = MarketNode.GetRunnerName(row.SelectionID);

						if (String.IsNullOrEmpty(row.Runner))
						{
							Debug.WriteLine(row.ToString(), "Invalid runner in BetsManager");
						}

						if (o.Sm == 0 && o.Sr > 0)                          // unmatched
						{
							row.Stake = o.S.Value;
							row.Matched = o.Sm.Value;
						}
						if (o.Sm == o.S && o.Sr == 0)                       // fully matched
						{
							if (Math.Round(o.Avp.Value, 2) != o.Avp.Value)
							{
								Debug.WriteLine(row.ToString(), "Too many decimals in fully matched bet");
							}
							row.AvgPriceMatched = o.Avp.Value;
							row.Matched = o.Sm.Value;
						}
						if (o.Sm > 0 && o.Sr > 0)                           // partially matched
						{
							Row mrow = new Row(o);
							mrow.Matched = o.Sm.Value;
							mrow.AvgPriceMatched = o.Avp.Value;
							Rows.Insert(0, mrow);
							row.Stake = o.Sr.Value;
							if (Math.Round(o.Avp.Value, 2) != o.Avp.Value)
							{
								Debug.WriteLine(row.ToString(), "Too many decimals in partially matched bet");
							}
						}
						row.Hidden = UnmatchedOnly && row.Matched > 0;

						if (o.Sc > 0)										// cancelled
						{
							Rows.Remove(row);
						}
					}
				}
				NotifyPropertyChanged("");
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void PopulateDataGrid()
		{
			_LastUpdated = DateTime.UtcNow;
			if (MarketNode != null)
			{
				if (Betfair == null)
				{
					Betfair = MainWindow.Betfair;
				}
				Rows = new ObservableCollection<Row>();

				if (MarketNode.MarketID != null)
				{
					try
					{
						CurrentOrderSummaryReport report = Betfair.listCurrentOrders(MarketNode.MarketID); // "1.185904913"

						if (report.currentOrders.Count > 0) foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
						{
							OrdersStatic.BetID2SelectionID[o.betId] = o.selectionId;
							Row.RunnerNames[o.selectionId] = RunnersControl.GetRunnerName(o.selectionId);
							Rows.Insert(0, new Row(o)
							{
								Runner = RunnersControl.GetRunnerName(o.selectionId),
						});
						}
						NotifyPropertyChanged("");
					}
					catch (Exception xe) 
					{ 
						Debug.WriteLine(xe.Message);
						Dispatcher.BeginInvoke(new Action(() =>
						{
							Extensions.MainWindow.Status = xe.Message;
						}));
					}
				}
			}
		}
		private void RowButton_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			Row row = b.DataContext as Row;
			if (Betfair == null)
			{
				Betfair = MainWindow.Betfair;
			}
			if (row.Matched >= row.Stake )
			{
				Status = "Bet is already fully matched";
				return;
			}
			Status = "Bet cancelled";
			Debug.WriteLine("cancel {0} for {1} {2}", MarketNode.MarketID, row.BetID, row.Runner);

			DateTime LastUpdate = DateTime.UtcNow;
			Betfair.cancelOrder(MarketNode.MarketID, row.BetID);
			MarketNode.TurnaroundTime = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
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
			}).Wait(1000);
		}
		private void Disconnect()
		{
			hubConnection.Stop(new TimeSpan(2000));
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
				Betfair = MainWindow.Betfair;
			}
			Button b = sender as Button;
			switch(b.Tag)
			{
				case "Stream": if (IsConnected) Disconnect(); else Connect(); break;
				case "CancelAll":
					if (MarketNode != null)
					{
						//CancelExecutionReport report = Betfair.cancelOrders(MarketNode.MarketID, null);
					}
					BackgroundWorker bw = new BackgroundWorker();
					bw.DoWork += (o, e2) =>
					{
						if (MarketNode != null)
						{
							CancelExecutionReport report = Betfair.cancelOrders(MarketNode.MarketID, null);
							//Debug.WriteLine("status: {0}", report.status);
						}
					};
					bw.RunWorkerAsync();
					break;
			}
		}
		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			if (Rows.Count > 0) foreach (Row row in Rows)
			{
				row.Hidden = cb.IsChecked == true && row.Matched > 0;
			}
		}
		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			CheckBox_Checked(sender, e);
		}
#region Debugging aids
		private string[] lines = null;
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			lines = File.ReadAllLines("lines.json");
			MarketNode = new NodeViewModel(Betfair) { MarketID = "1.185904913" };
			PopulateDataGrid();
			// populate
		}
		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			lines = File.ReadAllLines("lines.json");
			Row row = dataGrid.SelectedItem as Row;
			if (row != null)
			{
				OnOrderChanged(lines[3].Replace("\"sr\":0.0", "\"sr\":0.25").Replace("\"sm\":2.0", "\"sm\":1.25").Replace("239904106141", row.BetID.ToString()));
			}
			// fill
		}
		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			lines = File.ReadAllLines("lines.json");
			OnOrderChanged(lines[2]);
			// cancel
		}
		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			lines = File.ReadAllLines("lines.json");
			OnOrderChanged(lines[3]);
			String fill = lines[0];
			String cancel = lines[1];
			String newbet = lines[2];
		}
#endregion
		private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Label lb = sender as Label;
			Row row = lb.DataContext as Row;
			UpdateBet ub = new UpdateBet(row);
			ub.ShowDialog();
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
