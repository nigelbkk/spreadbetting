using Betfair.ESASwagger.Model;
using BetfairAPI;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        public double DisplayStake { get {
                //if (IsFullyMatched)
                //    return OriginalStake;
                //if (IsPartiallyMatched)
                //    return SizeMatched;
                return Stake;
            } }
        public double AvgPriceMatched { get; set; }
        public double Profit { get { return Math.Round(DisplayStake * (DisplayOdds - 1), 5); } }
        public double _SizeMatched { get; set; }
        public double SizeMatched { get { return _SizeMatched; } set { _SizeMatched = value; NotifyPropertyChanged(""); } }
        public bool IsMatched { get { return SizeMatched > 0; } }
        //public bool IsUnMatched { get { return AvgPriceMatched == 0; } }
        //public bool IsPartiallyMatched { get { return ; } }
        //public bool IsFullyMatched { get { return AvgPriceMatched > 0; } }
        public String IsMatchedString { get { return SizeMatched > 0 ? "F" : "U"; } }
        public bool IsBack { get { return Side.ToUpper() == "BACK"; } }
        private bool _Hidden = false;
        public bool Hidden { get { return _Hidden; } set { _Hidden = value; NotifyPropertyChanged(""); } }
        public bool Override { get; set; }
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
        public String Status { get { return _Status; } set { 
                _Status = value;
                Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = value; }));
                //Extensions.MainWindow.Status = value;
            } }
        private bool _Connected { get { return !String.IsNullOrEmpty(hubConnection.ConnectionId); } }
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
        private void ProcessIncomingOrders(object o)
        {
            BackgroundWorker sender = o as BackgroundWorker;
            while (!sender.CancellationPending)
            {
                while (incomingOrdersQueue.Count > 0)
                {
                    try
                    {
                        Debug.WriteLine("Fetch from queue");
                        String json = incomingOrdersQueue.Dequeue();
                        OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json);

                        Dispatcher.BeginInvoke(new Action(() => { OnOrderChanged(json); }));
                       
                    }
                    catch (Exception xe)
                    {
                        Status = xe.Message;
                    }
                }
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
                        UInt64 betid = cancellation_queue.Dequeue();

                        Debug.WriteLine("submit cancel {0}", betid);
                        CancelExecutionReport report = Betfair.cancelOrder(MarketNode.MarketID, betid);
                        if (report.errorCode == null)
                        {
                            Debug.WriteLine("bet is cancelled {0}", betid);
                        }
                        Status = report.errorCode != null ? report.errorCode : report.status;
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
            bw.DoWork += (o, e) => ProcessIncomingOrders(o);
            bw.RunWorkerAsync();

            BackgroundWorker bw2 = new BackgroundWorker();
            bw2.DoWork += (o, e) => ProcessCancellationQueue(o);
            bw2.RunWorkerAsync();

            hubConnection = new HubConnection("http://" + props.StreamUrl);
            hubProxy = hubConnection.CreateHubProxy("WebSocketsHub");

            hubProxy.On<string, string, string>("ordersChanged", (json1, json2, json3) =>
            {
                OrderMarketSnap snapshot = JsonConvert.DeserializeObject<OrderMarketSnap>(json3);
                OrderMarketChange change = JsonConvert.DeserializeObject<OrderMarketChange>(json3);
                if (MarketNode != null && snapshot.MarketId == MarketNode.MarketID)
                {
                    Debug.WriteLine("Add to queue");
                    incomingOrdersQueue.Enqueue(json1);

                    if (props.production)        // live system?
                    {
                        String file_name = String.Format(".\\notifications.json");
                        if (!File.Exists(file_name))
                        {
                            using (var stream = File.CreateText(file_name))
                            {
                            }
                        }
                        File.AppendAllText(file_name, json1 + "\n");
                    }
                }
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

            OnSubmitBets += (runner, lay, back) =>
            {
                String MarketID  = MarketNode.MarketID;
                long Selection = runner.SelectionId;

                StringBuilder sb = new StringBuilder(String.Format("Back: {0} for {1:c} at ", runner.Name, back[0].size));

                foreach (PriceSize p in back)
                {
                    sb.AppendFormat("{0:0.00}, ", p.price);
                }
                Debug.WriteLine(sb.ToString().TrimEnd(' ').TrimEnd(','));

                sb = new StringBuilder(String.Format("Lay : {0} for {1:c} at ", runner.Name, lay[0].size));

                foreach (PriceSize p in lay)
                {
                    sb.AppendFormat("{0:0.00}, ", p.price);
                }
                Debug.WriteLine(sb.ToString().TrimEnd(' ').TrimEnd(','));
            };

            OnFavoriteChanged += (runner) =>
            {
                //Debug.WriteLine("OnFavoriteChanged");
               // Favorite = runner;
            };
            
            OnMarketSelected += (node) =>
            {
                if (IsLoaded)
                {
                    MarketNode = node;
                    PopulateDataGrid();
                }
            };
            Connect();
        }
        private object lockObj = new object();

        private void OnOrderChanged(String json)
        {
            lock (lockObj)
            {
                if (String.IsNullOrEmpty(json))
                    return;

                Debug.WriteLine(json);

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

                                    if (betid == 299150050157)
                                    {
                                    }
                                    Debug.Assert(o.Status == Order.StatusEnum.E || o.Status == Order.StatusEnum.Ec);

                                    Row row = FindUnmatchedRow(o.Id);
                                    if (row == null)
                                    {
                                        row = new Row(o) { MarketID = MarketNode.MarketID, SelectionID = orc.Id.Value };
                                        Rows.Insert(0, row);
                                        NotifyPropertyChanged("");
                                        Debug.WriteLine(o.Id, "new bet");
                                    }
                                    row.Runner = MarketNode.GetRunnerName(row.SelectionID);

                                    if (o.Sm == 0 && o.Sr > 0)                                      // unmatched
                                    {
                                        row.Stake = o.S.Value;
                                        row.SizeMatched = o.Sm.Value;
                                        row.Hidden = false;
                                        Debug.WriteLine(o.Id, "unmatched");
                                    }
                                    if (o.Sc == 0 && o.Sm > 0 && o.Sr == 0)                          // fully matched
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
                                        if (!String.IsNullOrEmpty(props.MatchedBetAlert))
                                        {
                                            SoundPlayer snd = new SoundPlayer(props.MatchedBetAlert);
                                            snd.Play();
                                        }
                                        Debug.WriteLine(o.Id, "fully matched");
                                    }
                                    if (o.Sm > 0 && o.Sr > 0)                           // partially matched
                                    {
                                        Row mrow = new Row(row);
                                        mrow.SizeMatched = o.Sm.Value;
                                        mrow.Odds = o.P.Value;
                                        mrow.Stake = o.Sm.Value;
                                        mrow.AvgPriceMatched = o.Avp.Value;
                                        mrow.Hidden = UnmatchedOnly;
                                        Int32 idx = Rows.IndexOf(row);

                                        Rows.Insert(idx + 1, mrow);                  // insert a new row for the matched portion
                                        row.Stake = o.Sr.Value;                     // change stake for the unmatched remainder
                                        NotifyPropertyChanged("");

                                        if (!String.IsNullOrEmpty(props.MatchedBetAlert))
                                        {
                                            SoundPlayer snd = new SoundPlayer(props.MatchedBetAlert);
                                            snd.Play();
                                        }
                                        Debug.WriteLine(o.Id, "partial match");
                                    }
                                    if (o.Sc > 0)                                       // cancelled
                                    {
                                        if (o.Sr != 0)                                  // cancellation of partially matched bet
                                        {
                                            row.Stake = o.Sr.Value;                     // adjust unmatched remainder
                                            Debug.WriteLine(o.Id, "Cancellation of partially matched bet");
                                        }
                                        if (o.Sr == 0)
                                        {
                                            Debug.WriteLine("Bet fully cancelled");
                                            to_remove.Add(row);
                                        }
                                        Debug.WriteLine(o.Id, "cancelled");
                                    }
                                }
                                foreach (Row o in to_remove)
                                {
                                    Debug.WriteLine(o.BetID, "Remove");
                                    if (Rows.Contains(o))
                                    {
                                        Debug.WriteLine(o.BetID);
                                        Rows.Remove(o);
                                    }
                                }
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
                Debug.WriteLine("enqueue cancel {0} for {1}", row.BetID, row.Runner);
                
                cancellation_queue.Enqueue(row.BetID);
                Rows.Remove(row);

                //ProcessCancellationQueue(MarketNode.MarketID);
            }
            else
            {
                Debug.WriteLine("null context");
            }
        }
        private void Connect()
        {
            String result = "";
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
        private Int32 newbetid = 44448880;
        private double amount_remaining = 0;
        private String newbet(LiveRunner lr, Order.SideEnum side)
        {
            Int32 new_stake = 100;
            amount_remaining = new_stake;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OrderMarketChange change = new OrderMarketChange();

            change.Orc = new List<OrderRunnerChange>();
            OrderRunnerChange orc = new OrderRunnerChange();
            orc.Uo = new List<Order>();
            orc.Id = lr.SelectionId;
            change.Orc.Add(orc);

            Order o = new Order();
            o.Status = Order.StatusEnum.E;
            o.Id = newbetid++.ToString();

            o.Pd = now.ToUnixTimeMilliseconds();
            o.Side = side;
            o.Sm = 0;
            o.Sr = new_stake;
            o.P = side == Order.SideEnum.B ? lr.BackValues[0].price : lr.LayValues[0].price;
            o.S = new_stake;
            o.Sc = 0;
            orc.Uo.Add(o);

            return JsonConvert.SerializeObject(change);
        }
        private String matchbet(LiveRunner lr)
        {
            if (Rows.Count <= 0)
                return "";

            Int32 match_stake = 10;
            amount_remaining -= match_stake;

            Row r = FindUnmatchedRow(lr);
            if (r == null)
                return null;

            UInt64 betid = r.BetID;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            OrderMarketChange change = new OrderMarketChange();

            change.Orc = new List<OrderRunnerChange>();
            OrderRunnerChange orc = new OrderRunnerChange();
            orc.Uo = new List<Order>();
            orc.Id = lr.SelectionId;
            change.Orc.Add(orc);

            Order o = new Order();
            o.Status = Order.StatusEnum.E;
            o.Id = betid.ToString();

            o.Pd = now.ToUnixTimeMilliseconds();
            o.P = r.Odds;
            o.Avp = r.Odds;
            o.Sm = match_stake;
            o.Sr = amount_remaining;
            o.S = match_stake;
            o.Sc = 0;
            orc.Uo.Add(o);
            return JsonConvert.SerializeObject(change);
        }
        Int32 json_index = 0;
        String[] json_rows = new String[0];
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Betfair == null)
            {
                Betfair = MainWindow.Betfair;
            }
            Button b = sender as Button;
            switch (b.Tag)
            {
                case "Reset":
                    MarketNode = new NodeViewModel("json") { MarketID = "1.448881" };
                    json_rows = File.ReadAllLines(".\\notifications.json");
                    json_index = 0;
                    Rows.Clear();
                    break;
                case "Run":
                    MarketNode = new NodeViewModel("json") { MarketID = "1.448881" };
                    json_rows = File.ReadAllLines(".\\notifications.json");
                    json_index = 0;
                    foreach (String j in json_rows)
                    {
                        json_index++;
                        OnOrderChanged(j);
                    }
                    break;
                case "Next":
                    if (json_index >= json_rows.Length)
                        break;
                    String json_row = json_rows[json_index];
                    OnOrderChanged(json_row);
                    json_index++;
                    break;
                case "Back1":
                    OnOrderChanged(newbet(MarketNode.LiveRunners[0], Order.SideEnum.B)); break;
                case "Lay1":
                    OnOrderChanged(newbet(MarketNode.LiveRunners[0], Order.SideEnum.L)); break;
                case "Match1":
                    OnOrderChanged(matchbet(MarketNode.LiveRunners[0])); break;
                case "Back2":
                    OnOrderChanged(newbet(MarketNode.LiveRunners[1], Order.SideEnum.B)); break;
                case "Lay2":
                    OnOrderChanged(newbet(MarketNode.LiveRunners[1], Order.SideEnum.L)); break;
                case "Match2":
                    OnOrderChanged(matchbet(MarketNode.LiveRunners[1])); break;
                case "Stream": if (IsConnected) Disconnect(); else Connect(); break;
                case "CancelAll":
                    BackgroundWorker bw = new BackgroundWorker();
                    String result = "";
                    bw.RunWorkerCompleted += (o, e2) => { Status = result; };
                    bw.DoWork += (o, e2) =>
                    {
                        if (MarketNode != null)
                        {
                            CancelExecutionReport report = Betfair.cancelOrders(MarketNode.MarketID, null);
                            if (report != null)
                                result = report.errorCode != null ? report.errorCode : report.status;
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
