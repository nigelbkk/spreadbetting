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
	public delegate void StreamUpdateDelegate(List<LiveRunner> liveRunners);

	public partial class RunnersControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public StreamUpdateDelegate StreamUpdateEventSink = null;
		private StreamingAPI streamingAPI = new StreamingAPI();
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

			StreamingAPI.Callback += (liveRunners) =>
			{
				Int32 ct = Math.Min(LiveRunners.Count, liveRunners.Count); //TODO
				for(int i=0;i<ct; i++)
				{
					LiveRunners[i].BackValues = liveRunners[i].BackValues;
					LiveRunners[i].LayValues = liveRunners[i].LayValues;
					LiveRunners[i].NotifyPropertyChanged("");
				}
				if (Worker.IsBusy)
					Worker.CancelAsync();
			};
			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					try
					{
						LiveRunners = MarketNode.GetLiveRunners();
						if (props.UseStreaming)
						{
							streamingAPI.Start(MarketNode.MarketID);
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
						MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
						if (mw != null) mw.Status = xe.Message;
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
					for (int i = 0; i < LiveRunners.Count; i++)
					{
						if (NewRunners[i].ngrunner != null)
						{
							LiveRunners[i].SetPrices(NewRunners[i].ngrunner);
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
						if (MarketNode != null && MarketNode.MarketName != null && IsSelected)
						{
							var runners = streamingAPI.LiveRunners;
							DateTime LastUpdate = DateTime.UtcNow;
							var lr = MarketNode.GetLiveRunners();
							Int32 rate = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
							sender.ReportProgress(rate, lr);
							if (stop_async)
								break;
						}
					}
					catch (Exception xe)
					{
						Debug.WriteLine(xe.Message);
						MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
						if (mw != null) mw.Status = xe.Message;
					}
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
				DateTime LastUpdate = DateTime.UtcNow;
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
					MessageBox.Show("Please Select the Favourite", "Submit Bets", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
				for (Int32 i = 0; i < 9; i++)
				{
					if (laybets[i].IsChecked && laybets[i].ParentChecked)
					{
						placeOrder(MarketNode.Market.marketId, LiveRunner.Favorite, sideEnum.LAY, laybets[i]);
					}
				}
				for (Int32 i = 0; i < 9; i++)
				{
					if (backbets[i].IsChecked && backbets[i].ParentChecked)
					{
						placeOrder(MarketNode.Market.marketId, LiveRunner.Favorite, sideEnum.BACK, backbets[i]);
					}
				}
				MarketNode.TurnaroundTime = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
			}
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

					double odds = Convert.ToDouble(t1.Text);

					String side = "Back";
					for (int i = 1; i < VisualTreeHelper.GetChildrenCount(parent); i++)
					{
						if (VisualTreeHelper.GetChild(parent, i) == b && i > 3)
						{
							side = "Lay";
							break;
						}
					}
					ConfirmationDialog dlg = new ConfirmationDialog(this, b, live_runner, side, odds);
					dlg.ShowDialog();
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
				MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
				if (mw != null) mw.Status = xe.Message;
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
				ContentPresenter cp = Extensions.FindParentOfType<ContentPresenter>(lb);
				if (LiveRunner.Favorite != null) LiveRunner.Favorite.IsFavorite = false;
				LiveRunner.Favorite = cp.Content as LiveRunner;
				LiveRunner.Favorite.IsFavorite = true;
			}
			catch (Exception xe)
			{
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			MainWindow mw2 = Extensions.FindParentOfType<MainWindow>(SV1);
			if (mw2 != null)
				mw2.OnShutdown += () =>
				{
					streamingAPI.Stop();
				};
		}
	}
}