using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpreadTrader
{
	public delegate void NodeSelectionDelegate(NodeViewModel node);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		public Decimal UKBalance { get; set; }
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
			props.ColumnWidth = Convert.ToInt32(OuterGrid.ColumnDefinitions[0].Width.Value);
			props.Save();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Settings":
						new Settings().ShowDialog(); 
						break;
					case "Refresh":
						TabItem tab = TabControl.Items[TabControl.SelectedIndex] as TabItem;
						tab.Content = new NodeViewModel(new BetfairAPI.BetfairAPI());
						break;
					case "Favourites":
						if (NodeViewModel.Betfair == null) break;
						new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()); break;
				}
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			EventsTree tree = new EventsTree();
			EventsTreeGrid.Content = tree;
			tree.NodeCallback += (node) =>
			{
				ClosableTab tab = TabControl.Items[TabControl.SelectedIndex] as ClosableTab;
				if (tab != null)
				{
					MarketControl mc = tab.Content as MarketControl;
					if (mc != null)
					{
						tab.Title = node.FullName;
						mc.NotificationSink(node);
					}
				}
			};
			tree.Populate();
		}
		private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			ClosableTab tab = new ClosableTab();
			tab.Content = new MarketControl();
			tab.Title = "Small title";
			TabControl.Items.Insert(0, tab);
			tab.Focus();
			e.Handled = true;
		}
	}
}
