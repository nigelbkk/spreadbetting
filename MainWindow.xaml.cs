using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
	public delegate void NotificationDelegate(String s);
	public delegate void SelectedNodeDelegate(NodeViewModel node);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public NodeViewModel RootNode { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		public Decimal UKBalance { get; set; }
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

			//if (props.RowHeight1 > 0)
			//	RightGrid.RowDefinitions[0].Height = new GridLength(props.RowHeight1, GridUnitType.Pixel);

			//if (props.RowHeight2 > 0)
			//	RightGrid.RowDefinitions[1].Height = new GridLength(props.RowHeight2, GridUnitType.Pixel);

//			TabControl.Items[0].SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;

			if (props.Maximised)
			{
				WindowState = System.Windows.WindowState.Maximized;
			}
//			PopulateBetsGrid();
			NotifyPropertyChanged("");
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
			//props.RowHeight1 = Convert.ToInt32(RightGrid.RowDefinitions[0].Height.Value);
			//props.RowHeight2 = Convert.ToInt32(RightGrid.RowDefinitions[1].Height.Value);

			props.Save();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Market Description":
	//					new MarketDescription(this, b, SelectedNode).ShowDialog(); break;
					case "Settings":
						new Settings().ShowDialog(); break;
					case "Refresh":
//						RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged); break;
					case "Favourites":
						new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()); break;
						break;
				}
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void OnNotification(String cs)
		{
			Dispatcher.BeginInvoke(new Action(() => { Status = cs; NotifyPropertyChanged(""); }));
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			ClosableTab theTabItem = new ClosableTab();
			theTabItem.Title = "Small title";
			TabControl.Items.Add(theTabItem);
			theTabItem.Focus();
			e.Handled = true;
		}
	}
}
