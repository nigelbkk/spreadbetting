using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public EventsModel EventsModel { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged("Status"); } }
		public String UKBalance { get; set; }
		public ObservableCollection<EventType> AllEventTypes { get; set; }
		public ObservableCollection<Market> AllMarkets { get; set; }
		public ObservableCollection<Bet> AllBets { get; set; }
		private BackgroundWorker bw = new BackgroundWorker();
		private BetfairAPI.BetfairAPI Betfair = null;
		public Market SelectedMarket { get; set; }
		public ObservableCollection<LiveRunner> LiveRunners { get; set; }
		public ObservableCollection<Bet> LiveBets { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public MainWindow()
		{
			//bw.DoWork += RefreshSelectedMarket;

			System.Net.ServicePointManager.Expect100Continue = false;
			InitializeComponent();
			if (!props.Upgraded)
			{
				props.Upgrade();
				props.Upgraded = true;
				props.Save();
				Trace.WriteLine("INFO: Settings upgraded from previous version");
			}
			this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
			this.Top = props.Top;
			this.Left = props.Left;
			this.Height = props.Height;
			this.Width = props.Width;

			if (props.ColumnWidth > 0)
				OuterGrid.ColumnDefinitions[0].Width = new GridLength(props.ColumnWidth, GridUnitType.Pixel);

			if (props.Maximised)
			{
				WindowState = System.Windows.WindowState.Maximized;
			}
			PopulateBetsGrid();
			NotifyPropertyChanged("");
		}
		public void PopulateBetsGrid()
		{
			AllBets = new ObservableCollection<Bet>();
			AllEventTypes = new ObservableCollection<EventType>();
			AllMarkets = new ObservableCollection<Market>();
			LiveRunners = new ObservableCollection<LiveRunner>();

			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		}
		private void RefreshSelectedMarket(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			while (!bw.CancellationPending)
			{
				if (SelectedMarket != null)
				{
					try
					{
						MarketBook book = Betfair.GetMarketBook(SelectedMarket);
						foreach (Runner rr in book.Runners)
						{
							foreach (Market.RunnerCatalog cat in SelectedMarket.runners)
							{
								if (rr.selectionId == cat.selectionId)
								{
									rr.Catalog = cat;
								}
							}
							foreach (LiveRunner r in LiveRunners)
							{
								if (r.selectionId == rr.selectionId)
								{
									r.SetPrices(rr);
								}
							}
						}
//						CalculateProfitAndLoss(SelectedMarket.marketId, LiveRunners.ToList());
						System.Threading.Thread.Sleep(props.WaitBF);
					}
					catch (Exception xe)
					{
						Status = xe.Message.ToString();
						System.Threading.Thread.Sleep(props.WaitBF);
					}
				}
				System.Threading.Thread.Sleep(props.WaitBF);
			}
		}
		public void CalculateProfitAndLoss(String marketId, List<LiveRunner> runners)
		{
			List<MarketProfitAndLoss> pl = Betfair.listMarketProfitAndLoss(marketId);
			if (pl.Count > 0)
			{
				foreach (LiveRunner v in runners)
				{
					foreach (var o in pl[0].profitAndLosses)
					{
						if (v.ngrunner.selectionId == o.selectionId)
						{
							v._prices[0][6].size = o.ifWin;
						}
					}
				}
			}
		}
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (WindowState == System.Windows.WindowState.Maximized)
			{
				// Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
				props.Top = RestoreBounds.Top;
				props.Left = RestoreBounds.Left;
				props.Height = RestoreBounds.Height;
				props.Width = RestoreBounds.Width;
				props.Maximised = true;
			}
			else
			{
				props.Top = this.Top;
				props.Left = this.Left;
				props.Height = this.Height;
				props.Width = this.Width;
				props.Maximised = false;
			}
			props.ColumnWidth = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
			props.RowHeight1 = Convert.ToInt32(RightGrid.RowDefinitions[0].Height.Value);
			props.RowHeight2 = Convert.ToInt32(RightGrid.RowDefinitions[1].Height.Value);

			props.Save();
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Betfair = new BetfairAPI.BetfairAPI();
			EventsModel = new EventsModel(Betfair);
			NotifyPropertyChanged("");
		}
		private void SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			NodeViewModel selectedItem = (NodeViewModel)EventsTreeView.SelectedItem;
			SelectedMarket = selectedItem.Tag as Market;
			if (SelectedMarket != null)
			{
				using (new WaitCursor())
				{
					try
					{
						Market m = SelectedMarket;
						LiveRunners.Clear();
						MarketBook book = Betfair.GetMarketBook(m);
						foreach (Runner r in book.Runners)
						{
							if (r.removalDate == new DateTime())
							{
								LiveRunner rl = new LiveRunner(r);
								LiveRunners.Add(rl);
								if (r.ex.availableToBack.Count > 0)
								{
								}
							}
						}
						NotifyPropertyChanged("");
						Status = "Ready";
					}
					catch (Exception xe) { Status = "Markets_SelectionChanged: " + String.Format(xe.Message.ToString()); }
				}
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Settings":
						Settings sd = new Settings();
						if (sd.ShowDialog() == true)
						{
							NotifyPropertyChanged("");
						}
						break;
					case "Refresh":
						EventsModel = new EventsModel(Betfair);
						NotifyPropertyChanged("");
						break;
					case "Favourites":
						{
							Point coords = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(80, 24)));
							Favourites f = new Favourites(Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList());
							f.Top = coords.Y;
							f.Left = coords.X;
							f.ShowDialog();
							f.Save();
						}
						break;
				}
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
	}
}
