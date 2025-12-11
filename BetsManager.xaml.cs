using Betfair.ESASwagger.Model;
using BetfairAPI;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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
		public DateTime Time { get { return _Time.AddHours(props.TimeOffset); } set { _Time = value; } }
		public String MarketID { get; set; }
		public long SelectionID { get; set; }
		public UInt64 BetID { get; set; }
		public bool IP { get; set; }
		public bool SP { get; set; }
		public String Runner { get; set; }
		public String Side { get; set; }
		public double _Stake { get; set; }
		public double OriginalStake { get; set; }
		public double Stake { get { return _Stake; } set { _Stake = value; NotifyPropertyChanged(""); } }
		public double Odds { get; set; }
		//        public double DisplayOdds { get { return Odds; } }// IsFullyMatched || IsPartiallyMatched ? AvgPriceMatched : Odds; } }
		public double DisplayOdds { get { return SizeMatched > 0 ? Math.Round(AvgPriceMatched, 2) : Odds; } }
		public double DisplayStake
		{
			get
			{
				return Stake;
			}
		}
		public double AvgPriceMatched { get; set; }
		public double Profit { get { return Math.Round(DisplayStake * (DisplayOdds - 1), 5); } }
		public double _SizeMatched { get; set; }
		public double SizeMatched { get { return _SizeMatched; } set { _SizeMatched = value; NotifyPropertyChanged(""); } }
		public bool IsMatched { get { return SizeMatched > 0; } }
		public String IsMatchedString { get { return SizeMatched > 0 ? "F" : "U"; } }
		public bool IsBack { get { return Side.ToUpper() == "BACK"; } }
		private bool _Hidden = false;
		public bool Hidden { get { return _Hidden; } set { _Hidden = value; NotifyPropertyChanged(""); } }
        public bool Override { get; set; }
        public bool NoCancel { get; set; }
		public Row(String id)
		{
			BetID = Convert.ToUInt64(id);
			Time = DateTime.Now;
		}
		public Row(Row r)
		{
			BetID = r.BetID;
			OriginalStake = r.OriginalStake;
			SelectionID = r.SelectionID;
			SizeMatched = r.SizeMatched;
			MarketID = r.MarketID;
			Side = r.Side;
			Runner = r.Runner;
			Time = DateTime.Now;
		}
		public Row(Order o)         // new bet
		{
			Time = new DateTime(1970, 1, 1).AddMilliseconds(o.Pd.Value).ToLocalTime();
			Odds = o.P.Value;
			Stake = (Int32)o.S.Value;
			OriginalStake = Stake;
			//AmountRemaining = Stake;
			Side = o.Side == Order.SideEnum.L ? "Lay" : "Back";
			BetID = Convert.ToUInt64(o.Id);
		}
		public Row(CurrentOrderSummaryReport.CurrentOrderSummary o)
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

	public partial class BetsManager : UserControl, INotifyPropertyChanged
	{
		public OnShutdownDelegate OnShutdown;
		private Queue<String> incomingOrdersQueue = new Queue<String>();
		private Queue<UInt64> cancellation_queue = new Queue<UInt64>();

		private Properties.Settings props = Properties.Settings.Default;
		public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
		public MarketSelectionDelegate OnMarketSelected;
		public FavoriteChangedDelegate OnFavoriteChanged;
		public SubmitBetsDelegate OnSubmitBets;
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
		public String LastUpdated { get { return String.Format("Orders last updated {0}", _LastUpdated.AddHours(props.TimeOffset).ToString("HH:mm:ss")); } }
		public event PropertyChangedEventHandler PropertyChanged;
		private String _Status = "Ready";
		public String Status
		{
			get { return _Status; }
			set
			{
				_Status = value;
				Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = value; }));
				//Extensions.MainWindow.Status = value;
			}
		}
		private String _Notification = "";
		public String Notification
		{
			get { return _Notification; }
			set
			{
				_Notification = value;
				Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Notification = value; }));
				//Extensions.MainWindow.Status = value;
			}
		}
		private bool _Connected { get { return !String.IsNullOrEmpty(hubConnection?.ConnectionId); } }
		public bool IsConnected { get { return _Connected; } }
		public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
		public String StreamingButtonText { get { return IsConnected ? "Streaming Connected" : "Streaming Disconnected"; } }
		private IHubProxy hubProxy = null;
		private HubConnection hubConnection = null;
		private Row FindUnmatchedRow(String id)
		{
			if (Rows.Count > 0) foreach (Row r in Rows)
				{
					if (r.BetID == Convert.ToUInt64(id))
					{
						if (r.SizeMatched == 0)
							return r;
					}
				}
			return null;
		}
		private Row FindUnmatchedRow(LiveRunner lr)
		{
			if (Rows.Count > 0) foreach (Row r in Rows)
				{
					if (r.SelectionID == lr.SelectionId)
					{
						if (r.SizeMatched == 0)
							return r;
					}
				}
			return null;
		}
		private void ProcessIncomingNotifications(object o)
		{
			BackgroundWorker sender = o as BackgroundWorker;
			while (!sender.CancellationPending)
			{
				while (incomingOrdersQueue.Count > 0)
				{
					try
					{
						//Debug.WriteLine("Fetch from queue");
						//lock (incomingOrdersQueue) 
						{ 
							String json = incomingOrdersQueue.Dequeue();
							OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json);
							OnOrderChanged(json);
						}
					}
					catch (Exception xe)
					{
						Status = xe.Message;
					}
				}
				System.Threading.Thread.Sleep(10);
			}
		}
		private void ProcessCancellationQueue(object o)
		{
			BackgroundWorker sender = o as BackgroundWorker;
			while (!sender.CancellationPending)
			{
				while (cancellation_queue.Count > 0)
				{
					try
					{
						//lock (cancellation_queue)
						{
							UInt64 betid = cancellation_queue.Dequeue();

							//Debug.WriteLine("submit cancel {0}", betid);
							CancelExecutionReport report = Betfair.cancelOrder(MarketNode.MarketID, betid, null);
							if (report.errorCode == null)
							{
								Debug.WriteLine("bet is cancelled {0}", betid);
							}
							Status = report.errorCode != null ? report.errorCode : report.status;
						}
					}
					catch (Exception xe)
					{
						Debug.WriteLine(xe.Message);
						Status = xe.Message;
					}
				}
				System.Threading.Thread.Sleep(10);
			}
		}
		public BetsManager()
		{
			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += (o, e) => ProcessIncomingNotifications(o);
			bw.RunWorkerAsync();

			BackgroundWorker bw2 = new BackgroundWorker();
			bw2.DoWork += (o, e) => ProcessCancellationQueue(o);
			bw2.RunWorkerAsync();

			hubConnection = new HubConnection("http://" + props.StreamUrl);
			hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

			hubConnection.Reconnecting += () =>
			{
				Debug.WriteLine("[SignalR] Reconnecting...");
				Connect();
			};

			hubConnection.Reconnected += () =>
			{
				Debug.WriteLine("[SignalR] Reconnected!");
				Connect();
			};

			hubConnection.Closed += () =>
			{
				Debug.WriteLine("[SignalR] Closed — manual reconnect required.");
				Connect();
			};

			hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
			{
				OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json1);
				OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
				if (MarketNode != null && snapshot.MarketId == MarketNode.MarketID)
				{
					lock (incomingOrdersQueue)
					{
						//Debug.WriteLine("Add to queue");
						incomingOrdersQueue.Enqueue(json1);
					}
				}
			});

			hubProxy.On<string, string, string>("marketChanged", (json1, json2, json3) =>
			{
				OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
				OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json3);
				if (MarketNode != null && snapshot.MarketId == MarketNode.MarketID)
				{
					//Debug.WriteLine($"marketChanged: {MarketNode.FullName}");
				}
			});

			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				try
				{
					StreamActive = true;
					timer.Start();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			};

			Rows = new ObservableCollection<Row>();
			InitializeComponent();

			OnSubmitBets += (runner, lay, back) =>
			{
				String MarketID = MarketNode.MarketID;
				long Selection = runner.SelectionId;

				Debug.WriteLine("###################################################");
				Debug.WriteLine("###                                             ###");
				StringBuilder sb = new StringBuilder(String.Format("Back: {0} for {1:c} at ", runner.Name, back[0].size));

				List<PlaceInstruction> place_instructions = new List<PlaceInstruction>();

				foreach (PriceSize p in back)
				{
					if (p.IsChecked)
					{
						sb.AppendFormat("{0:0.00}, ", p.price);
						place_instructions.Add(new PlaceInstruction()
						{
							selectionId = runner.SelectionId,
							sideEnum = sideEnum.BACK,
							orderTypeEnum = orderTypeEnum.LIMIT,
							limitOrder = new LimitOrder()
							{
								size = p.size,
								price = p.price,
								persistenceTypeEnum = persistenceTypeEnum.LAPSE,
							}
						});
					}
				}
				Debug.WriteLine(sb.ToString().TrimEnd(' ').TrimEnd(','));

				sb = new StringBuilder(String.Format("Lay : {0} for {1:c} at ", runner.Name, lay[0].size));

				foreach (PriceSize p in lay)
				{
					if (p.IsChecked)
					{
						sb.AppendFormat("{0:0.00}, ", p.price);
						place_instructions.Add(new PlaceInstruction()
						{
							selectionId = runner.SelectionId,
							sideEnum = sideEnum.LAY,
							orderTypeEnum = orderTypeEnum.LIMIT,
							limitOrder = new LimitOrder()
							{
								size = p.size,
								price = p.price,
								persistenceTypeEnum = persistenceTypeEnum.LAPSE,
							}
						});
					}
				}
				Debug.WriteLine(sb.ToString().TrimEnd(' ').TrimEnd(','));
				Debug.WriteLine("###                                             ###");
				Debug.WriteLine("###################################################");

				System.Threading.Thread t = new System.Threading.Thread(() =>
				{
					PlaceExecutionReport report = Betfair.placeOrders(MarketNode.MarketID, place_instructions);
					Status = report.status;
					//Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
				});
				t.Start();

			};

			OnFavoriteChanged += (runner) =>
			{
				Debug.WriteLine("OnFavoriteChanged");
				// Favorite = runner;
			};

			OnMarketSelected += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					Extensions.MainWindow.Commission = MarketNode.Commission;
					PopulateDataGrid();
					Debug.WriteLine($"Market Selected: {MarketNode.Name} : {MarketNode.MarketID}");
					RequestMarketSelected(MarketNode.MarketID);
				}
			};
			Connect();
		}
		private async void RequestMarketSelected(String marketid)
		{
			var http = new HttpClient();
			var url = $"http://{props.StreamUrl}/api/market/subscribe";
			var payload = new { MarketId = marketid, };
			string json = JsonConvert.SerializeObject(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await http.PostAsync(url, content);
			string responseString = await response.Content.ReadAsStringAsync();
			Console.WriteLine(responseString);
		}

		~BetsManager()
		{
			hubConnection?.Dispose();
			hubConnection?.Stop();
		}
		private object lockObj = new object();
		private void NotifyBetMatched()
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				foreach (TabItem ti in Extensions.MainWindow.TabControl.Items)
				{
					TabContent ct = ti.Content as TabContent;
					CustomTabHeader header = ti.Header as CustomTabHeader;

					if (!ti.IsSelected && ct.MarketNode != null && ct.MarketNode.MarketID == MarketNode.MarketID)
					{
						header.OnMatched();                     // change the tab color
						break;
					}
				}
			}));
			if (!String.IsNullOrEmpty(props.MatchedBetAlert))
			{
				SoundPlayer snd = new SoundPlayer(props.MatchedBetAlert);
				snd.Play();
			}
		}
		private void OnOrderChanged(String json)
		{
			lock (lockObj)
			{
				if (String.IsNullOrEmpty(json))
					return;

				//Debug.WriteLine(json);

				OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

				if (change.Orc == null)
					return;

				_LastUpdated = DateTime.UtcNow;
				try
				{
					if (change.Closed == true)
					{
						Debug.WriteLine("market closed");
						Dispatcher.BeginInvoke(new Action(() => { Rows.Clear(); }));
					}

					if (change.Orc.Count > 0)
					{
						foreach (OrderRunnerChange orc in change.Orc)
						{
							if (orc.Uo == null)
								continue;

							if (orc.Uo.Count > 0)
							{
								List<Row> to_remove = new List<Row>();
								foreach (Order o in orc.Uo)
								{
									UInt64 betid = Convert.ToUInt64(o.Id);
									Debug.Assert(o.Status == Order.StatusEnum.E || o.Status == Order.StatusEnum.Ec);

									Row row = FindUnmatchedRow(o.Id);
									if (row == null)
									{
										row = new Row(o) { MarketID = MarketNode.MarketID, SelectionID = orc.Id.Value };
										Dispatcher.BeginInvoke(new Action(() =>
										{
											//Debug.WriteLine(o.Id, "Insert into grid: ");
											Rows.Insert(0, row);
										}));
										//Debug.WriteLine(o.Id, "new bet: ");
									}
									row.Runner = MarketNode.GetRunnerName(row.SelectionID);

									if (o.Sm == 0 && o.Sr > 0)                                          // unmatched
									{
										row.Stake = o.S.Value;
										row.SizeMatched = o.Sm.Value;
										row.Hidden = false;
										//Debug.WriteLine(o.Id, "unmatched: ");
										//Debug.WriteLine(MarketNode.MarketName);
									}
									if (o.Sc == 0 && o.Sm > 0 && o.Sr == 0)                             // fully matched
									{
										foreach (Row r in Rows)
										{
											if (r.BetID == Convert.ToUInt64(o.Id))
											{
												if (r.SizeMatched > 0)
												{
													to_remove.Add(r);
												}
											}
										}
										row.AvgPriceMatched = o.Avp.Value;
										row.SizeMatched = row.OriginalStake;// o.Sm.Value;
										row.Hidden = UnmatchedOnly;
										NotifyBetMatched();
										NotifyPropertyChanged("");
										Debug.WriteLine(o.Id, "fully matched: ");
									}
									if (o.Sm > 0 && o.Sr > 0)                                           // partially matched
									{
										Row mrow = new Row(row);
										mrow.SizeMatched = o.Sm.Value;
										mrow.Odds = o.P.Value;
										mrow.Stake = o.Sm.Value;
										mrow.AvgPriceMatched = o.Avp.Value;
										mrow.Hidden = UnmatchedOnly;
										Int32 idx = Rows.IndexOf(row);

										Dispatcher.BeginInvoke(new Action(() =>
										{
											Debug.WriteLine("Append to grid", mrow.BetID);
											Rows.Insert(idx + 1, mrow);
										}));
										row.Stake = o.Sr.Value;                         // change stake for the unmatched remainder
										NotifyBetMatched();
										NotifyPropertyChanged("");
										Debug.WriteLine(o.Id, "partial match: ");
									}
									if (o.Sc > 0)                                       // cancelled
									{
										if (o.Sr != 0)                                  // cancellation of partially matched bet
										{
											row.Stake = o.Sr.Value;                     // adjust unmatched remainder
											Debug.WriteLine(o.Id, "Cancellation of partially matched bet: ");
										}
										if (o.Sr == 0)
										{
											//Debug.WriteLine(o.Id, "Bet fully cancelled: ");
											to_remove.Add(row);
										}
										//Debug.WriteLine(o.Id, "cancelled: ");
										//										NotifyBetMatched();
									}
								}
								Dispatcher.BeginInvoke(new Action(() =>
								{
									foreach (Row o in to_remove)
									{
										if (Rows.Contains(o))
										{
											//Debug.WriteLine("Remove from grid: " + o.BetID.ToString());
											Rows.Remove(o);
										}
									}
								}));
							}
						}
					}
					NotifyPropertyChanged("");
				}
				catch (Exception xe)
				{
					Status = xe.Message;
					//Debug.WriteLine(xe.Message);
				}
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
						//dataGrid.Columns[0].Width = 200;
						NotifyPropertyChanged("");
					}
					catch (Exception xe)
					{
						Status = xe.Message;
					}
				}
			}
		}
		private void RowButton_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			Row row = b.DataContext as Row;
			if (row != null)
			{
				if (row.IsMatched || row.SizeMatched >= row.Stake)
				{
					Status = "Bet already matched";
					return;
				}

				lock (cancellation_queue)
				{
					Debug.WriteLine("enqueue cancel {0} for {1}", row.BetID, row.Runner);
					cancellation_queue.Enqueue(row.BetID);
					row.Hidden = true;
				}
			}
			else
			{
				Debug.WriteLine("null context");
			}
		}
		
		/// <summary>
		/// HUB stuff ///////////////////////////////////
		/// </summary>
		private void Connect()
		{
			String result = "";
			if (hubConnection != null)
			{
				hubConnection.Start().ContinueWith(task =>
				{
					if (OnFail(task))
					{
						result = "Failed to Connect";
						return;
					}
					result = "Connected";
				}).Wait(1000);
				Status = result;
			}
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
		private async void RequestCapture()
		{
			var client = new HttpClient();
			string response = await client.GetStringAsync("http://88.202.230.157:8088/api/market/capture");
			Debug.WriteLine(response);
		}
		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (Betfair == null)
			{
				Betfair = MainWindow.Betfair;
			}
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "HalveUnmatched":

						if (MarketNode != null)
						{
							await Task.Run(() =>
							{
								List<CancelInstruction> cancel_instructions = new List<CancelInstruction>();

								foreach (Row row in Rows)
								{
									if (!row.IsMatched && row.Stake >= 4)
									{
										cancel_instructions.Add(new CancelInstruction(row.BetID) { sizeReduction = Math.Round((row.Stake / 2), 2) });
									}
								}

								if (cancel_instructions.Count == 0)
								{
									Notification = "Nothing to do";
								}
								else
								{
									CancelExecutionReport report = Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);
									if (report != null && report.errorCode != null)
									{
										//throw new Exception(ErrorCodes.FaultCode(report.errorCode));
									}
									Status = report.errorCode != null ? report.errorCode : report.status;
								}
							});
						}
						break;

					case "DoubleUnmatched":

						if (MarketNode != null)
						{
							await Task.Run(() =>
							{
								List<CancelInstruction> cancel_instructions = new List<CancelInstruction>();
								List<PlaceInstruction> place_instructions = new List<PlaceInstruction>();

								foreach (Row row in Rows)
								{
									if (!row.IsMatched && row.Stake > 0)
									{
										cancel_instructions.Add(new CancelInstruction(row.BetID)
										{
											sizeReduction = null
										});
										place_instructions.Add(new PlaceInstruction()
										{
											selectionId = row.SelectionID,
											sideEnum = row.Side == "BACK" ? sideEnum.BACK : sideEnum.LAY,
											orderTypeEnum = orderTypeEnum.LIMIT,
											limitOrder = new LimitOrder()
											{
												size = row.Stake * 2,
												price = row.Odds,
												persistenceTypeEnum = persistenceTypeEnum.LAPSE,
											}
										});
									}
								}
								if (cancel_instructions.Count == 0)
								{
									Notification = "Nothing to do";
								}
								else
								{
									CancelExecutionReport cancel_report = Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);

									if (cancel_report.status != "SUCCESS")
									{
										Status = cancel_report.status;
									}
									else
									{
										PlaceExecutionReport place_report = Betfair.placeOrders(MarketNode.MarketID, place_instructions);
										Status = place_report.status;
									}
								}
							});
						}
						break;

					case "Capture":
						RequestCapture();
						break;
					case "Stream": if (IsConnected) Disconnect(); else Connect(); break;
					case "CancelAll":
						Status = "CancelAll";
						Notification = "";
						if (MarketNode == null)
						{
							Notification = "CancelAll No Market Selected";
						}
                        if (MarketNode != null)
                        {
							try
							{
								await Task.Run(() =>
								{
									List<CancelInstruction> cancel_instructions = new List<CancelInstruction>();

									foreach (Row row in Rows)
									{
										if (row.NoCancel)
											continue;

										if (!row.IsMatched && row.Stake > 0)
										{
											cancel_instructions.Add(new CancelInstruction(row.BetID)
											{
												sizeReduction = null
											});
										}
									}
									if (cancel_instructions.Count == 0)
									{
										Notification = "Nothing to do";
									}
									else
									{
										Notification = $"Cancelling {cancel_instructions.Count} bets";
										var sw = Stopwatch.StartNew();
										Betfair.cancelOrdersAsync(MarketNode.MarketID, cancel_instructions);
										sw.Stop();
										Debug.WriteLine($"==================================>  Execution time: {sw.ElapsedMilliseconds} ms");
									}
									Status = $"Cancellation Task completed";
								});
							}
							catch(Exception xxe)
                            {
								Notification = $"Cancellation Task failed with {xxe}";
							}
						}
						break;

                    case "AbsoluteCancelAll":

                        if (MarketNode != null)
                        {
                            await Task.Run(() =>
                            {
                                List<CancelInstruction> cancel_instructions = new List<CancelInstruction>();

                                foreach (Row row in Rows)
                                {
                                    if (!row.IsMatched && row.Stake > 0)
                                    {
                                        cancel_instructions.Add(new CancelInstruction(row.BetID)
                                        {
                                            sizeReduction = null
                                        });
                                    }
                                }
                                if (cancel_instructions.Count == 0)
                                {
                                    Status = "Nothing to do";
                                }
                                else
                                {
									Notification = $"Cancelling {cancel_instructions.Count} bets";
									var sw = Stopwatch.StartNew();
									Betfair.cancelOrdersAsync(MarketNode.MarketID, cancel_instructions);
									sw.Stop();
									Debug.WriteLine($"==================================>  Execution time: {sw.ElapsedMilliseconds} ms");
									Status = $"Cancellation Task completed";
                                }
                            });
                        }
                        break;
				}
			}
			catch(Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			if (Rows.Count > 0) foreach (Row row in Rows)
				{
					row.Hidden = cb.IsChecked == true && row.SizeMatched > 0;
				}
		}
		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			CheckBox_Checked(sender, e);
		}
		private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Label lb = sender as Label;
			Row row = lb.DataContext as Row;
			UpdateBet ub = new UpdateBet(row);
			ub.ShowDialog();
		}
		private void UserControl_Initialized(object sender, EventArgs e)
		{
			String json = props.ColumnWidths;
			double[] widths = JsonConvert.DeserializeObject<double[]>(json);
			if (widths != null)
			{
				for (int i = 0; i < Math.Min(dataGrid.Columns.Count, widths.Length); i++)
				{
					dataGrid.Columns[i].Width = new DataGridLength(widths[i]);
				}
			}
		}
		private void dataGrid_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			List<double> widths = new List<double>();

			foreach (DataGridColumn col in dataGrid.Columns)
			{
				widths.Add(col.ActualWidth);
			}
			String json = JsonConvert.SerializeObject(widths.ToArray());
			Debug.WriteLine(json);
			props.ColumnWidths = json;
			props.Save();
		}

        private void NoCancelCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Row row = cb.DataContext as Row;
            row.NoCancel = cb.IsChecked == true;
        }
        private void OverrideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Row row = cb.DataContext as Row;
            row.Override = cb.IsChecked == true;
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
