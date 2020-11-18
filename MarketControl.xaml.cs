using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class MarketControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode; } set { _MarketNode = value; NotifyPropertyChanged(""); } }
		public bool IsSelected { set 
			{
				RunnersControl.IsSelected = value; 
			} 
		}
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
			NodeChangeEventSink += RunnersControl.NodeChangeEventSink;
			NodeChangeEventSink += BetsManagerControl.NodeChangeEventSink;
			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					BettingGridControl.MarketNode = node;
					RunnersControl.MarketNode = MarketNode;
				}
			};
			SliderControl.SubmitBets += BettingGridControl.SubmitBets;
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
			}
		}
		private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			GridSplitter gs = sender as GridSplitter;
			props.HorizontalSplitter = RunnersGrid.ActualHeight;
			props.Save();
		}
		private void GridSplitter_DragCompleted_1(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			props.VerticalSplitter = RunnersAndSlidersGrid.ColumnDefinitions[0].Width.Value;
			props.Save();
		}
		private void UpperGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.HorizontalSplitter > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.HorizontalSplitter, GridUnitType.Pixel);
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			BetsManagerControl.RunnersControl = RunnersControl;
		}
	}
}
