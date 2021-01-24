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
using System.IO;
using System.Windows.Media;
using System.Timers;

namespace SpreadTrader
{
	public class Row : INotifyPropertyChanged
	{
		public static Dictionary<long, String> RunnerNames = new Dictionary<long, string>();

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
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
				//				double p = Side.ToLower() == "back" ? Stake * (Odds - 1) : Stake;
				double p = Matched * (Odds - 1);
				if (Matched == 0)
					p = Stake * (Odds - 1);
				return Math.Round(p, 2);
			}
		}
		public double _Matched { get; set; }
		public double Matched { get { return _Matched; } set { _Matched = value; NotifyPropertyChanged("Matched"); } }
		public bool IsMatched { get { return _Matched > 0; } }
		public bool IsBack { get { return Side == "Back"; } }
		private bool _Hidden = false;
		public bool Hidden { get { return _Hidden; } set { _Hidden = value; NotifyPropertyChanged(""); } }
		public bool Override { get; set; }
		public Row(String id)
		{
			BetID = Convert.ToUInt64(id);
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
			Matched = o.sizeMatched;
		}
		public override string ToString()
		{
			return Runner;
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
		public String LastUpdated { get { return String.Format("Orders last updated {0}", _LastUpdated.ToString("HH:mm:ss"));	}  }
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
			hubConnection = new HubConnection("http://"+props.StreamUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				try
				{
					using (StreamWriter w = File.AppendText("json.csv"))
					{
						w.WriteLine(json1);
					}
				}
				catch (Exception xe) { Debug.WriteLine(xe.Message); }

				Task.Run(() =>
				{
					OrderMarketSnap snap = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
					if (MarketNode != null && snap.MarketId == MarketNode.MarketID)
					{
						Dispatcher.BeginInvoke(new Action(() => OnOrderChanged(json1)));
					}
				});
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

				//this.Dispatcher.Invoke(() =>
				//{
				//	NotifyPropertyChanged("");
				//});
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
		private void OnOrderChanged(String json1)
		{
			OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json1);
			_LastUpdated = DateTime.Now;
			NotifyPropertyChanged("");
			try
			{
				if (change.Closed == true)
				{
					Debug.WriteLine("market closed");
					Rows.Clear();
				}
				//if (change.Id != "1.177854952")
				//{
				//	return;	///DEBUG ONLY
				//}
				if (change.Orc != null)
				{
					foreach (OrderRunnerChange orc in change.Orc)
					{
						if (orc.Uo != null)
						{
							foreach (Order o in orc.Uo)
							{
								if (o.Status == Order.StatusEnum.E) // new order
								{
									Row row = FindRow(o.Id);
									if (row == null)
									{
										row = new Row(o.Id);
										Debug.WriteLine("new unmatched");
										Rows.Insert(0, row);
									}
									else
									{
										Debug.WriteLine("existing unmatched");
									}
									row.SelectionID = orc.Id.Value;
									row.Runner = MarketNode.GetRunnerName(row.SelectionID);
//									row.BetID = Convert.ToUInt64(o.Id);
									row.Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
									row.Odds = o.P.Value;
									row.Stake = o.S.Value;
									row.Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
								}
								if (o.Status == Order.StatusEnum.Ec) // order changed
								{
									if (o.Sc > 0) // order canceled
									{
										Debug.WriteLine("canceled");
										Row row = FindRow(o.Id);
										Rows.Remove(row);
									}
									else if (o.Sl > 0) // order lapsed
									{
										Debug.WriteLine("lapsed");
										Row row = FindRow(o.Id);
										Rows.Remove(row);
									}
									else if (o.Sr == 0) // fully matched
									{
										Row row = FindRow(o.Id);
										if (row == null)
										{
											row = new Row(o.Id);
											Rows.Insert(0, row);
										}
										row.SelectionID = orc.Id.Value;
										row.Runner = MarketNode.GetRunnerName(row.SelectionID);
										row.Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Md.Value).ToLocalTime();
										row.Odds = o.Avp.Value;
										row.Stake = o.S.Value;
										row.Matched = o.Sm.Value;
										row.NotifyPropertyChanged("");
										Debug.WriteLine("fully matched");
									}
									else
									{
										if (orc.Mb != null)
										{
											foreach (var mb in orc.Mb)
											{
												Row row = FindRow(o.Id);
												if (row == null)
												{
													row = new Row(o.Id);
													Rows.Insert(0, row);
												}
												row.SelectionID = orc.Id.Value;
												row.Runner = MarketNode.GetRunnerName(row.SelectionID);
//												row.BetID = Convert.ToUInt64(o.Id);
												row.Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Md.Value).ToLocalTime();
												row.Odds = o.Avp.Value;
												row.Stake = o.S.Value;
												row.Matched = o.Sm.Value;
												row.Side = "Back";
												Debug.WriteLine("partially matched");
											}
										}
										if (orc.Ml != null)
										{
											foreach (var ml in orc.Ml)
											{
												Row row = FindRow(o.Id);
												if (row == null)
												{
													row = new Row(o.Id);
													Rows.Insert(0, row);
												}
												row.SelectionID = orc.Id.Value;
												row.Runner = MarketNode.GetRunnerName(row.SelectionID);
//												row.BetID = Convert.ToUInt64(o.Id);
												row.Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Md.Value).ToLocalTime();
												row.Odds = o.Avp.Value;
												row.Stake = o.S.Value;
												row.Matched = o.Sm.Value;
												row.Side = "Lay";
												Debug.WriteLine("partially matched");
											}
										}
									}
								}
							}
						}
					}
				}
				MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
			}
			catch(Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void PopulateDataGrid()
		{
			_LastUpdated = DateTime.Now;
			if (MarketNode != null)
			{
				if (Betfair == null)
				{
					Betfair = MainWindow.Betfair;// new BetfairAPI.BetfairAPI();
				}
				Rows = new ObservableCollection<Row>();

				if (MarketNode.MarketID != null)
				{
					try
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
					catch (Exception xe) 
					{ 
						Debug.WriteLine(xe.Message);
						Dispatcher.BeginInvoke(new Action(() =>
						{
							MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
							if (mw != null) mw.Status = xe.Message;
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
				Betfair = MainWindow.Betfair;//new BetfairAPI.BetfairAPI();
			}
			if (row.Matched >= row.Stake )
			{
				Status = "Bet is already fully matched";
				return;
			}
			Status = "Bet canceled";
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
				Betfair = MainWindow.Betfair;//new BetfairAPI.BetfairAPI();
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
							DateTime LastUpdate = DateTime.UtcNow;
							Betfair.cancelOrders(MarketNode.MarketID, instructions);
							MarketNode.TurnaroundTime = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
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
		private string[] lines = null;
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			lines = File.ReadAllLines("json.csv");
			DebugID = 100;
			Rows.Clear();
			NotifyPropertyChanged("");
		}
		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			if (lines == null)
				Button_Click_1(sender, e);

			if (DebugID < lines.Length)
				OnOrderChanged(lines[DebugID++]);

			NotifyPropertyChanged("");
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
