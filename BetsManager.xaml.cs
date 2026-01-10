using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpreadTrader
{
#region Properties
    public partial class BetsManager : UserControl, INotifyPropertyChanged
    {
        private Properties.Settings props = Properties.Settings.Default;
        public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
        public RunnersControl RunnersControl { get; set; }
        //public ObservableCollection<BetsManagerRow> Rows { get; set; }
		public BulkObservableCollection<BetsManagerRow> Rows { get; set; }
        private readonly Dictionary<ulong, BetsManagerRow> _byBetId = new Dictionary<ulong, BetsManagerRow>();
		private NodeViewModel MarketNode { get; set; }
        private DateTime _LastUpdated { get; set; }
		public String LastUpdated { get { return String.Format("Orders last updated {0}", _LastUpdated.AddHours(props.TimeOffset).ToString("HH:mm:ss")); } }

        private BetfairAPI.BetfairAPI Betfair { get; set; }
        private bool _StreamActive { get; set; }
        public bool StreamActive { get => _StreamActive;  set { _StreamActive = value; OnPropertyChanged(""); } }
        private Timer timer = new Timer();
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
            }
        }
        public bool UnmatchedOnly { get; set; }
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
                _Notification = value;
                Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Notification = value; }));
            }
        }
        public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
        public String StreamingButtonText { get { return "Streaming Connected"; } }
		#endregion Properties
		public void OnMarketSelected(NodeViewModel d2, RunnersControl rc)
		{
			MarketNode = d2;
            RunnersControl = rc;
            PopulateDataGrid();
		}
		private void OnMessageReceived(string messageName, object data)
		{
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
            ControlMessenger.Send("Update P&L");
        }

        public BetsManager()
        {
            Rows = new BulkObservableCollection<BetsManagerRow>();
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;
        }
        private async void NotifyBetMatchedAsync()
        {
            ControlMessenger.Send("Update P&L");

            String path = props.MatchedBetAlert;
            if (!string.IsNullOrEmpty(path))
            {
                var player = new SoundPlayer(path);
                player.Play();
            }
		}

		public void OnOrderChanged(String json)
        {
            if (String.IsNullOrEmpty(json))
                return;

            OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json);

			if (change.Orc == null || MarketNode == null)
                return;

            if (MarketNode.MarketID.ToString() != change.Id)
                return;

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
								utc = o.Pd.Value;
								UInt64 betid = Convert.ToUInt64(o.Id);
                                Debug.Assert(o.Status == Order.StatusEnum.E || o.Status == Order.StatusEnum.Ec);

								_byBetId.TryGetValue(betid, out BetsManagerRow row);

								if (row == null)        // not in the grid
                                {
                                    row = new BetsManagerRow(o) { MarketID = change.Id, SelectionID = orc.Id.Value };
                                    ops.Add(() => { Rows.Insert(0, row); _byBetId[row.BetID] = row; });
                                    Debug.WriteLine(o.Id, "new bet: ");
								}
								String runner_name = MarketNode.GetRunnerName(row.SelectionID);
								ops.Add(() => row.Runner = runner_name);

								if (o.Sm == 0 && o.Sr > 0)                                          // unmatched
								{
									ops.Add(() => row.Stake = o.S.Value);
									ops.Add(() => row.SizeMatched = o.Sm.Value);
									ops.Add(() => row.Hidden = false);
									
                                    Debug.WriteLine(o.Id, "unmatched: ");
									Debug.WriteLine(MarketNode.MarketName);
								}
								if (o.Sm == 0 && o.Sl > 0)                                          // lapsed
								{
									Debug.WriteLine(o.Id, "lapsed: ");
									ops.Add(() => { if (_byBetId.Remove(row.BetID)) Rows.Remove(row); });
								}
								if (o.Sc == 0 && o.Sm > 0 && o.Sr == 0)                             // fully matched
                                {
									ops.Add(() => row.AvgPriceMatched = o.Avp.Value);
									ops.Add(() => row.SizeMatched = row.Stake);
									ops.Add(() => row.Hidden = UnmatchedOnly);
                                    betMatched = true;
                                    Debug.WriteLine(o.Id, "fully matched: ");
                                }
                                if (o.Sm > 0 && o.Sr > 0)                                           // partially matched
                                {
									BetsManagerRow mrow = new BetsManagerRow(row);
                                    mrow.SizeMatched = o.Sm.Value;
                                    mrow.Odds = o.P.Value;
                                    mrow.Stake = o.Sm.Value;
                                    mrow.AvgPriceMatched = o.Avp.Value;
                                    mrow.Hidden = UnmatchedOnly;

									ops.Add(() =>
									{
										int idx = Rows.IndexOf(row);
										Rows.Insert(idx + 1, mrow);
									});

									ops.Add(() => row.Stake = o.Sr.Value);                         // change stake for the unmatched remainder
									betMatched = true;

                                    Debug.WriteLine(o.Id, "partial match: ");
                                }
                                if (o.Sc > 0)                                       // cancelled
                                {
                                    if (o.Sr != 0)                                  // cancellation of partially matched bet
                                    {
										ops.Add(() => row.Stake = o.Sr.Value);                     // adjust unmatched remainder
                                        Debug.WriteLine(o.Id, "Cancellation of partially matched bet: ");
                                    }
                                    if (o.Sr == 0)
                                    {
                                        Debug.WriteLine(o.Id, "Bet fully cancelled: ");
										ops.Add(() =>
										{
											if (_byBetId.Remove(row.BetID))
												Rows.Remove(row);
										});
									}
								}
                            }
						}
					}
					Dispatcher.BeginInvoke(new Action(() =>
					{
						using (Rows.SuspendNotifications())
						{
							foreach (var op in ops)
								op();
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
                Rows = new BulkObservableCollection<BetsManagerRow>();

                if (MarketNode.MarketID != null)
                {
                    try
                    {
                        CurrentOrderSummaryReport report = Betfair.listCurrentOrders(MarketNode.MarketID); // "1.185904913"

                        Rows.Clear();
                        if (report.currentOrders.Count > 0)
                        {
                            foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
                            {
								BetsManagerRow.RunnerNames[o.selectionId] = MarketNode.GetRunnerName(o.selectionId);
                                Rows.Insert(0, new BetsManagerRow(o) { Runner = MarketNode.GetRunnerName(o.selectionId), });
                            }
                        }
						OnPropertyChanged("");
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
			BetsManagerRow row = b.DataContext as BetsManagerRow;
            if (row != null)
            {
                if (row.IsMatched || row.SizeMatched >= row.Stake)
                {
                    Status = "Bet already matched";
                    return;
                }
                CancelExecutionReport cancel_report = Betfair.cancelOrder(MarketNode.MarketID, row.BetID);
                ControlMessenger.Send("Update P&L");
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
                        ControlMessenger.Send("Update P&L");
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
                                Notification = $"Cancellation Task failed with {xxe}";
                            }
                        }
                        ControlMessenger.Send("Update P&L");
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
									//CancelExecutionReport report = Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);

									//foreach (Tuple<UInt64, String> _report in report.statuses)
									//{
									//	Debug.WriteLine(_report.Item1, _report.Item2);
									//}
									//if (cancel_report.status != "SUCCESS")
         //                           {
         //                               Status = cancel_report.status;
         //                           }
                                }
                            });
                        }
                        ControlMessenger.Send("Update P&L");
                        break;
                }
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (Rows.Count > 0) foreach (BetsManagerRow row in Rows)
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
			BetsManagerRow row = cb.DataContext as BetsManagerRow;
            row.NoCancel = cb.IsChecked == true;
        }
        private void OverrideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
			BetsManagerRow row = cb.DataContext as BetsManagerRow;
            row.Override = cb.IsChecked == true;
        }
    }
    //public class OrderMarketSnap
    //{
    //    public string MarketId { get; set; }
    //    public bool IsClosed { get; set; }
    //    public IEnumerable<OrderMarketRunnerSnap> OrderMarketRunners { get; set; }
    //}
    //public class OrderMarketRunnerSnap
    //{
    //    public IList<PriceSize> MatchedLay { get; set; }
    //    public IList<PriceSize> MatchedBack { get; set; }
    //    public Dictionary<string, Order> UnmatchedOrders { get; set; }
    //}
}
