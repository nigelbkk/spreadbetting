using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetfairAPI;

namespace SpreadTrader
{
	public delegate void MarketSelectionDelegate(NodeViewModel node);
	public delegate void OnShutdownDelegate();
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public OnShutdownDelegate OnShutdown = null;
		public ICommand ExpandingCommand { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Debug.WriteLine(value); NotifyPropertyChanged("");} }
		public double Balance { get; set; }
		public double Exposure { get; set; }
		private double _DiscountRate { get; set; }
		public double DiscountRate { get { return _DiscountRate; } set { _DiscountRate = value; NotifyPropertyChanged("NetCommission"); } }
		private double _Commission { get; set; }
		public double Commission { get { return _Commission; } set { _Commission = value; NotifyPropertyChanged("Commission"); } }
		public double NetCommission { get { return _Commission - DiscountRate; } }
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		public MainWindow()
		{
			ServicePointManager.DefaultConnectionLimit = 800;
			System.Net.ServicePointManager.Expect100Continue = false;
			InitializeComponent();
			//SetWindowPosition();
			Betfair = new BetfairAPI.BetfairAPI();
			if (!props.UseProxy)
			{
				Betfair.login(props.CertFile, props.CertPassword, props.AppKey, props.BFUser, props.BFPassword);
			}
			UpdateAccountInformation();
		}
		public void UpdateAccountInformation()
		{
			try
			{
				AccountFundsResponse response = Betfair.getAccountFunds(1);
				Balance = response.availableToBetBalance;
				Exposure = response.exposure;
				//Commission = response.retainedCommission - response.discountRate;
				NotifyPropertyChanged("");
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		//private void SetWindowPosition()
		//{
		//	if (!props.Upgraded)
		//	{
		//		props.Upgrade();
		//		props.Upgraded = true;
		//		props.Save();
		//		Trace.WriteLine("INFO: Settings upgraded from previous version");
		//	}
		//	this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
		//	this.Height = props.Height;
		//	this.Width = props.Width;

		//	//if (props.ColumnWidth > 0)
		//	//	OuterGrid.ColumnDefinitions[0].Width = new GridLength(props.ColumnWidth, GridUnitType.Pixel);

		//	if (props.Maximised)
		//	{
		//		WindowState = System.Windows.WindowState.Maximized;
		//	}
		//	using (StreamWriter sw = File.CreateText(props.LogFile))
		//	{
		//		sw.WriteLine("Market Name, Market, Side, Runner, Stake, Odds, Time");
		//	}
		//}
		private void Button_Click(object sender, RoutedEventArgs e) // move to tab
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Hide Tree":
						{
							//Int32 w = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
							//bool hidden = w == 0;
							//OuterGrid.ColumnDefinitions[0].Width = new GridLength(hidden ? props.ColumnWidth : 0, GridUnitType.Pixel);
							//EventsTree.Visibility = EventsTree.Visibility == Visibility.Collapsed ? EventsTree.Visibility = Visibility.Visible : EventsTree.Visibility = Visibility.Collapsed;
							//RightArrow.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
							//LeftArrow.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
							//VerticalSplitter.Visibility = LeftArrow.Visibility;
						}
						break;
					case "Settings":
						new Settings(this, b).ShowDialog();
						break;
					case "Refresh":
						EventsTree.Refresh();
						break;
					case "Favourites":
						if (NodeViewModel.Betfair == null) break;
						new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()).ShowDialog(); break;
				}
				e.Handled = true;
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				AppendNewTab();
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void AppendNewTab()
		{
			ClosableTab tab = new ClosableTab();
			TabContentControl tc = tab.Content as TabContentControl;
			EventsTree.OnMarketSelected += tc.OnMarketSelected;
			EventsTree.OnMarketSelected += tab.OnMarketSelected;

			tab.Title = "New Market";

			TabControl.Items.Insert(0, tab);
			tab.Focus();
			NotifyPropertyChanged("");
		}
		//private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		//{
		//	props.ColumnWidth = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
		//	props.Save();
		//}
		//private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	TabControl tabControl = sender as TabControl;
		//	if (e.RemovedItems.Count > 0)
		//	{
		//		TabItem tr = e.RemovedItems[0] as TabItem;
		//		if (tr != null)
		//		{
		//			MarketControl mcr = tr.Content as MarketControl;
		//			if (mcr != null)
		//				mcr.IsSelected = false;
		//		}
		//	}
		//	if (e.AddedItems.Count > 0)
		//	{
		//		TabItem ta = e.AddedItems[0] as TabItem;
		//		if (ta != null)
		//		{
		//			MarketControl mca = ta.Content as MarketControl;
		//			if (mca != null)
		//				mca.IsSelected = true;
		//		}
		//	}
		//}
		//private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//	props.Width = e.NewSize.Width;
		//	props.Height = e.NewSize.Height;
		//	props.Save();
		//}
		private void StackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			UpdateAccountInformation();
			NotifyPropertyChanged("");
		}
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			props.Save();
			if (OnShutdown != null)
			{
				OnShutdown();
			};
		}
		//private void ClosableTab_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		//{
		//	AppendNewTab();
		//	e.Handled = true;
		//}
	}
}
