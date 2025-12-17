using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpreadTrader
{
	#region Properties
	public class MarketSnapDto
	{
		public String MarketId { get; set; }
		public bool InPlay { get; set; }
		public DateTime Time { get; set; }
		public MarketDefinition.StatusEnum? Status { get; set; }

		public List<MarketRunnerSnapDto> Runners { get; set; }
	}
	public class MarketRunnerSnapDto
	{
		public long SelectionId { get; set; }
		public MarketRunnerPricesDto Prices { get; set; }
	}
	public class MarketRunnerPricesDto
	{
		public List<PriceDto> Back { get; set; }
		public List<PriceDto> Lay { get; set; }
	}
	public class PriceDto
	{
		public double Price { get; set; }
		public double Size { get; set; }
	}
	#endregion Properties

	public partial class RunnersControl : UserControl, INotifyPropertyChanged
    {
# region
        private NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); NotifyPropertyChanged(""); } }
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
        public async Task PopulateNewMarketAsync(NodeViewModel node)
        {
			if (_MarketNode != null && node.MarketID != _MarketNode.MarketID)
			{
				Debug.WriteLine($"Not our market: {MarketNode.FullName}");
                return;
			}
			_MarketNode = node;
			MarketNode.GetLiveRunners();
			LiveRunners = MarketNode.LiveRunners;
			NotifyPropertyChanged("");
		}
        LiveRunner GetRunnerFromSelectionID(Int64 selid)
        {
            foreach (LiveRunner runner in LiveRunners)
            {
                if (runner.SelectionId == selid)
                {
                    return runner;
                }
            }
            return null;
        }

		//private async void OnMarketChangedAsync(MarketChange mc)
  //      {
		//	try
		//	{
		//		foreach (var rs in mc?.Rc)
		//		{
		//			if (rs == null)
		//				return;

		//			LiveRunner lr = RunnerFromSelid((long)rs?.Id.Value);

		//			if (lr == null)
		//				return;

		//			//double? traded_volume = rs.Tv;
		//			List<List<double?>> backs = rs.Batb;

		//			if (backs != null)
		//			{
  //                      foreach (List<double?> back in backs)
  //                      {
  //                          Int32 i = (Int32)back[0].Value;
		//					Debug.WriteLine($"Back: {lr.Name} : {back[1].Value} : cell id = {i}");

  //                          lr.BackValues[i].price = back[1].Value;
  //                          lr.BackValues[i].size = back[2].Value;
  //                          //lr.BackValues[i].CellBackgroundColor = Brushes.Yellow;
  //                          i++;
  //                      }
  //                  }
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		Debug.WriteLine(e.Message);
		//	}

		//}
		public void OnMarketChanged(MarketSnapDto snap)
		{
			try
			{
                if (snap != null)
                {
                    //Debug.WriteLine($"our market: {MarketNode.FullName}");
                    //Debug.Assert(snap.MarketId == _MarketNode.MarketID);

                    var epoch = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    long unixSeconds = (long)epoch.TotalSeconds;
                    long diff = DateTime.UtcNow.Ticks - snap.Time.Ticks;
                    String cs = $"{Math.Round(1000 * diff / (double)TimeSpan.TicksPerSecond)}ms";
                    //Debug.WriteLine(cs);
                    ControlMessenger.Send("Update Latency", new { Latency = cs, Status = snap.Status });

                    //double tradedVolume = 0;
                    if (LiveRunners != null)
                    {
                        foreach (MarketRunnerSnapDto rsnap in snap.Runners)
                        {
                            LiveRunner lr = GetRunnerFromSelectionID(rsnap.SelectionId);
                            if (lr == null)
                                continue;

                            Int32 i = 0;
                            foreach (var ps in rsnap.Prices.Back)
                            {
                                lr.BackValues[i].price = ps.Price;
                                lr.BackValues[i].size = ps.Size;
                                i++;
                            }

                            i = 0;
                            foreach (var ps in rsnap.Prices.Lay)
                            {
                                lr.LayValues[i].price = ps.Price;
                                lr.LayValues[i].size = ps.Size;
                                i++;
                            }
                        }
                    }
                    //_ = UpdateRunnerPnLAsync();

                    //List<Tuple<long, double?, double?>> last_traded = new List<Tuple<long, double?, double?>>();
                    //if (change?.Rc != null)
                    //{
                    //	foreach (RunnerChange rc in change?.Rc)  /// examine Atb abd Atl get determine correct side
                    //	{
                    //		if (rc.Tv != null && rc.Ltp == null)
                    //		{
                    //			last_traded.Add(new Tuple<long, double?, double?>((long)rc.Id, rc.Ltp, rc.Tv));
                    //		}
                    //		else if (rc.Ltp != null)
                    //		{
                    //			last_traded.Add(new Tuple<long, double?, double?>((long)rc.Id, rc.Ltp, rc.Tv));
                    //		}
                    //	}
                    //}
                    NotifyPropertyChanged("");
                    //Callback?.Invoke(e.Snap.MarketId, _LiveRunners, tradedVolume, e.Change.Rc, !e.Market.IsClosed && e.Snap.MarketDefinition.InPlay == true);
                }
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}

		LiveRunner RunnerFromSelid(long selid)
		{
			foreach (var runner in LiveRunners)
			{
				if (runner.SelectionId == selid)
				{
					return runner;
				}
			}
			return null;
		}

		/// //////////////////////////////////////////////
		/// //////////////////////////////////////////////
		/// //////////////////////////////////////////////
		void FlashTraded(MarketChange mc)
		{
			try
			{
				foreach (var rs in mc?.Rc)
				{
                    if (rs == null)
                        return;

					LiveRunner lr = RunnerFromSelid((long) rs?.Id.Value);

                    if (lr == null)
                        return;

					double? traded_volume = rs.Tv;
					List<List<double?>> back = rs.Batb;

                    if (back != null)
                    {
                        Int32 i = 0;
                        foreach (List<double?> lb in back)
                        {
                            foreach (double? b in lb)
                            {
                         //       Debug.WriteLine($"Back: {lr.Name} : cell id = {i}");
                        //        lr.BackValues[i].CellBackgroundColor = Brushes.Yellow;
                                i++;
                            }
                        }
                    }
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}
		}

		private void OnMessageReceived(string messageName, object data)
        {
			//if (messageName == "Market Selected")
			//{
			//	dynamic d = data;
			//	NodeViewModel d2 = d.NodeViewModel;
			//	if (MarketNode != null && d2.MarketID != MarketNode.MarketID)
			//	{
			//		Debug.WriteLine($"Not our market: {d2.FullName}");
			//		return;
			//	}
			//	Debug.WriteLine($"RunnersContrkL {d2.FullName}");
			//	_ = PopulateNewMarketAsync(d2);
			//}
			//if (messageName == "Market Changed")
			//{
			//	dynamic d = data;
			//	MarketSnapDto snap = d.MarketSnapDto;
			//	//if (LiveRunners != null)
			//	//	OnMarketChanged(snap);
			//}
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