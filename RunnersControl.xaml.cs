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
	public delegate void StreamUpdateDelegate(String marketid, List<LiveRunner> liveRunners, double tradedVolume, bool inplay);

	public partial class RunnersControl : UserControl, INotifyPropertyChanged
	{
		public BetsManager betsManager = null;
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public StreamUpdateDelegate StreamUpdateEventSink = null;
		public FavoriteChangedDelegate OnFavoriteChanged = null;
		private StreamingAPI streamingAPI = new StreamingAPI();
		private BackgroundWorker Worker = null;
		public NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode;  } set { _MarketNode = value; LiveRunners = new List<LiveRunner>(); NotifyPropertyChanged(""); } }
		public bool IsSelected { get; set; }
		public double BackBook { get; set; }
		public double LayBook { get; set; }
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
				Overlay.Visibility = MarketNode.Status == marketStatusEnum.OPEN ? Visibility.Hidden : Visibility.Visible;
				OverlayText.Text = MarketNode.Status.ToString();
			}));
		}
		public RunnersControl()
		{
			MarketNode = new NodeViewModel(MainWindow.Betfair);
			LiveRunners = new List<LiveRunner>();
			InitializeComponent();

			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				if (marketid == MarketNode.MarketID)
				{
					List<MarketProfitAndLoss> pl = MainWindow.Betfair.listMarketProfitAndLoss(MarketNode.MarketID);

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
						LiveRunners[i].NotifyPropertyChanged("");
						LiveRunners[i].Width = ItemsGrid.ColumnDefinitions[0].ActualWidth;
						foreach (var p in pl[0].profitAndLosses)
						{
							if (p.selectionId == LiveRunners[i].SelectionId)
							{
								LiveRunners[i].ifWin = p.ifWin;
							}
						}
					}
					MarketNode.LiveRunners = LiveRunners;
					MarketNode.CalculateLevelProfit();
					MarketNode.TotalMatched = tradedVolume;
					BackBook = totalBack;
					LayBook = totalLay;
					UpdateMarketStatus();
					NotifyPropertyChanged("");
					if (Worker.IsBusy)
						Worker.CancelAsync();
				}
			};
			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					try
					{
						LiveRunners = MarketNode.GetLiveRunners();
						Debug.WriteLine(MarketNode.Status);
						if (props.UseStreaming)
						{
							BackgroundWorker bw = new BackgroundWorker();
							bw.DoWork += (o, e) =>
							{
								streamingAPI.Start(MarketNode.MarketID);
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
					for (int i = 0; i < LiveRunners.Count; i++)
					{
						if (NewRunners[i].ngrunner != null)
						{
							LiveRunners[i].SetPrices(NewRunners[i].ngrunner);
							LiveRunners[i].LevelProfit = NewRunners[i].LevelProfit;
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
							UpdateMarketStatus();
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
		}
		private PlaceExecutionReport placeOrders(String marketId, List<PlaceInstruction> pis)
		{
			BetfairAPI.BetfairAPI Betfair = MainWindow.Betfair;
			PlaceExecutionReport report = Betfair.placeOrders(marketId, pis);
			return report; 
		}
		public void SubmitBets(PriceSize[] LayValues, PriceSize[] BackValues)
		{
			if (MarketNode != null)
			{
				DateTime LastUpdate = DateTime.UtcNow;
				BetfairAPI.BetfairAPI betfairAPI = MainWindow.Betfair;
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
				List<PlaceInstruction> orders = new List<PlaceInstruction>();
				for (int j = 0; j < 2; j++)
				{
					List<PriceSize> bets = j == 0 ? laybets : backbets;
					PriceSize[] values = j == 0 ? LayValues : BackValues;
					sideEnum side = j == 0 ? sideEnum.LAY : sideEnum.BACK;
					for (Int32 i = 0; i < 9; i++)
					{
						if (!bets[i].IsChecked || !bets[i].ParentChecked)
							continue;

						PlaceInstruction pi = new PlaceInstruction()
						{
							orderTypeEnum = orderTypeEnum.LIMIT,
							sideEnum = side,
							Runner = LiveRunner.Favorite.Name,
							marketTypeEnum = marketTypeEnum.WIN,
							selectionId = LiveRunner.Favorite.SelectionId,
							limitOrder = new LimitOrder()
							{
								persistenceTypeEnum = persistenceTypeEnum.LAPSE,
								price = values[i].price,
								size = values[i].size,
							}
						};
						orders.Add(pi);
					}
				}
				//for (Int32 i = 0; i < 9; i++)
				//{
				//	if (backbets[i].IsChecked && backbets[i].ParentChecked)
				//	{
				//		PlaceInstruction pi = new PlaceInstruction()
				//		{
				//			orderTypeEnum = orderTypeEnum.LIMIT,
				//			sideEnum = sideEnum.BACK,
				//			Runner = LiveRunner.Favorite.Name,
				//			marketTypeEnum = marketTypeEnum.WIN,
				//			selectionId = LiveRunner.Favorite.SelectionId,
				//			limitOrder = new LimitOrder()
				//			{
				//				persistenceTypeEnum = persistenceTypeEnum.LAPSE,
				//				price = BackValues[i].price,
				//				size = BackValues[i].size,
				//			}
				//		};
				//		backorders.Add(pi);
				//		if (props.SafeBets)
				//			break;
				//	}
				//}
				if (props.SafeBets)
				{
					foreach(PlaceInstruction pi in orders)
					{
						pi.limitOrder.size = 2.00;
						pi.limitOrder.price = pi.sideEnum == sideEnum.LAY ? 1.01 : 1000;
					}
				}
				placeOrders(MarketNode.Market.marketId, orders);
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
		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SV1.Height = Math.Max(25, e.NewSize.Height - Header.Height);
//			Debug.WriteLine("RunnersControl.Grid_SizeChanged");
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
				if (OnFavoriteChanged != null)
				{
					OnFavoriteChanged(LiveRunner.Favorite);
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			Extensions.MainWindow.OnShutdown += () =>
			{
				streamingAPI.Stop();
			};
		}
		private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			double diff = ItemsGrid.ColumnDefinitions[0].Width.Value - SV1.ActualWidth;
			foreach (LiveRunner v in SV1.Items)
			{
				v.Width = ItemsGrid.ColumnDefinitions[0].Width.Value;
			}
		}
	}
}