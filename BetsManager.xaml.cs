using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using Newtonsoft.Json;
using SpreadTrader.Simulator;
using StreamSimulator;
using StreamSimulator.Synthetic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Networking.NetworkOperators;

namespace SpreadTrader
{

    public partial class BetsManager : UserControl, INotifyPropertyChanged
    {
#region Properties
		private Properties.Settings props = Properties.Settings.Default;
        //public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
        public RunnersControl RunnersControl { get; set; }
        public BulkObservableCollection<BetsManagerRow> Rows { get; set; }
		private List<BetsManagerRow> _allRows = new List<BetsManagerRow>();
		private List<ulong> _cancelledBets = new List<ulong>();

		private readonly Dictionary<ulong, BetsManagerRow> _byBetId = new Dictionary<ulong, BetsManagerRow>();
		private readonly object _lock = new object();
		private NodeViewModel MarketNode { get; set; }
        private DateTime _LastUpdated { get; set; }
		public String LastUpdated { get { return String.Format("Orders last updated {0}", _LastUpdated.AddHours(props.TimeOffset).ToString("HH:mm:ss")); } }

        private BetfairAPI.BetfairAPI Betfair { get; set; }
        private bool _StreamActive { get; set; }
        public bool StreamActive { get => _StreamActive;  set { _StreamActive = value; OnPropertyChanged(""); } }

		void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private bool _UnmatchedOnly;
		public bool UnmatchedOnly { get => _UnmatchedOnly; set { if (_UnmatchedOnly != value)
                { 
                    _UnmatchedOnly = value;
					OnPropertyChanged();
					ApplyFilter();
				} 
            } 
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private String _Status = "Ready";
        public String Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = value; }));
            }
        }
        private String _Notification = "";
        public String Notification
        {
            get { return _Notification; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;

                _Notification = value;
                Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Notification = value; }));
            }
        }
        public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
        public String StreamingButtonText { get { return "Streaming Connected"; } }
		#endregion Properties

		private SimulatedStream _simulatedStream;

		public BetsManager()
		{
			Rows = new BulkObservableCollection<BetsManagerRow>();
			InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;

            /////////// SIMULATOR STUFF

            InitSimulator();

            ///////////////////////////
		}

        void InitSimulator()
        {
            OcmDiagnostics.Clear();
			_simulatedStream = new SimulatedStream(ReplayMode.WallClockAccurate, 1, 1.0);
			_simulatedStream.OnChange = (change) => WebSocketsHub.Instance.Simulate(change);
		}

		private void ApplyFilter()
		{
            using (Rows.SuspendNotifications())
            {
                Rows.Clear();

                foreach (var row in _allRows)
                {
                    if (!UnmatchedOnly || !row.IsMatched)
                        Rows.Add(row);
                }
				Interlocked.Increment(ref OcmDiagnostics.MessagesProcessed);
			}
		}
		public void OnMarketSelected(NodeViewModel d2, RunnersControl rc)
		{
			String _marketId = "";
			if (MarketNode != null)
				_marketId = MarketNode.MarketID; 
                
			MarketNode = d2;
            RunnersControl = rc;
            PopulateDataGrid();

			_simulatedStream?.MapRealMarket(d2);

			WebSocketsHub.Instance.Attach(MarketNode.MarketID, this);
			WebSocketsHub.Instance.Detach(_marketId, this);
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "New")
			{
				_simulatedStream?.SimulateNewBet(MarketNode.MarketID, MarketNode.LiveRunners[0].SelectionId, 25);
			}
			if (messageName == "Full")
			{
				if (SelectedRow != null)
					_simulatedStream?.SimulateFull(SelectedRow.BetID.ToString());
			}
			if (messageName == "Partial")
			{
                if (SelectedRow != null)
					_simulatedStream?.SimulatePartial(SelectedRow.BetID.ToString(), 5);
			}
			if (messageName == "Random Burst")
			{
				_simulatedStream?.SimulateRandomBurst (MarketNode.MarketID, MarketNode.LiveRunners[0].SelectionId, 10);
			}
			if (messageName == "Clear")
			{
                Rows.Clear();
                _byBetId.Clear();
                _allRows.Clear();
				InitSimulator();
			}

			if (messageName == "Favorite Changed")
			{
				dynamic d = data;
				Debug.WriteLine($"BetsManager: {messageName} : {d.Name}");
			}
            if (messageName == "Execute Bets")
			{
				dynamic d = data;
				Debug.WriteLine($"BetsManager: {messageName} : {d.Favorite.Name}");
				ExecuteBets(d.Favorite, d.LayValues, d.BackValues);
			}
		}
		private void SendOrdersLatency(long utc_time)
        {
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			String cs = $"{now - utc_time}ms";
			ControlMessenger.Send("Update Orders Latency", new { OrdersLatency = cs });
		}
		private async void ExecuteBets(LiveRunner runner, List<PriceSize> lay, List<PriceSize> back)
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

			await Task.Run(() =>
			{
				PlaceExecutionReport report = Betfair.placeOrders(MarketNode.MarketID, place_instructions);
				Status = report.status;
            });
        }
        private async void NotifyBetMatchedAsync()
        {
            String path = props.MatchedBetAlert;
            if (!string.IsNullOrEmpty(path))
            {
                var player = new SoundPlayer(path);
                player.Play();
            }
		}
		private void ApplyPartialMatch(Order o, String runner_name)
		{
			UInt64 betid = Convert.ToUInt64(o.Id);
			var actualRow = _allRows.FirstOrDefault(r => r.BetID == betid && !r.IsMatchedFragment);

			if (actualRow == null)
			{
				Debug.WriteLine($"Row not found for BetID {o.Id}");
				return;
			}
			// clone row
			var mrow = new BetsManagerRow(actualRow)
			{
                SelectionID = actualRow.SelectionID,
                Runner = runner_name,
				SizeMatched = o.Sm ?? 0.0,
				Odds = o.P ?? 0.0,
				Stake = o.Sm ?? 0.0,
				//AvgPriceMatched = o.Avp ?? 0.0,
				AvgPriceMatched = o.P ?? 0.0,

				Hidden = UnmatchedOnly,
				IsMatchedFragment = true
			};

			int idx = _allRows.IndexOf(actualRow);

			if (idx >= 0)
			{
				_allRows.Insert(idx + 1, mrow);
			}
			else
			{
				Debug.WriteLine($"Index failure for BetID {o.Id} : {actualRow}");
			}
			// update unmatched remainder
			actualRow.Stake = o.Sr.Value;
		}

		private static int _inFlight = 0;
		private static long _lastPd = -1;

		public void OnOrderChanged(OrderMarketChange change)
        {
			if (change?.Orc == null || MarketNode == null)
                return;

            if (MarketNode.MarketID.ToString() != change.Id)
                return;

			var inFlight = Interlocked.Increment(ref _inFlight);

			_LastUpdated = DateTime.UtcNow;
            try
            {
                long utc = 0;
                if (change.Closed == true)
                {
                    Debug.WriteLine("market closed");
                    ControlMessenger.Send("Market Status Changed", new { String = "Closed" });
                    Dispatcher.BeginInvoke(new Action(() => { Rows.Clear(); }));
                    return;
                }

                if (change.Orc.Count > 0)
                {
					var ops = new List<Action>();
                    bool betMatched = false;

					foreach (OrderRunnerChange orc in change.Orc)
                    {
                        if (orc.Uo == null)
                            continue;

                        if (orc.Uo.Count > 0)
                        {
							foreach (Order o in orc.Uo)
                            {
								if (inFlight > 1)
								{
									Debug.WriteLine(
										$"[CONCURRENT] Pd={o.Pd} InFlight={inFlight}");
								}

								if (_lastPd != -1 && o.Pd < _lastPd)
								{
									Debug.WriteLine(
										$"[OCM OUT OF ORDER] Pd={o.Pd} LastPd={_lastPd}");
								}

								_lastPd = o.Pd.Value;

                                ////////////////////////////////  
								Debug.WriteLine( $"[ENTER] Pd={o.Pd} " + $"Thread={Thread.CurrentThread.ManagedThreadId} " + $"UI={Application.Current.Dispatcher.CheckAccess()}");
								////////////////////////////////  
								OcmDiagnostics.ApplyOcmUpdate(o.Id, o.Pd.Value, Thread.CurrentThread.ManagedThreadId);
								////////////////////////////////  

								utc = o.Pd.Value;
								UInt64 betid = Convert.ToUInt64(o.Id);
                                Debug.Assert(o.Status == Order.StatusEnum.E || o.Status == Order.StatusEnum.Ec);

                                _byBetId.TryGetValue(betid, out BetsManagerRow row);

								if (row == null)
								{
                                    if (_cancelledBets.Contains(betid))
                                    {
                                        Debug.WriteLine($"_cancelledBets.Contains({betid})");
                                        continue;
                                    }

									////////////////////////////////  
									Debug.WriteLine( $"[DISPATCH BEFORE] Pd={o.Pd} " + $"Thread={Thread.CurrentThread.ManagedThreadId}");

									Dispatcher.Invoke(() =>
									{
										////////////////////////////////  
										Debug.WriteLine( $"[DISPATCH INSIDE] Pd={o.Pd} " + $"Thread={Thread.CurrentThread.ManagedThreadId}");

										var newRow = new BetsManagerRow(o)
										{
											IsMatchedFragment = false,
											MarketID = change.Id,
											SelectionID = orc.Id.Value
										};

										_allRows.Insert(0, newRow);
										_byBetId[betid] = newRow;
                                        row = newRow;
									});

									////////////////////////////////  
									Debug.WriteLine( $"[DISPATCH AFTER] Pd={o.Pd} " + $"Thread={Thread.CurrentThread.ManagedThreadId}");
									Debug.WriteLine($"CREATED base row {betid}");
								}
                                								
								String runner_name = MarketNode.GetRunnerName(row.SelectionID);
								ops.Add(() => row.Runner = runner_name);

								// --- 1. LIFECYCLE / TERMINAL EVENTS FIRST ---

								if (o.Sl > 0)
								{
									///Debug.WriteLine(o.Id, "lapsed");
									ops.Add(() => { if (_byBetId.Remove(row.BetID)) _allRows.Remove(row); });
								}

								if (o.Sc > 0)
								{
									if (o.Sr > 0)
									{
										///Debug.WriteLine(o.Id, "Partial cancellation (unmatched remainder reduced)");
										ops.Add(() => row.Stake = o.Sr.Value);
									}
									else if (o.Sr == 0 && o.Sm == 0)
									{
										///Debug.WriteLine(o.Id, "Fully cancelled (never matched)");
										_cancelledBets.Add(row.BetID);

										ops.Add(() =>
										{
											if (_byBetId.Remove(row.BetID))
												_allRows.Remove(row);
										});
									}
									else if (o.Sr == 0 && o.Sm > 0)
									{
										///Debug.WriteLine(o.Id, "Remainder cancelled after partial match");
										// INCORRECT: ops.Add(() => row.Stake = 0);
										_cancelledBets.Add(row.BetID);

										ops.Add(() =>
										{
											if (_byBetId.Remove(row.BetID))
												_allRows.Remove(row);
										});
									}
								}

								// --- 2. MATCH STATE (independent of cancellation) ---

								if (o.Sm > 0 && o.Sr > 0)
								{
									///Debug.WriteLine(o.Id, "partial match"); 									            // partially matched
									if (!_allRows.Any(r => r.BetID == row.BetID))
									{
										Debug.WriteLine($"INCONSISTENCY: row {row.BetID} not in _allRows");
										Debug.WriteLine($"_byBetId contains: {_byBetId.ContainsKey(betid)}");
										Debug.WriteLine($"_allRows contains: {_allRows.Any(r => r.BetID == betid)}");
									}
									Dispatcher.Invoke(() =>
									{
										ApplyPartialMatch(o, runner_name);
									});
								}
								else if (o.Sm > 0 && o.Sr == 0)                                                                     // fully matched
								{
									///Debug.WriteLine(o.Id, "fully matched");
									ops.Add(() => row.AvgPriceMatched = o.Avp ?? 0);
									ops.Add(() => row.SizeMatched = row.Stake);
									betMatched = true;
								}
								else if (o.Sm == 0 && o.Sr > 0)
								{
									// unmatched
									///Debug.WriteLine(o.Id, "unmatched");
									ops.Add(() => row.Stake = o.S.Value);
									ops.Add(() => row.SizeMatched = o.Sm.Value);
									ops.Add(() => row.Hidden = false);
								}
								OcmDiagnostics.MeasureLatency(o.Id, o.Pd.Value);

								Debug.WriteLine( $"[EXIT] Pd={o.Pd} " + $"Thread={Thread.CurrentThread.ManagedThreadId}");
							}
						}
					}
					Dispatcher.BeginInvoke(new Action(() =>
					{
						foreach (var op in ops)
							op();

						using (Rows.SuspendNotifications())
						{
                            ApplyFilter();
						}
					}));

					if (betMatched)
						NotifyBetMatchedAsync();

					SendOrdersLatency(utc);
				}
            }
            catch (Exception xe)
            {
                Status = xe.Message;
                Debug.WriteLine(xe.Message);
            }
			Interlocked.Decrement(ref _inFlight);
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
                if (MarketNode.MarketID != null)
                {
                    try
                    {
						Debug.WriteLine(MarketNode.MarketID, "PopulateDataGrid: ");

						CurrentOrderSummaryReport report = Betfair.listCurrentOrders(MarketNode.MarketID); 

                        _allRows.Clear();
                        if (report.currentOrders.Count > 0)
                        {
                            foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
                            {
								if (_cancelledBets.Contains(o.betId))
									continue;   // do not resurrect

								if (_byBetId.ContainsKey(o.betId))
									continue;   // already have it from stream

								BetsManagerRow newrow = new BetsManagerRow(o) { Runner = MarketNode.GetRunnerName(o.selectionId), };
                                _allRows.Insert(0, newrow);
								_byBetId[o.betId] = newrow;
								Debug.WriteLine(o.betId, "insert new bet into _allRows: ");
							}
                        }
                        ApplyFilter();
					}
					catch (Exception xe)
                    {
                        Status = xe.Message;
                        Debug.WriteLine(xe);
					}
                }
            }
        }
        private void RowButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
			BetsManagerRow row = b.DataContext as BetsManagerRow;
            if (row != null)
            {
                if (row.IsMatched || row.SizeMatched >= row.Stake)
                {
                    Status = "Bet already matched";
                    return;
                }
				_simulatedStream?.SimulateCancel(row.BetID.ToString());

                if (_simulatedStream != null)
                    Betfair.cancelOrder(MarketNode.MarketID, row.BetID);
			}
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

                                foreach (BetsManagerRow row in Rows)
                                {
                                    if (!row.IsMatched && row.Stake >= 4)
                                    {
                                        cancel_instructions.Add(new CancelInstruction(row.BetID) { sizeReduction = Math.Round((row.Stake / 2), 2) });
										_simulatedStream?.SimulateCancel(row.BetID.ToString());
									}
								}
                                if (cancel_instructions.Count == 0)
                                {
                                    Notification = "Nothing to do";
                                }
                                else
                                {
									Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);
								}
							});
                        }
                        break;

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

                                    foreach (BetsManagerRow row in Rows)
                                    {
                                        if (row.NoCancel)
                                            continue;

                                        if (!row.IsMatched && row.Stake > 0)
                                        {
                                            cancel_instructions.Add(new CancelInstruction(row.BetID)
                                            {
                                                sizeReduction = null
                                            });
											_simulatedStream?.SimulateCancel(row.BetID.ToString());
										}
									}
                                    if (cancel_instructions.Count == 0)
                                    {
                                        Notification = "Nothing to do";
                                    }
                                    else
                                    {
                                        Notification = $"Cancelling {cancel_instructions.Count} bets";
                                        Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);
                                    }
                                    Status = "Cancellation Task completed";
                                });
                            }
                            catch (Exception xxe)
                            {
								Debug.WriteLine($"Cancellation Task failed with {xxe}");
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

                                foreach (BetsManagerRow row in Rows)
                                {
                                    if (!row.IsMatched && row.Stake > 0)
                                    {
                                        cancel_instructions.Add(new CancelInstruction(row.BetID)
                                        {
                                            sizeReduction = null
                                        });
										_simulatedStream?.SimulateCancel(row.BetID.ToString());
									}
								}
                                if (cancel_instructions.Count == 0)
                                {
                                    Status = "Nothing to do";
                                }
                                else
                                {
									Notification = $"Cancelling {cancel_instructions.Count} bets";
									Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);
                                }
                            });
                        }
                        break;
                }
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
            }
        }
        private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Label lb = sender as Label;
			BetsManagerRow row = lb.DataContext as BetsManagerRow;
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
        private BetsManagerRow SelectedRow;
        private void dataGrid_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dg = sender as DataGrid;

            var row = dg.SelectedItem as BetsManagerRow;

            if (row != null)
            {
                SelectedRow = row;
            }

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
			BetsManagerRow row = cb.DataContext as BetsManagerRow;
            row.NoCancel = cb.IsChecked == true;
        }
        private void OverrideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
			BetsManagerRow row = cb.DataContext as BetsManagerRow;
            row.Override = cb.IsChecked == true;
        }

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//Debug.WriteLine("BetsManager Unloaded");
			
   //         if (MarketNode != null)
			//{
			//	WebSocketsHub.Instance.Detach(MarketNode.MarketID, this);
			//	MarketNode = null;
			//}
		}
	}
}
