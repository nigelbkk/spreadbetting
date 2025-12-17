using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContent : UserControl, INotifyPropertyChanged
    {
        public CustomTabHeader customHeader = null;
        public NodeViewModel MarketNode = null;
		public marketStatusEnum MarketStatus { get; set; }
		public Visibility OverlayVisibility { get; set; }
		public string MarketName { get { return MarketNode?.MarketName; } }
		//public Double? TotalMatched { get { return MarketNode?.TotalMatched; } }
		public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
            }
        }

		System.Timers.Timer market_status_timer = new System.Timers.Timer();

		public TabContent() { }
        public TabContent(CustomTabHeader header)
        {
            customHeader = header;
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;
            marketHeader.TabContent = this;
			OverlayVisibility = Visibility.Hidden;
			NotifyPropertyChanged("");

			//market_status_timer = new System.Timers.Timer(1000);
			//market_status_timer.Elapsed += (o, e) =>
			//{
			//	if (MarketNode != null && MarketStatus != MarketNode.Status)
			//	{
			//		if (MarketStatus != marketStatusEnum.CLOSED)
			//		{
			//			MarketStatus = MarketNode.Status;
			//			NotifyPropertyChanged("");
			//		}
			//	}
			//};
			//market_status_timer.Enabled = true;
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Selected")
			{
				dynamic d = data;
				NodeViewModel d2 = d.NodeViewModel as NodeViewModel;
				Debug.WriteLine($"TabContent: {d2.FullName}");
				if (MarketNode != null && d2.MarketID != MarketNode.MarketID)
				{
					Debug.WriteLine($"Not our market: {d2.FullName}");
					return;
				}
				MarketNode = d2;
				customHeader.Title = d2.FullName;
				customHeader.MarketId = d2.MarketID;
			}
			if (messageName == "Update Latency")
			{
				//dynamic d = data;
				//if (MarketStatus != (marketStatusEnum)d.Status)
				//{
				//	if (MarketStatus != marketStatusEnum.CLOSED)
				//	{
				//		MarketStatus = MarketNode.Status;
				//		NotifyPropertyChanged("");
				//	}
				//	MarketStatus = (marketStatusEnum)d.Status;
				//}
			}
			if (messageName == "Market Changed")
			{
			}
			if (messageName == "Orders Changed")
			{
				NotifyPropertyChanged("");
			}
		}
	}
}
