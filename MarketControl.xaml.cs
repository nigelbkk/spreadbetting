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
		public String MarketName { get { return MarketNode == null ? "No market selected" : MarketNode.Name;  } }
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
				if (IsLoaded)
				{
					MarketNode = node;
					BetsManagerControl.MarketNode = node;
					NotifyPropertyChanged("");
				}
			};
			SliderControl.OnSliderChanged += BettingGridControl.OnSliderChanged;
		}
		private void LowerGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.Splitter1 > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.Splitter1, GridUnitType.Pixel);
		}
		private void UpperGrid_Loaded(object sender, RoutedEventArgs e)
		{
			Grid grid = sender as Grid;
			if (props.Splitter2 > 0 && grid.RowDefinitions.Count > 0)
				grid.RowDefinitions[0].Height = new GridLength(props.Splitter2, GridUnitType.Pixel);
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
			props.Splitter2 = RunnersGrid.ActualHeight;
		}
		private void GridSplitter_DragCompleted_1(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			GridSplitter gs = sender as GridSplitter;
			props.Splitter1 = BettingGrid.ActualHeight;
		}
	}
}
