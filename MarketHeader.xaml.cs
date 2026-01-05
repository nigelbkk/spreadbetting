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
		
		private String _MarketLatency { get; set; }
		private Visibility _down_visible;
		private Visibility _up_visible;

		public String MarketLatency { get => _MarketLatency;
			set {
				if (_MarketLatency != value)
				{
					_MarketLatency = value;
					OnPropertyChanged(nameof(MarketLatency));
				} } }
		public String OrdersLatency { get; set; }

		public Double? _TotalMatched = 0;
		public Double? TotalMatched
		{
			get => _TotalMatched;
			set { if (_TotalMatched != value){
					_TotalMatched = value;
					OnPropertyChanged(nameof(TotalMatched));
				}
			}
		}
		
		public Visibility down_visible { get => _down_visible; set { _down_visible = value; OnPropertyChanged(nameof(down_visible)); } }
		public Visibility up_visible { get => _up_visible; set { _up_visible = value; OnPropertyChanged(nameof(up_visible)); } }
		public SolidColorBrush TimeToGoColor { get { return System.Windows.Media.Brushes.DarkGray; } }

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		private System.Timers.Timer timer = new System.Timers.Timer();

		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Update Market Latency")
			{
				dynamic d = data;
				MarketLatency = d?.MarketLatency;
			}
            if (messageName == "Update Orders Latency")
            {
                dynamic d = data;
                OrdersLatency = d.OrdersLatency;
            }
        }
        public MarketHeader()
		{
			InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;

			up_visible = Visibility.Visible;
			down_visible = Visibility.Collapsed;
			ConnectButtonText = "Reconnect";
			timer.Elapsed += (o, e) =>
			{
				OnPropertyChanged(nameof(TimeToGo));
				OnPropertyChanged(nameof(TotalMatched));
			};
			timer.Interval = 1000;
			timer.Enabled = true;
			timer.Start();
		}
		public void OnMarketSelected(NodeViewModel d2)
		{
			MarketNode = d2;
		}
		public void OnTabSelected()
		{
			OnPropertyChanged(nameof(TotalMatched));
			OnPropertyChanged(nameof(FullName));
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Connect":
						ControlMessenger.Send("Reconnect", new { MarketId = MarketNode.MarketID});
						break;
					case "Market Description":
						new MarketDescription(this, b, TabContent.MarketNode).ShowDialog();
						break;
					case "Hide Grid":
						down_visible = TabContent.BettingGrid.Visibility;
						up_visible = TabContent.BettingGrid.Visibility = TabContent.BettingGrid.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
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
