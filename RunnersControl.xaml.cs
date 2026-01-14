using Betfair.ESAClient.Cache;
using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpreadTrader
{
	public partial class RunnersControl : UserControl, INotifyPropertyChanged
    {
        #region
        MarketStateEngine _marketStateEngine = new MarketStateEngine();
		private NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); } }

        private double _backBook;
        private double _layBook;
		public double BackBook { set { if (_backBook != value) { _backBook = value; OnPropertyChanged(nameof(BackBook)); OnPropertyChanged(nameof(BackBookString)); } } }
		public double LayBook { set { if (_layBook != value) { _layBook = value; OnPropertyChanged(nameof(LayBook)); OnPropertyChanged(nameof(LayBookString)); } } }
		public String BackBookString { get { if (_backBook == 0)
                    return "88";
                return MarketNode == null ? "" : _backBook.ToString("0.00")+"%"; } }
		public String LayBookString { get { return MarketNode == null ? "" : _layBook.ToString("0.00")+"%"; } }
		public List<LiveRunner> LiveRunners { get; set; }
		private Dictionary<long, LiveRunner> RunnerBySelectionId = new Dictionary<long, LiveRunner>();
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
		}
#endregion
        public void PopulateNewMarket(NodeViewModel node)
        {
			_MarketNode = node;
			MarketNode.GetLiveRunners();
			LiveRunners = MarketNode.LiveRunners;
            foreach (var runner in LiveRunners)
            {
                RunnerBySelectionId[runner.SelectionId] = runner;
            }
			OnPropertyChanged("");

			_marketStateEngine.Start(_MarketNode.Market);

			_marketStateEngine.TelemetryAvailable += telemetry =>
			{
				Dispatcher.Invoke(() =>
				{
					BackBook = telemetry.BackBook;
					LayBook = telemetry.LayBook;
                    foreach(var pnl in telemetry.ProfitAndLosses)
                    {
                        RunnerBySelectionId[pnl.selectionId].ifWin = pnl.ifWin;
                    }
				});
			};
		}
        public void OnMarketChanged(MarketChangeDto change)
        {
			if (MarketNode == null)
				return;

			if (MarketNode.MarketID != change.MarketId)
				return;

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
                    LiveRunner lr;
                    RunnerBySelectionId.TryGetValue(runner.Id, out lr) ;
                    if (lr == null)
                        return;

                    String runner_name = lr.Name;

                    if (runner.Trd != null)
                    {
                        foreach (var ti in runner.Trd)
                        {
                            for (int i = 0; i < 3; i++)
                            {
								if (lr.BackValues[i].price == ti[0])
								    lr.BackValues[i].TradedVolume = ti[1].Value;
                            }
                            for (int i = 0; i < 3; i++)
                            {
								if (lr.LayValues[i].price == ti[0])
								    lr.LayValues[i].TradedVolume = ti[1].Value;
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
        }
        public RunnersControl()
        {
            InitializeComponent();
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