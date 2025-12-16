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
		private NodeViewModel MarketNode { get; set; }
		public TabContent TabContent { get; set; }
		public String ConnectButtonText { get; set; }
		public String FullName { get { return MarketNode == null ? "No Market Selected" : MarketNode.MarketName; } }
		public String TimeToGo { get { return MarketNode == null ? "00:00:00" : MarketNode.TimeToGo; } }
		public String Lag { get; set; }
		private System.Timers.Timer timer = new System.Timers.Timer();
		public Double? TotalMatched { get { return MarketNode == null ? 0 : MarketNode.TotalMatched; } }
		public Visibility up_visible { get; set; }
		public SolidColorBrush TimeToGoColor { get { return System.Windows.Media.Brushes.DarkGray; } }
		public Visibility down_visible { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		public SolidColorBrush StreamingColor { get { return System.Windows.Media.Brushes.LightGreen; } }
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Selected")
			{
				dynamic d = data;
				NodeViewModel d2 = d.NodeViewModel;
				Debug.WriteLine($"MarketHeader {d2.MarketName}");
				MarketNode = d2;
			}
			if (messageName == "Update Lag")
			{
				dynamic d = data;
				//Debug.WriteLine($"MarketHeader {d.Lag}");
				Lag = d.Lag;
			}
		}
		public MarketHeader()
		{
			InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;

			up_visible = Visibility.Visible;
			down_visible = Visibility.Collapsed;
			ConnectButtonText = "Reconnect";
			NotifyPropertyChanged("");
			timer.Elapsed += (o, e) =>
			{
				NotifyPropertyChanged("");
			};
			timer.Interval = 1000;
			timer.Enabled = true;
			timer.Start();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Connect":
						ControlMessenger.Send("Reconnect requested");
						break;
					case "Market Description":
						new MarketDescription(this, b, TabContent.MarketNode).ShowDialog();
						break;
					case "Hide Grid":
						down_visible = TabContent.BettingGrid.Visibility;
						up_visible = TabContent.BettingGrid.Visibility = TabContent.BettingGrid.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
						NotifyPropertyChanged("");
						break;
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
				Extensions.MainWindow.Status = xe.Message;
			}
		}
		private double previous_width = 200;
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			double present_width = Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width.Value;

			if (present_width > 10)
				previous_width = Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width.Value;

			double new_width = present_width >= 2 ? 0 : previous_width;
			Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width = new System.Windows.GridLength(new_width);
		}
	}
}
