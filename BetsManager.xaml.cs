using Betfair.ESASwagger.Model;
using BetfairAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpreadTrader
{
#region Properties
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
        private Queue<String> incomingOrdersQueue = new Queue<String>();
        private Queue<UInt64> cancellation_queue = new Queue<UInt64>();

        private Properties.Settings props = Properties.Settings.Default;
        public static Dictionary<UInt64, Order> Orders = new Dictionary<ulong, Order>();
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
		public Int32 TabID { get; set; }
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
		public void OnSelected(NodeViewModel d2, RunnersControl rc)
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

            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                PlaceExecutionReport report = Betfair.placeOrders(MarketNode.MarketID, place_instructions);
                Status = report.status;
                //Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
            });
            Debug.WriteLine("Execute Bets not available for development");          //t.Start();
        }

        public BetsManager()
        {
            Rows = new ObservableCollection<Row>();
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;
        }
        private async void NotifyBetMatchedAsync()
        {
            String path = props.MatchedBetAlert;
			var player = new SoundPlayer(path);
			player.Play();
		}

		public void OnOrderChanged(String json)
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
                    ControlMessenger.Send("Market Status Changed", new { String = "Closed" });
                    Dispatcher.BeginInvoke(new Action(() => { Rows.Clear(); }));
                    return;
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
                                    row = new Row(o) { MarketID = change.Id, SelectionID = orc.Id.Value };
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        Debug.WriteLine(o.Id, "Insert into grid: ");
                                        Rows.Insert(0, row);
                                    }));
                                    Debug.WriteLine(o.Id, "new bet: ");
                                }
                                row.Runner = MarketNode.GetRunnerName(row.SelectionID);

                                if (o.Sm == 0 && o.Sr > 0)                                          // unmatched
                                {
                                    row.Stake = o.S.Value;
                                    row.SizeMatched = o.Sm.Value;
                                    row.Hidden = false;
                                    Debug.WriteLine(o.Id, "unmatched: ");

                                    Debug.WriteLine(MarketNode.MarketName);
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
									NotifyBetMatchedAsync();
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
									NotifyBetMatchedAsync();
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
                                        Debug.WriteLine(o.Id, "Bet fully cancelled: ");
                                        to_remove.Add(row);
                                    }
									//Debug.WriteLine(o.Id, "cancelled: ");
									//										NotifyBetMatchedAsync();
								}
							}
                            foreach (Row o in to_remove)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (Rows.Contains(o))
                                    {
                                        //Debug.WriteLine("Remove from grid: " + o.BetID.ToString());
                                        Rows.Remove(o);
                                    }
                                }));
                            }
                            NotifyPropertyChanged("");
                        }
                    }
                }
            }
            catch (Exception xe)
            {
                Status = xe.Message;
                //Debug.WriteLine(xe.Message);
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

                        Rows.Clear();
                        if (report.currentOrders.Count > 0)
                        {
                            foreach (CurrentOrderSummaryReport.CurrentOrderSummary o in report.currentOrders)
                            {
                                OrdersStatic.BetID2SelectionID[o.betId] = o.selectionId;
                                Row.RunnerNames[o.selectionId] = RunnersControl.GetRunnerName(o.selectionId);
                                Rows.Insert(0, new Row(o)
                                {
                                    Runner = RunnersControl.GetRunnerName(o.selectionId),
                                });
                            }
                        }
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
                                        CancelExecutionReport cancel_report = Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);

                                        if (cancel_report?.status != "SUCCESS")
                                        {
                                            Status = cancel_report?.errorCode;
                                        }
                                        else
                                        {
                                            Status = cancel_report.status;
                                        }
                                    }
                                    //Status = "Cancellation Task completed";
                                });
                            }
                            catch (Exception xxe)
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
                                    CancelExecutionReport cancel_report = Betfair.cancelOrders(MarketNode.MarketID, cancel_instructions);

                                    if (cancel_report.status != "SUCCESS")
                                    {
                                        Status = cancel_report.status;
                                    }
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
