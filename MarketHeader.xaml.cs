using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	public partial class MarketHeader : UserControl, INotifyPropertyChanged
	{
		public MarketSelectionDelegate OnMarketSelected;
		public TabContentControl TabContent { get; set; }
		public NodeViewModel MarketNode { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		public SolidColorBrush StreamingColor { get { return System.Windows.Media.Brushes.LightGreen; } }
//		public SolidColorBrush StreamingColor { get { return StreamActive ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightGray; } }
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public MarketHeader()
		{
			InitializeComponent();
			OnMarketSelected += (node) =>
			{
				MarketNode = node;
				NotifyPropertyChanged("");
			};
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
						if (TabContent.LowerGrid.RowDefinitions[0].ActualHeight == 0)
						{
							TabContent.LowerGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Auto);
							sp.Children[0].Visibility = Visibility.Visible;
							sp.Children[1].Visibility = Visibility.Collapsed;
						}
						else
						{
							TabContent.LowerGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
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
