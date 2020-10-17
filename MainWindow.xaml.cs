using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class MainWindow : Window
	{
		private Properties.Settings props = Properties.Settings.Default;
		private String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged("Status"); } }
		public String UKBalance { get; set; }
		public ObservableCollection<EventType> AllEventTypes { get; set; }
		public ObservableCollection<Market> AllMarkets { get; set; }
		private BackgroundWorker bw = new BackgroundWorker();
		private BetfairAPI.BetfairAPI ng = null;
		public Market SelectedMarket { get; set; }
		public ObservableCollection<RunnerLive> LiveRunners { get; set; }
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
			bw.DoWork += RefreshSelectedMarket;

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
		}
		private void FetchEvents(object sender, RoutedEventArgs args)
		{
			
			AllEventTypes = new ObservableCollection<EventType>();
			AllMarkets = new ObservableCollection<Market>();
			LiveRunners = new ObservableCollection<RunnerLive>();
			try
			{
				Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						_busyIndicator.BusyContent = "Loading markets...";
						_busyIndicator.IsBusy = true;
						EventsTreeView.Items.Clear();
						ng.login(props.CertFile, props.CertPassword, props.AppKey, props.BFUser, props.BFPassword);
						List<EventTypeResult> eventTypes = ng.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
						Favourites f = new Favourites(ng.GetEventTypes().OrderBy(o => o.eventType.name).ToList());
						foreach (EventTypeResult ev in eventTypes)
						{
							if (!f.IsFavourite(ev.eventType.id))
								continue;

							var item = EventsTreeView.Items.Add(new TreeViewItem() { Header = ev.eventType.name } );
							TreeViewItem tvi = (TreeViewItem)EventsTreeView.Items[item];
							try
							{
								List<CompetitionResult> competitions = ng.GetCompetitions(ev.eventType.id);
								foreach (CompetitionResult cr in competitions)
								{
									Int32 item2 = tvi.Items.Add(new TreeViewItem() { Header = cr.competition.name });
									TreeViewItem tvi2 = (TreeViewItem)tvi.Items[item2];
									List<Event> events = ng.GetEvents(ev.eventType.id).OrderBy(o => o.details.name).ToList();
									foreach (Event e in events)
									{
										Int32 item3 = tvi2.Items.Add(new TreeViewItem() { Header = e.details.name });
										TreeViewItem tvi3 = (TreeViewItem)tvi2.Items[item3];
										List<Market> markets = ng.GetMarkets(e.details.id).OrderBy(o => o.marketName).ToList();
										foreach (Market m in markets)
										{
											tvi3.Items.Add(new TreeViewItem() { Header = String.Format("{0} {1}", m.details.openDate.ToString("HH:mm"), m.marketName)});
										}
									}
								}
							}
							catch (Exception xe)
							{
								Status = xe.Message;
//								MessageBox.Show(xe.Message);
							}
						}
						_busyIndicator.IsBusy = false;
					}
					catch (Exception xe)
					{
						Status = xe.Message;
						MessageBox.Show(xe.Message);
					}
				}));
			}
			catch (Exception xe)
			{
				MessageBox.Show(xe.Message);
			}
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
						MarketBook book = ng.GetMarketBook(SelectedMarket);
						foreach (Runner rr in book.Runners)
						{
							foreach (Market.RunnerCatalog cat in SelectedMarket.runners)
							{
								if (rr.selectionId == cat.selectionId)
								{
									rr.Catalog = cat;
								}
							}
							foreach (RunnerLive r in LiveRunners)
							{
								if (r.selectionId == rr.selectionId)
								{
									r.SetPrices(rr);
								}
							}
						}
						CalculateProfitAndLoss(SelectedMarket.marketId, LiveRunners.ToList());
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
		public void CalculateProfitAndLoss(String marketId, List<RunnerLive> runners)
		{
			List<MarketProfitAndLoss> pl = ng.listMarketProfitAndLoss(marketId);
			if (pl.Count > 0)
			{
				foreach (RunnerLive v in runners)
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
			ng = new BetfairAPI.BetfairAPI();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			switch (b.Tag)
			{
				case "Refresh":
					FetchEvents(null, null);
					break;
				case "Favourites":
					{
						Point coords = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(80, 24)));
						Favourites f = new Favourites(ng.GetEventTypes().OrderBy(o => o.eventType.name).ToList());
						f.Top = coords.Y;
						f.Left = coords.X;
						f.ShowDialog();
						f.Save();
					}
					break;
			}
		}
	}
}
