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
		public MarketSelectionDelegate OnMarketSelected;
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
			OnMarketSelected += RunnersControl.OnMarketSelected;
			OnMarketSelected += BetsManagerControl.OnMarketSelected;
			OnMarketSelected += (node) =>
			{
				// new market selected from the tree control
				if (IsLoaded)
				{
					MarketNode = node;
					BettingGridControl.MarketNode = node;
					RunnersControl.MarketNode = MarketNode;
					Extensions.MainWindow.Commission = MarketNode.Commission;
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
				StreamActive = true;
				timer.Start();
				NotifyPropertyChanged("");
			};
			ttg_timer.Interval = 500;
			ttg_timer.Enabled = true;
			ttg_timer.Start();
			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				this.Dispatcher.Invoke(() =>
				{
					NotifyPropertyChanged("TimeToGo");
				});
			};
			SliderControl.SubmitBets += RunnersControl.SubmitBets;
//			RunnersAndSlidersGrid.ColumnDefinitions[0].Width = new GridLength(props.VerticalSplitter);
			RunnersControl.OnFavoriteChanged += SliderControl.OnFavoriteChanged;
		}
		//private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		//{
		//	GridSplitter gs = sender as GridSplitter;
		//	props.HorizontalSplitter = RunnersGrid.ActualHeight;
		//	BetsManagerControl.Height = Math.Max(25, Extensions.MainWindow.ActualHeight - RunnersGrid.ActualHeight - 255);
		//	props.Save();
		//}

		//private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		//{
		//	props.Save();
		//}
	}
}
