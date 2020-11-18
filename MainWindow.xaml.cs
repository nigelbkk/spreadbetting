using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetfairAPI;

namespace SpreadTrader
{
	public delegate void NodeSelectionDelegate(NodeViewModel node);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public ICommand ExpandingCommand { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		private double _Balance = 0;
		private double _Exposure = 0;
		public double _Commission = 0;
		public String Balance { get { return String.Format("Balance: {0:C}", _Balance); } }
		public String Exposure { get { return String.Format("Exposure: {0:C}", _Exposure); } }
		public String Commission { get { return String.Format("Commission: {0:0.00}%", _Commission); } }
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
		EventsTree EventsTree = null;
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
			System.Net.ServicePointManager.Expect100Continue = false;
			InitializeComponent();
			SetWindowPosition();

			Betfair = new BetfairAPI.BetfairAPI();

			try
			{
				AccountFundsResponse response = Betfair.getAccountFunds(1);
				_Balance = response.availableToBetBalance;
				_Exposure = response.exposure;
				_Commission = response.retainedCommission;
				NotifyPropertyChanged("");
			}
			catch(Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void SetWindowPosition()
		{
			if (!props.Upgraded)
			{
				props.Upgrade();
				props.Upgraded = true;
				props.Save();
				Trace.WriteLine("INFO: Settings upgraded from previous version");
			}
			this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
			//this.Top = props.Top;
			//this.Left = props.Left;
			this.Height = props.Height;
			this.Width = props.Width;

			if (props.ColumnWidth > 0)
				OuterGrid.ColumnDefinitions[0].Width = new GridLength(props.ColumnWidth, GridUnitType.Pixel);

			if (props.Maximised)
			{
				WindowState = System.Windows.WindowState.Maximized;
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Hide Tree":
						{
							Int32 w = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
							bool hidden = w == 0;
							OuterGrid.ColumnDefinitions[0].Width = new GridLength(hidden ? props.ColumnWidth : 0, GridUnitType.Pixel);
							EventsTree.Visibility = EventsTree.Visibility == Visibility.Collapsed ? EventsTree.Visibility = Visibility.Visible : EventsTree.Visibility = Visibility.Collapsed;
							RightArrow.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
							LeftArrow.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
							VerticalSplitter.Visibility = LeftArrow.Visibility;
						}
						break;
					case "Settings":
						new Settings().ShowDialog();
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
				EventsTree = new EventsTree();
				EventsTreeGrid.Content = EventsTree;
				EventsTree.Populate();
				AppendNewTab();
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void AppendNewTab()
		{
			// hook the new tab up to receive tree control events 
			ClosableTab tab = new ClosableTab();
			MarketControl mc = new MarketControl();
			tab.Content = mc;
			EventsTree.NodeCallback += mc.NodeChangeEventSink;
			EventsTree.NodeCallback += tab.NodeChangeEventSink;

			tab.Title = "";
			TabControl.Items.Insert(0, tab);
			tab.Focus();
		}
		private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			AppendNewTab();
			e.Handled = true;
		}
		private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			props.ColumnWidth = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
		}
		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TabControl tabControl = sender as TabControl;
			if (e.RemovedItems.Count > 0)
			{
				TabItem tr = e.RemovedItems[0] as TabItem;
				MarketControl mcr = tr.Content as MarketControl;
				if (mcr != null)
					mcr.IsSelected = false;
			}
			if (e.AddedItems.Count > 0)
			{
				TabItem ta = e.AddedItems[0] as TabItem;
				MarketControl mca = ta.Content as MarketControl;
				if (mca != null)
					mca.IsSelected = true;
			}
		}
		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			props.Width = e.NewSize.Width;
			props.Height = e.NewSize.Height;
			props.Save();
		}
	}
}
