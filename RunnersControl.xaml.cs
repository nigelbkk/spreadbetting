using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpreadTrader
{
	public partial class RunnersControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		private BackgroundWorker Worker = null;
		public NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode;  } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); NotifyPropertyChanged(""); } }
		public bool IsSelected { get; set; }
		public double BackBook { get { return _MarketNode.Market.MarketBook == null ? 0.00 : _MarketNode.Market.MarketBook.BackBook; } }
		public double LayBook { get { return _MarketNode.Market.MarketBook == null ? 0.00 : _MarketNode.Market.MarketBook.LayBook; } }
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
		public RunnersControl()
		{
			MarketNode = new NodeViewModel(new BetfairAPI.BetfairAPI());
			LiveRunners = new List<LiveRunner>();
			InitializeComponent();

			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					LiveRunners = MarketNode.GetLiveRunners();
				}
			};

			Worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			Worker.ProgressChanged += (o, e) =>
			{
				List<LiveRunner> NewRunners = e.UserState as List<LiveRunner>;

				if (LiveRunners.Count != NewRunners.Count)
				{
					LiveRunners = NewRunners;
				}
				for (int i=0;i<LiveRunners.Count;i++)
				{
					LiveRunners[i].SetPrices(NewRunners[i].ngrunner);
				}
				MarketNode.UpdateRate = e.ProgressPercentage;
				NotifyPropertyChanged("");
			};
			Worker.DoWork += (o, ea) =>
			{
				BackgroundWorker sender = o as BackgroundWorker;
				while (!sender.CancellationPending)
				{
					try
					{
						if (MarketNode != null && MarketNode.MarketName != null && IsSelected)
						{
							DateTime LastUpdate = DateTime.UtcNow;
							var lr = MarketNode.GetLiveRunners();
							Int32 rate = (Int32) ((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
							sender.ReportProgress(rate, lr);
						}
					}
					catch (Exception xe)
					{
						Debug.WriteLine(xe.Message);
					}
					//break;
					System.Threading.Thread.Sleep(props.WaitBF);
				}
			};
			Worker.RunWorkerAsync();
		}

		private PlaceExecutionReport placeOrder(String marketId, LiveRunner runner, sideEnum side,  PriceSize ps)
		{
			BetfairAPI.BetfairAPI Betfair = new BetfairAPI.BetfairAPI();

			PlaceInstruction pi = new PlaceInstruction()
			{
				orderTypeEnum = orderTypeEnum.LIMIT,
				sideEnum = side,
				Runner = runner.Name,
				marketTypeEnum = marketTypeEnum.WIN,
				selectionId = runner.SelectionId,
				limitOrder = new LimitOrder()
				{
					persistenceTypeEnum = persistenceTypeEnum.LAPSE,
					price = ps.price,
					size = ps.size,
				}
			};
			using (StreamWriter w = File.AppendText(props.LogFile))
			{
					w.WriteLine(MarketNode.FullName + "," + pi);
					Debug.WriteLine(pi);
			}
			//PlaceExecutionReport report = Betfair.placeOrders(bet.MarketId, instructions);
			return new PlaceExecutionReport();
		}
		public void SubmitBets(PriceSize[] LayValues, PriceSize[] BackValues)
		{
			if (MarketNode != null)
			{
				BetfairAPI.BetfairAPI betfairAPI = new BetfairAPI.BetfairAPI();
				bool auto_back_lay = SliderControl.AutoBackLay;
				List<PriceSize> laybets = new List<PriceSize>();
				List<PriceSize> backbets = new List<PriceSize>();
				for (Int32 i = 0; i < 9; i++)
				{
					laybets.Add(LayValues[i]);
					backbets.Add(BackValues[i]);
				}
				laybets.Sort((a, b) => Math.Sign(b.price - a.price));
				backbets.Sort((a, b) => Math.Sign(a.price - b.price));
				if (LiveRunner.Favorite == null)
				{
					MessageBox.Show("Please Choose a Favourite", "Submit Bets", MessageBoxButton.OK, MessageBoxImage.Exclamation );
					return;
				}
				for (Int32 i = 0; i < 9; i++)
				{
					if (laybets[i].IsChecked)
					{
						placeOrder(MarketNode.Market.marketId, LiveRunner.Favorite, sideEnum.LAY, laybets[i]);
					}
				}
				for (Int32 i = 0; i < 9; i++)
				{
					if (backbets[i].IsChecked)
					{
						placeOrder(MarketNode.Market.marketId, LiveRunner.Favorite, sideEnum.BACK, backbets[i]);
					}
				}			}
		}
		public String GetRunnerName(Int64 SelectionID)
		{
			foreach(LiveRunner r in LiveRunners)
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
				var parent2 = VisualTreeHelper.GetParent(parent);
				var parent3 = VisualTreeHelper.GetParent(parent2);
				var parent4 = VisualTreeHelper.GetParent(parent3);
				var parent5 = VisualTreeHelper.GetParent(parent4);
				ContentPresenter cp = parent5 as ContentPresenter;
				LiveRunner live_runner = cp.Content as LiveRunner;

				var vb = VisualTreeHelper.GetChild(parent, 8);
				TextBox tb = VisualTreeHelper.GetChild(parent, 8) as TextBox;
				TextBox tl = VisualTreeHelper.GetChild(parent, 9) as TextBox;

				var grid = VisualTreeHelper.GetChild(b, 0);
				grid = VisualTreeHelper.GetChild(grid, 0);
				var sp = VisualTreeHelper.GetChild(grid, 0);
				var t1 = VisualTreeHelper.GetChild(sp, 0) as TextBlock;

				double odds = Convert.ToDouble(t1.Text);

				String side = "Back";
//				Int32 stake = Convert.ToInt32(tb.Text);
				for (int i = 1; i < VisualTreeHelper.GetChildrenCount(parent); i++)
				{
					if (VisualTreeHelper.GetChild(parent, i) == b && i > 3)
					{
						side = "Lay";
	//					stake = Convert.ToInt32(tl.Text);
						break;
					}
				}
				ConfirmationDialog dlg = new ConfirmationDialog(this, b, live_runner, side, odds);
				dlg.ShowDialog();
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SV1.Height = Math.Max(25, e.NewSize.Height - Header.Height);
		}
		private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			Label lb = sender as Label;
			try
			{
				var parent = VisualTreeHelper.GetParent(lb);
				var parent2 = VisualTreeHelper.GetParent(parent);
				var parent3 = VisualTreeHelper.GetParent(parent2);
				var parent4 = VisualTreeHelper.GetParent(parent3);
				var parent5 = VisualTreeHelper.GetParent(parent4);
				ContentPresenter cp = parent5 as ContentPresenter;
				if (LiveRunner.Favorite != null) LiveRunner.Favorite.IsFavorite = false;
				LiveRunner.Favorite = cp.Content as LiveRunner;
				LiveRunner.Favorite.IsFavorite = true;
			}
			catch (Exception xe)
			{

			}
		}
	}
	public static class Extensions
	{
		public static T FindParentOfType<T>(this DependencyObject child) where T : DependencyObject
		{
			DependencyObject parentDepObj = child;
			do
			{
				parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
				T parent = parentDepObj as T;
				if (parent != null) return parent;
			}
			while (parentDepObj != null);
			return null;
		}
	}
}