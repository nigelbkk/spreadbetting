using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Betfair.ESASwagger.Model;
using System.Linq;

namespace SpreadTrader
{
    public delegate void MarketChangedDelegate(NodeViewModel node);
    public delegate void StreamUpdateDelegate(String marketid, List<LiveRunner> liveRunners, double tradedVolume, List<RunnerChange> last_traded, bool inplay);
//    public delegate void StreamUpdateDelegate(String marketid, List<LiveRunner> liveRunners, double tradedVolume, List<Tuple<long, double?, double?>> last_traded, bool inplay);
    public partial class RunnersControl : UserControl, INotifyPropertyChanged
    {
        public BetsManager betsManager = null;
        public MarketSelectionDelegate OnMarketSelected;
        public StreamUpdateDelegate StreamUpdateEventSink = null;
        public FavoriteChangedDelegate OnFavoriteChanged = null;
        public MarketChangedDelegate OnMarketChanged = null;
        private StreamingAPI streamingAPI = new StreamingAPI();
        private BackgroundWorker Worker = null;
        public NodeViewModel _MarketNode { get; set; }
        public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); NotifyPropertyChanged(""); } }
        public double BackBook { get { return MarketNode == null ? 0.00 : MarketNode.BackBook; } }
        public double LayBook { get { return MarketNode == null ? 0.00 : MarketNode.LayBook;  } }
        public List<LiveRunner> LiveRunners { get; set; }
        private Properties.Settings props = Properties.Settings.Default;
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public void UpdateMarketStatus()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                //Overlay.Visibility = MarketNode.Status == marketStatusEnum.OPEN ? Visibility.Hidden : Visibility.Visible;
                //OverlayText.Text = MarketNode.Status.ToString();
            }));
        }
        String RunnerFromSelid(long selid)
        {
            foreach (var runner in LiveRunners)
            {
                if (runner.SelectionId == selid)
                {
                    return runner.Name;
                }
            }
            return null;
        }

        Int32? GetCellidFromPricePoint(Int32 runner_id, sideEnum side, double price)
        {
            for (int i = 0; i < 3; i++)
            {
                if (side == sideEnum.BACK)
                    if (LiveRunners[runner_id].BackValues[i].price == price)
                        return i;
                if (side == sideEnum.LAY)
                    if (LiveRunners[runner_id].LayValues[i].price == price)
                        return i;
            }
            return null;
        }

        Tuple<int, List<Int32>, List<Int32>> GetCellIDs(RunnerChange rc)   // for a single last traded item
        { 
            int runner_id = 0;
            long selid = rc.Id.Value;
            double? price = null;
            foreach (var runner in LiveRunners)
            {
                if (runner.SelectionId == selid)
                {
                    String runner_name = LiveRunners[runner_id].Name;
                    List<Int32> back_ids = new List<Int32>();
                    List<Int32> lay_ids = new List<Int32>();
                    
                    if (rc.Ltp == null && rc.Tv == null)
                        return null;
                    if (rc.Ltp == null && rc.Tv != null)
                        price = LiveRunners[runner_id].LastPriceTraded;
                    else
                        price = rc.Ltp.Value;
                    
                    if (rc.Atb != null)
                    {
                        int i = 0;
                        foreach (var atb in rc.Atb)
                        {
                            double? ltp = atb[0];
                            if (price == ltp)
                            {
                                Int32? cell_id = GetCellidFromPricePoint(runner_id, sideEnum.BACK,  ltp.Value);
                                if (cell_id != null)
                                    back_ids.Add(cell_id.Value);
                            }
                            i++;
                        }
                    }
                    if (rc.Atl != null)
                    {
                        int i = 0;
                        foreach (var atl in rc.Atl)
                        {
                            double? ltp = atl[0];
                            if (price == ltp)
                            {
                                Int32? cell_id = GetCellidFromPricePoint(runner_id, sideEnum.LAY, ltp.Value);
                                if (cell_id != null)
                                    lay_ids.Add(cell_id.Value);
                            }
                            i++;
                        }
                    }
                    return new Tuple<Int32, List<Int32>, List<Int32>> (runner_id, back_ids, lay_ids);
                }
                runner_id++;
            }
            return null;
        }

        void FlashTraded(List<LiveRunner> liveRunners, List<RunnerChange> Rc)
        {
            if (Rc == null)
                return;

            try
            {
                foreach (var rc in Rc)
                {
                    if (rc.Ltp != null)
                    {
                    }
                    String RunnerName = RunnerFromSelid(rc.Id.Value);

                    Tuple<int, List<Int32>, List<Int32>> cell_ids = GetCellIDs(rc);
                    if (cell_ids != null)
                    {
                        foreach (Int32 cell_id in cell_ids.Item2)
                        {
                            Debug.WriteLine($"Back: {RunnerName} : {rc.Ltp} : cell id = {cell_id}");
                            liveRunners[cell_ids.Item1].BackValues[cell_id].CellBackgroundColor = Brushes.Yellow;
                        }
                        foreach (Int32 cell_id in cell_ids.Item3)
                        {
                            Debug.WriteLine($"Lay:  {RunnerName} : {rc.Ltp} : cell id = {cell_id}");
                            liveRunners[cell_ids.Item1].LayValues[cell_id].CellBackgroundColor = Brushes.Yellow;
                        }
                        NotifyPropertyChanged("");
                    }
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        private async Task<List<MarketProfitAndLoss>> GetProfitAndLossAsync(string marketId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return MainWindow.Betfair.listMarketProfitAndLoss(marketId);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return null;
                }
            });
        }

        public async Task UpdateRunnerPnLAsync()
        {
            var plList = await GetProfitAndLossAsync(MarketNode.MarketID);
            var pl = plList?.FirstOrDefault();
            if (pl == null || pl.profitAndLosses == null)
                return;

            // Build lookup for speed
            var lookup = pl.profitAndLosses.ToDictionary(x => x.selectionId);

            foreach (var runner in LiveRunners)
            {
                if (lookup.TryGetValue(runner.SelectionId, out var pnl))
                {
                    runner.ifWin = pnl.ifWin;
                }
            }
        }

        public RunnersControl()
        {
            LiveRunners = new List<LiveRunner>();
            InitializeComponent();

            StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, Rc, inplay) =>
            {
                if (MarketNode != null && marketid == MarketNode.MarketID)
                {
                    FlashTraded(liveRunners, Rc);

                    if (MarketNode != null && marketid == MarketNode.MarketID)
                    {
                        FlashTraded(liveRunners, Rc);
                        double totalBack = 0;
                        double totalLay = 0;
                        Int32 ct = Math.Min(LiveRunners.Count, liveRunners.Count); //TOCHECK
                        for (int i = 0; i < ct; i++)
                        {
                            if (liveRunners[i].BackValues[0].price > 0)
                                totalBack += 100 / liveRunners[i].BackValues[0].price;
                            if (liveRunners[i].LayValues[0].price > 0)
                                totalLay += 100 / liveRunners[i].LayValues[0].price;
                            LiveRunners[i].BackValues = liveRunners[i].BackValues;
                            LiveRunners[i].LayValues = liveRunners[i].LayValues;
                            LiveRunners[i].LastPriceTraded = liveRunners[i].LastPriceTraded;
                            LiveRunners[i].LevelProfit = liveRunners[i].LevelProfit;
                            LiveRunners[i].BackLayRatio = liveRunners[i].BackLayRatio;
                            LiveRunners[i].TradedVolume = liveRunners[i].TradedVolume;
                            LiveRunners[i].NotifyPropertyChanged("");
                        }
                        MarketNode.LiveRunners = LiveRunners;
                        MarketNode.CalculateLevelProfit();
                        MarketNode.TotalMatched = tradedVolume;
                        if (Worker.IsBusy)
                            Worker.CancelAsync();
                    }
                    _ = UpdateRunnerPnLAsync();

                };
                OnMarketSelected += (node) =>
                {
                    if (IsLoaded)
                    {
                        MarketNode = node;
                        try
                        {
                            LiveRunners = MarketNode.GetLiveRunners();
                            //Debug.WriteLine(MarketNode.Status);
                            if (props.UseStreaming)
                            {
                                BackgroundWorker bw = new BackgroundWorker();
                                bw.DoWork += (o, e) =>
                                {
                                    try
                                    {
                                        streamingAPI.Start(MarketNode.MarketID);
                                    }
                                    catch (Exception xe)
                                    {
                                        Debug.WriteLine(xe.Message);
                                        //Extensions.MainWindow.Status = xe.Message;
                                    }
                                };
                                bw.RunWorkerAsync();
                            }
                            else
                            {
                                streamingAPI.Stop();
                            }
                            if (!Worker.IsBusy)
                                Worker.RunWorkerAsync();
                        }
                        catch (Exception xe)
                        {
                            Debug.WriteLine(xe.Message);
                            Extensions.MainWindow.Status = xe.Message;
                        }
                    }
                };

                Worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
                Worker.ProgressChanged += (o, e) =>
                {
                    List<LiveRunner> NewRunners = e.UserState as List<LiveRunner>;

                    if (NewRunners != null)
                    {
                        if (LiveRunners.Count != NewRunners.Count)
                        {
                            LiveRunners = NewRunners;
                        }
                        if (LiveRunners.Count > 0) foreach (LiveRunner lr in LiveRunners)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    lr.BackValues[i] = new PriceSize(i);
                                    lr.LayValues[i] = new PriceSize(i + 3);
                                }
                            }
                        for (int i = 0; i < LiveRunners.Count; i++)
                        {
                            if (NewRunners[i].ngrunner != null)
                            {
                                LiveRunners[i].SetPrices(NewRunners[i].ngrunner);
                                LiveRunners[i].LevelProfit = NewRunners[i].LevelProfit;
                                LiveRunners[i].LevelStake = NewRunners[i].LevelStake;
                                LiveRunners[i].LevelSide = NewRunners[i].LevelSide;
                                LiveRunners[i].NotifyPropertyChanged("");
                            }
                        }
                        MarketNode.UpdateRate = e.ProgressPercentage;
                        NotifyPropertyChanged("");
                    }
                };
                Worker.DoWork += (o, ea) =>
                {
                    BackgroundWorker sender = o as BackgroundWorker;
                    while (!sender.CancellationPending)
                    {
                        try
                        {
                            if (MarketNode != null)
                            {
                                //var runners = streamingAPI.LiveRunners;
                                DateTime LastUpdate = DateTime.UtcNow;
                                var lr = MarketNode.GetLiveRunners();
                                UpdateMarketStatus();

                                if (OnMarketChanged != null)
                                {
                                    OnMarketChanged(MarketNode);
                                }
                                NotifyPropertyChanged("");
                                Int32 rate = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
                                sender.ReportProgress(rate, lr);
                                if (stop_async)
                                    break;
                            }
                        }
                        catch (Exception xe)
                        {
                            Debug.WriteLine(xe.Message);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Extensions.MainWindow.Status = xe.Message;
                            }));
                        }
                        System.Threading.Thread.Sleep(props.WaitBF);
                    }
                };
                Worker.RunWorkerAsync();
            };
        }

        public String GetRunnerName(Int64 SelectionID)
        {
            if (LiveRunners.Count > 0) foreach (LiveRunner r in LiveRunners)
                {
                    if (r.SelectionId == SelectionID)
                        return r.Name;
                }
            return null;
        }
        bool stop_async = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            try
            {
                var parent = VisualTreeHelper.GetParent(b);
                ContentPresenter cp = Extensions.FindParentOfType<ContentPresenter>(b);
                if (cp != null)
                {
                    LiveRunner live_runner = cp.Content as LiveRunner;

                    var vb = VisualTreeHelper.GetChild(parent, 8);
                    TextBox tb = VisualTreeHelper.GetChild(parent, 8) as TextBox;
                    TextBox tl = VisualTreeHelper.GetChild(parent, 9) as TextBox;

                    var grid = VisualTreeHelper.GetChild(b, 0);
                    grid = VisualTreeHelper.GetChild(grid, 0);
                    var sp = VisualTreeHelper.GetChild(grid, 0);
                    var t1 = VisualTreeHelper.GetChild(sp, 0) as TextBlock;

                    String t1s = t1.Text;

                    if (String.IsNullOrEmpty(t1.Text))
                    {
                        t1s = "0";
                    }

                    double odds = Convert.ToDouble(t1s);

                    String side = "Back";
                    for (int i = 1; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                    {
                        if (VisualTreeHelper.GetChild(parent, i) == b && i > 3)
                        {
                            side = "Lay";
                            break;
                        }
                    }
                    ConfirmationDialog dlg = new ConfirmationDialog(this, MarketNode.MarketID, live_runner, side, odds);
                    dlg.ShowDialog();
                }
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
                Extensions.MainWindow.Status = xe.Message;
            }
        }
        private void NameMouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lb = sender as Label;
            ContentPresenter cp = Extensions.FindParentOfType<ContentPresenter>(lb);
            if (LiveRunner.Favorite != null) LiveRunner.Favorite.IsFavorite = false;
            LiveRunner.Favorite = cp.Content as LiveRunner;
            LiveRunner.Favorite.IsFavorite = true;
            if (OnFavoriteChanged != null)
            {
                OnFavoriteChanged(LiveRunner.Favorite);
            }
        }
        private void LevelProfitMouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lb = sender as Label;
            ContentPresenter cp = Extensions.FindParentOfType<ContentPresenter>(lb);
            LiveRunner live_runner = cp.Content as LiveRunner;

            BetfairAPI.BetfairAPI betfair = MainWindow.Betfair;

            if (LiveRunners.Count == 3)
            {
                LiveRunner live_runner1 = LiveRunners[0];
                double odds1 = live_runner1.LevelSide == sideEnum.LAY ? live_runner1.LayValues[0].price : live_runner1.BackValues[0].price;
                LiveRunner live_runner2 = LiveRunners[1];
                double odds2 = live_runner2.LevelSide == sideEnum.LAY ? live_runner2.LayValues[0].price : live_runner2.BackValues[0].price;

                List<PlaceInstruction> orders = new List<PlaceInstruction>();
                orders.Add(new PlaceInstruction()
                {
                    orderTypeEnum = orderTypeEnum.LIMIT,
                    sideEnum = live_runner1.LevelSide,
                    Runner = live_runner1.Name,
                    marketTypeEnum = marketTypeEnum.WIN,
                    selectionId = live_runner1.SelectionId,
                    limitOrder = new LimitOrder()
                    {
                        persistenceTypeEnum = persistenceTypeEnum.LAPSE,
                        price = odds1,
                        size = Math.Abs(live_runner1.LevelStake),
                    }
                });
                orders.Add(new PlaceInstruction()
                {
                    orderTypeEnum = orderTypeEnum.LIMIT,
                    sideEnum = live_runner2.LevelSide,
                    Runner = live_runner2.Name,
                    marketTypeEnum = marketTypeEnum.WIN,
                    selectionId = live_runner2.SelectionId,
                    limitOrder = new LimitOrder()
                    {
                        persistenceTypeEnum = persistenceTypeEnum.LAPSE,
                        price = odds1,
                        size = Math.Abs(live_runner2.LevelStake),
                    }
                });
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    PlaceExecutionReport report = betfair.placeOrders(MarketNode.MarketID, orders);
                    Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
                });
                t.Start();
            }
            else if (LiveRunners.Count == 2)
            {
                if (live_runner.LevelSide == sideEnum.LAY)
                {
                    System.Threading.Thread t = new System.Threading.Thread(() =>
                    {
                        PlaceExecutionReport report = betfair.placeOrder(MarketNode.MarketID, live_runner.SelectionId, sideEnum.LAY, Math.Abs(live_runner.LevelStake), live_runner.LayValues[0].price);
                        Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
                    });
                    t.Start();
                }
                if (live_runner.LevelSide == sideEnum.BACK)
                {
                    System.Threading.Thread t = new System.Threading.Thread(() =>
                    {
                        PlaceExecutionReport report = betfair.placeOrder(MarketNode.MarketID, live_runner.SelectionId, sideEnum.BACK, Math.Abs(live_runner.LevelStake), live_runner.BackValues[0].price);
                        Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
                    });
                    t.Start();
                }
            }
        }
    }
}