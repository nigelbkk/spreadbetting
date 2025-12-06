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
using System.Linq;

namespace SpreadTrader
{
    public delegate void MarketChangedDelegate(Market node);
    public delegate void StreamUpdateDelegate(String marketid, List<LiveRunner> liveRunners, double tradedVolume, bool inplay);
    public partial class RunnersControl : UserControl, INotifyPropertyChanged
    {
        //public BetsManager betsManager = null;
        //public MarketSelectionDelegate OnMarketSelected;
        //public StreamUpdateDelegate StreamUpdateEventSink = null;
        //public MarketChangedDelegate OnMarketChanged = null;

		public Market MarketNode { get; set; }
        public double BackBook { get { return MarketNode == null ? 0.00 : MarketNode.BackBook; } }
        public double LayBook { get { return MarketNode == null ? 0.00 : MarketNode.LayBook;  } }
        public List<LiveRunner> LiveRunners { get { return MarketNode?.LiveRunners ?? new List<LiveRunner>(); } }
        private Properties.Settings props = Properties.Settings.Default;
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
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
        private async Task PopulateNewMarket(Market node)
        {
            MarketNode = node;
			NotifyPropertyChanged("");
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Selected")
			{
				dynamic d = data;
                Debug.WriteLine((String) d.Name);
                PopulateNewMarket(d.MarketNode);
			}
		}
		public RunnersControl()
        {
            InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;
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