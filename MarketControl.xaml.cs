using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	public partial class MarketControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public NodeViewModel _MarketNode { get; set; }
		public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
		public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; NotifyPropertyChanged(""); } }
		private bool _StreamActive { get; set; }
		public bool StreamActive { get { return _StreamActive; } set { _StreamActive = value; NotifyPropertyChanged(""); } }
		public bool IsSelected { set 
			{
				RunnersControl.IsSelected = value; 
			} 
		}
		private Timer timer = new Timer();
		private Timer ttg_timer = new Timer();
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public MarketControl()
		{
			InitializeComponent();
			BetsManagerControl.RunnersControl = RunnersControl;
			RunnersControl.betsManager = BetsManagerControl;
			NodeChangeEventSink += RunnersControl.NodeChangeEventSink;
			NodeChangeEventSink += BetsManagerControl.NodeChangeEventSink;
			NodeChangeEventSink += (node) =>
			{
				// new market selected from the tree control
				if (IsLoaded)
				{
					MarketNode = node;
					BettingGridControl.MarketNode = node;
					RunnersControl.MarketNode = MarketNode;
					LiveRunner.Favorite = null;
				}
			};
			timer.Elapsed += (o, e) =>
			{
				StreamActive = false;
				timer.Stop();
			};
			timer.Interval = 2000;
			timer.Enabled = true;
			timer.AutoReset = true;
			timer.Start();
			ttg_timer.Elapsed += (o, e) =>
			{
				NotifyPropertyChanged("");
			};
			ttg_timer.Interval = 1000;
			ttg_timer.Enabled = true;
			ttg_timer.Start();
			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				StreamActive = true;
				timer.Start();

				this.Dispatcher.Invoke(() =>
				{
					NotifyPropertyChanged("TimeToGo");
				});
			};
			SliderControl.SubmitBets += RunnersControl.SubmitBets;
			RunnersAndSlidersGrid.ColumnDefinitions[0].Width = new GridLength(props.VerticalSplitter);
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Market Description": new MarketDescription(this, b, MarketNode).ShowDialog(); break;
					case "Hide Grid":
						StackPanel sp = b.Content as StackPanel;
						if (LowerGrid.RowDefinitions[0].ActualHeight == 0)
						{
							LowerGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Auto);
							sp.Children[0].Visibility = Visibility.Visible;
							sp.Children[1].Visibility = Visibility.Collapsed;
						}
						else
						{
							LowerGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
							sp.Children[0].Visibility = Visibility.Collapsed;
							sp.Children[1].Visibility = Visibility.Visible;
						}
						break;
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
				MainWindow mw = Extensions.FindParentOfType<MainWindow>(Parent);
				if (mw != null) mw.Status = xe.Message;
			}
		}
		private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			GridSplitter gs = sender as GridSplitter;
			props.HorizontalSplitter = RunnersGrid.ActualHeight;
			props.Save();
		}
		private void UpperGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.HorizontalSplitter > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.HorizontalSplitter, GridUnitType.Pixel);
			if (props.VerticalSplitter2 > 0)// && grid.RowDefinitions.Count > 0)
				RunnersAndSlidersGrid.ColumnDefinitions[0].Width = new GridLength(props.VerticalSplitter2, GridUnitType.Pixel);
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			BetsManagerControl.RunnersControl = RunnersControl;
		}
		private void GridSplitter_DragCompleted_1(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			props.VerticalSplitter2 = RunnersAndSlidersGrid.ColumnDefinitions[0].Width.Value;
			props.Save();
		}
		private void LowerGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
		}
	}
}
