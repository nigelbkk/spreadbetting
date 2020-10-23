using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public delegate void NotificationDelegate(String s);
	public delegate void SelectedNodeDelegate(NodeViewModel node);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public NodeViewModel RootNode { get; set; }
		public NodeViewModel SelectedNode { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		public ObservableCollection<Bet> AllBets { get; set; }
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

			if (props.Maximised)
			{
				WindowState = System.Windows.WindowState.Maximized;
			}
			PopulateBetsGrid();
			NotifyPropertyChanged("");
		}
		private void PopulateBetsGrid()
		{
			AllBets = new ObservableCollection<Bet>();
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
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
		private void OnNotification(String cs)
		{
			Dispatcher.BeginInvoke(new Action(() => { Status = cs; NotifyPropertyChanged(""); }));
		}
		private void OnSelectionChanged(NodeViewModel node)
		{
			SelectedNode = node;
			Dispatcher.BeginInvoke(new Action(() => { NotifyPropertyChanged(""); }));
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged);
//			SelectedNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification);
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
						RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged);
						break;
					case "Favourites":
						{
							Point coords = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(80, 24)));
							Favourites f = new Favourites(NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList());
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
