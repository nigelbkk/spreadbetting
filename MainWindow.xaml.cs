using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using APING;

namespace SpreadTrader
{
	public partial class MainWindow : Window
	{
		private String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged("Status"); } }
		public String UKBalance { get; set; }
		public ObservableCollection<APING.Market> markets { get; set; }
		private BackgroundWorker bw = new BackgroundWorker();
		private APING.APING ng = null;
		public APING.Market SelectedMarket { get; set; }
		public ObservableCollection<RunnerLive> LiveRunners { get; set; }
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
			bw.DoWork += bw_DoWork;

			markets = new ObservableCollection<APING.Market>();
			LiveRunners = new ObservableCollection<RunnerLive>();
			System.Net.ServicePointManager.Expect100Continue = false;
			InitializeComponent();
			if (!Properties.Settings.Default.Upgraded)
			{
				Properties.Settings.Default.Upgrade();
				Properties.Settings.Default.Upgraded = true;
				Properties.Settings.Default.Save();
				Trace.WriteLine("INFO: Settings upgraded from previous version");
			}
			this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
			this.Top = Properties.Settings.Default.Top;
			this.Left = Properties.Settings.Default.Left;
			this.Height = Properties.Settings.Default.Height;
			this.Width = Properties.Settings.Default.Width;

			if (Properties.Settings.Default.ColumnWidth > 0)
				grid.ColumnDefinitions[0].Width = new GridLength(Properties.Settings.Default.ColumnWidth, GridUnitType.Pixel);

			if (Properties.Settings.Default.Maximised)
			{
				WindowState = System.Windows.WindowState.Maximized;
			}
		}
		private void bw_DoWork(object sender, DoWorkEventArgs e)
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
						System.Threading.Thread.Sleep(Properties.Settings.Default.WaitBF);
					}
					catch (Exception xe)
					{
						Status = xe.Message.ToString();
						System.Threading.Thread.Sleep(Properties.Settings.Default.WaitBF);
					}
				}
				System.Threading.Thread.Sleep(Properties.Settings.Default.WaitBF);
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
			var p = Properties.Settings.Default;
			if (WindowState == System.Windows.WindowState.Maximized)
			{
				// Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
				p.Top = RestoreBounds.Top;
				p.Left = RestoreBounds.Left;
				p.Height = RestoreBounds.Height;
				p.Width = RestoreBounds.Width;
				p.Maximised = true;
			}
			else
			{
				p.Top = this.Top;
				p.Left = this.Left;
				p.Height = this.Height;
				p.Width = this.Width;
				p.Maximised = false;
			}
			p.ColumnWidth = Convert.ToInt32(grid.ColumnDefinitions[0].Width.Value);
			p.RowHeight1 = Convert.ToInt32(RightGrid.RowDefinitions[0].Height.Value);
			p.RowHeight2 = Convert.ToInt32(RightGrid.RowDefinitions[1].Height.Value);

			p.Save();
		}
	}
}
