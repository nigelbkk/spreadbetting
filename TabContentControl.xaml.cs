using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class TabContentControl : UserControl
	{
		public MarketSelectionDelegate OnMarketSelected;
		public TabContentControl()
		{
			InitializeComponent();
			BetsManager.RunnersControl = RunnersControl;
//			RunnersControl.betsManager = BetsManager;
			OnMarketSelected += (node) =>
			{
				RunnersControl.OnMarketSelected(node);
				MarketHeader.OnMarketSelected(node);
				BetsManager.OnMarketSelected(node);
			};
			MarketHeader.TabContent = this;
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
//					case "Market Description": new MarketDescription(this, b, MarketNode).ShowDialog(); break;
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
				Extensions.MainWindow.Status = xe.Message;
			}
		}
	}
}
