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
		public NodeViewModel SelectedNode { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		private static String _Status = "Ready";
		public String Status { get { return _Status; } set { _Status = value; Trace.WriteLine(value); NotifyPropertyChanged(""); } }
		public Decimal UKBalance { get; set; }
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

			if (props.RowHeight1 > 0)
				RightGrid.RowDefinitions[0].Height = new GridLength(props.RowHeight1, GridUnitType.Pixel);

			if (props.RowHeight2 > 0)
				RightGrid.RowDefinitions[1].Height = new GridLength(props.RowHeight2, GridUnitType.Pixel);

			SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;

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
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Market Description":
						new MarketDescription(this, b, SelectedNode).ShowDialog(); break;
					case "Settings":
						new Settings().ShowDialog(); break;
					case "Refresh":
						RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged);	break;
					case "Favourites":
						new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()); break;
				}
			}
			catch (Exception xe)
			{
				Status = xe.Message.ToString();
			}
		}
		private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;
		}
		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			if (e.Key == Key.Return || e.Key == Key.Escape)
			{
				Grid grid = textbox.Parent as Grid;
				Label label = grid.Children[0] as Label;
				label.Visibility = Visibility.Visible;
				textbox.Visibility = Visibility.Hidden;
				if (textbox.Text.All(char.IsNumber))
				{
					props.BackStake = Convert.ToDecimal(textbox.Text);
					props.Save();
				}
			}
		}
		private void label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Label label = sender as Label;
			Grid grid = label.Parent as Grid;
			TextBox textbox = grid.Children[1] as TextBox;
			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				textbox.Focus();
				Keyboard.Focus(textbox);
			}));
			label.Visibility = Visibility.Hidden;
			textbox.Visibility = Visibility.Visible;
			NotifyPropertyChanged("");
		}

		private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			Grid grid = textbox.Parent as Grid;
			Label label = grid.Children[0] as Label;
			label.Visibility = Visibility.Visible;
			textbox.Visibility = Visibility.Hidden;
			NotifyPropertyChanged("");
		}
	}
}
