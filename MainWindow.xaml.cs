using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
	public delegate void NodeSelectionDelegate(NodeViewModel node);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		private Decimal _Balance = 2458.51M;
		private Decimal _Exposure = -1;
		public Decimal _Commission = 8;
		public String Balance { get { return String.Format("Balance: {0:C}", _Balance); } }
		public String Exposure { get { return String.Format("Exposure: {0:C}", _Exposure); } }
		public String Commission { get { return String.Format("Commission: {0:0.00}%", _Commission); } }
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
			NotifyPropertyChanged("");
		}
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (WindowState == System.Windows.WindowState.Maximized)
			{
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
			props.Save();
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
	}
}
