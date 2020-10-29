using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
	public partial class MarketControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public NodeViewModel MarketNode { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		public MarketControl()
		{
			InitializeComponent();
			NodeChangeEventSink = RunnersControl.NodeChangeEventSink;
			NodeChangeEventSink += (node) =>
			{
				MarketNode = node;
				NotifyPropertyChanged("");
			};
		}
		private void LowerGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.LowerSplitter > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.LowerSplitter = props.UpperSplitter, GridUnitType.Pixel);
		}
		private void UpperGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.UpperSplitter > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.UpperSplitter  = props.UpperSplitter, GridUnitType.Pixel);
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
						if (LowerGrid.RowDefinitions[0].ActualHeight == 0)
							LowerGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star); 
						else
							LowerGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
						break;
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			GridSplitter gs = sender as GridSplitter;
			props.UpperSplitter = RunnersGrid.ActualHeight;
		}
		private void GridSplitter_DragCompleted_1(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			GridSplitter gs = sender as GridSplitter;
			props.LowerSplitter = BettingGrid.ActualHeight;
		}
		//		private void Button_Click(object sender, RoutedEventArgs e)
		//		{
		//			Button b = sender as Button;
		//			try
		//			{
		//				switch (b.Tag)
		//				{
		//					case "Market Description": new MarketDescription(this, b, MarketNode).ShowDialog(); break;
		//					case "Hide Grid":
		//						//Int32 w = Convert.ToInt32(RightGrid.RowDefinitions[2].Height.Value);

		////						double h0 = RightGrid.RowDefinitions[0].Height.Value;
		////						double h1 = RightGrid.RowDefinitions[1].Height.Value;
		////						double h2 = RightGrid.RowDefinitions[2].Height.Value;
		////						bool hidden = h1 == 0;

		//////						BettingGrid.Visibility = BettingGrid.Visibility == Visibility.Collapsed ? BettingGrid.Visibility = Visibility.Visible : BettingGrid.Visibility = Visibility.Collapsed;

		////						RightGrid.RowDefinitions[0].Height = new GridLength(hidden ? props.RowHeight1 : h0 + h1, GridUnitType.Pixel);
		////						RightGrid.RowDefinitions[1].Height = new GridLength(hidden ? props.RowHeight2 : 0, GridUnitType.Pixel);
		////						//UpArrow.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
		////						//DownArrow.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
		////						//HorizontalSplitter2.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
		//						break;
		//				}
		//			}
		//			catch (Exception xe)
		//			{
		//				Debug.WriteLine(xe.Message);
		//			}
		//		}
		//public void GridVisibility()
		//{
		//	Int32 w = Convert.ToInt32(BettingGrid.RowDefinitions[1].Height.Value);
		//	bool hidden = w == 0;
		//	BettingGrid.RowDefinitions[0].Height = new GridLength(hidden ? props.ColumnWidth : 0, GridUnitType.Pixel);
		//	BettingGrid.Visibility = BettingGrid.Visibility == Visibility.Collapsed ? BettingGrid.Visibility = Visibility.Visible : BettingGrid.Visibility = Visibility.Collapsed;
		//	UpArrow.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
		//	DownArrow.Visibility = hidden ? Visibility.Visible : Visibility.Collapsed;
		//	HorizontalSplitter2.Visibility = UpArrow.Visibility;
		//}
		//private void PopulateBetsGrid()
		//{
		//	AllBets = new ObservableCollection<Bet>();
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//}
	}
}
