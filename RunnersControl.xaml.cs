using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpreadTrader
{
	#region Properties
	//public class MarketSnapDto
	//{
	//	public String MarketId { get; set; }
	//	public bool InPlay { get; set; }
	//	public MarketDefinition.StatusEnum Status { get; set; }
	//	public DateTime Time { get; set; }
	//	public List<MarketRunnerSnapDto> Runners { get; set; }
	//}
	//public class MarketRunnerSnapDto
	//{
	//	public long SelectionId { get; set; }
	//	public MarketRunnerPricesDto Prices { get; set; }
	//}
	//public class MarketRunnerPricesDto
	//{
	//	public List<PriceDto> Back { get; set; }
	//	public List<PriceDto> Lay { get; set; }
	//}
	//public class PriceDto
	//{
	//	public double Price { get; set; }
	//	public double Size { get; set; }
	//}
	#endregion Properties

	public partial class RunnersControl : UserControl, INotifyPropertyChanged
    {
# region
        private NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); 
            //    NotifyPropertyChanged("MarketNode"); 
            } }
		public double BackBook { get { return MarketNode == null ? 0.00 : MarketNode.BackBook; } }
        public double LayBook { get { return MarketNode == null ? 0.00 : MarketNode.LayBook; } }
        //public List<LiveRunner> LiveRunners { get { return MarketNode?.LiveRunners ?? new List<LiveRunner>(); } }
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
#endregion
        private async Task<List<MarketProfitAndLoss>> GetProfitAndLossAsync(string marketId)
        {
            return await Task.Run(() =>
            {
                return MainWindow.Betfair.listMarketProfitAndLoss(marketId);
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
        public void PopulateNewMarket(NodeViewModel node)
        {
			_MarketNode = node;
			MarketNode.GetLiveRunners();
			LiveRunners = MarketNode.LiveRunners;
			NotifyPropertyChanged("");
		}
        public LiveRunner GetRunnerFromSelectionID(Int64 selid)
        {
            if (LiveRunners != null)
            {
				Int32 i = 0;
                foreach (LiveRunner runner in LiveRunners)
                {
                    if (runner.SelectionId == selid)
                    {
						runner.Index = i;
                        return runner;
                    }
					i++;
                }
            }
            return null;
        }
        async Task RunLoop()
        {
            while (true)
            {
                await Task.Delay(50);
                if (props.FlashYellow && LiveRunners != null)
                {
                    foreach (LiveRunner lr in LiveRunners)
                    {
                        foreach (PriceSize ps in lr.BackValues)
                        {
                            if (ps.lastFlashTime.HasValue)
                            {
                                var elapsed = (DateTime.UtcNow - ps.lastFlashTime.Value).TotalMilliseconds;

                                if (elapsed >= 200)
                                {
                                    ps.lastFlashTime = null;
                                }
                            }
                        }
                        foreach (PriceSize ps in lr.LayValues)
                        {
                            if (ps.lastFlashTime.HasValue)
                            {
                                var elapsed = (DateTime.UtcNow - ps.lastFlashTime.Value).TotalMilliseconds;

                                if (elapsed >= 200)
                                {
                                    ps.lastFlashTime = null;
                                }
                            }
                        }
                    }
                }
            }
        }
        public void OnMarketChanged(MarketChangeDto change)
        {
            var epoch = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long diff = DateTime.UtcNow.Ticks - change.Time.Ticks;
			String cs = $"{1000 * diff / TimeSpan.TicksPerSecond}ms";
            ControlMessenger.Send("Update Market Latency", new { MarketLatency = cs, Status = "" });

            try
            {
                if (LiveRunners == null || change.Runners == null)
                    return;

                foreach (RunnerChangeDto runner in change.Runners)
                {
                    LiveRunner lr = GetRunnerFromSelectionID(runner.Id);
                    if (lr == null)
                        return;

                    String runner_name = lr.Name;

                    if (props.FlashYellow)
                    {
                        if (runner.Trd != null && runner.Trd.Count > 0)
                        {
                            foreach (var ti in runner.Trd)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    PriceSize ps = lr.BackValues[i];
                                    if (ps.price == ti[0].Value)
                                    {
                                        lr.BackValues[i].lastFlashTime = DateTime.UtcNow;
                                    }
                                    ps = lr.LayValues[i];
                                    if (ps.price == ti[0].Value)
                                    {
                                        lr.LayValues[i].lastFlashTime = DateTime.UtcNow;
                                    }
                                }
                            }
                        }
                    }

                    if (runner.Ltp != null)
                    {
                        lr.LastPriceTraded = runner.Ltp.Value;
                    }
                    
                    if (runner.Bdatb != null)
                    {
                        foreach (PriceLevelDto lv in runner.Bdatb)
                        {
                            lr.BackValues[lv.Level].Update(lv.Price, lv.Size);
                        }
                    }
                    if (runner.Bdatl != null)
                    {
                        foreach (PriceLevelDto lv in runner.Bdatl)
                        {
                            lr.LayValues[lv.Level].Update(lv.Price, lv.Size);
                        }
                    }
                }
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
            }
        }
		private void OnMessageReceived(string messageName, object data)
        {
            if (messageName == "Update P&L")
            {
                _ = UpdateRunnerPnLAsync();
            }
        }
        private async void StartRunLoop()
        {
            _ = RunLoop();
        }
        public RunnersControl()
        {
            InitializeComponent();
            StartRunLoop();
            ControlMessenger.MessageSent += OnMessageReceived;
        }
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
            ControlMessenger.Send("Favorite Changed", new { Favorite = LiveRunner.Favorite, Name = LiveRunner.Favorite.Name });

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
                        ControlMessenger.Send("Update P&L");
                        Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
                    });
                    t.Start();
                }
                if (live_runner.LevelSide == sideEnum.BACK)
                {
                    System.Threading.Thread t = new System.Threading.Thread(() =>
                    {
                        PlaceExecutionReport report = betfair.placeOrder(MarketNode.MarketID, live_runner.SelectionId, sideEnum.BACK, Math.Abs(live_runner.LevelStake), live_runner.BackValues[0].price);
                        ControlMessenger.Send("Update P&L");
                        Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = report.errorCode != null ? report.instructionReports[0].errorCode : report.status; }));
                    });
                    t.Start();
                }
            }
        }
    }
}