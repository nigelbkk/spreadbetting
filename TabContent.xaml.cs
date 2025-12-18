using Betfair.ESASwagger.Model;
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
		public MarketDefinition.StatusEnum? MarketStatus { get; set; }
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
			TabIndex = header.ID;
			BetsManager.TabID = header.ID;
			OverlayVisibility = Visibility.Hidden;
			NotifyPropertyChanged("");
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Changed")
			{
				dynamic d = data;
				MarketSnapDto snap = d.MarketSnapDto;
				if (RunnersControl != null && RunnersControl.MarketNode != null)
				{
					String name = RunnersControl?.MarketNode.Market.marketName;
					String marketId = RunnersControl?.MarketNode.Market.marketId;

					if (marketId == snap.MarketId)
					{
						OnMarketChanged(snap);
						//Debug.WriteLine($"Status = {name} : {snap.MarketId} : {snap.Status.ToString()}");
					}
				}
			}
		}
		public void OnSelected(NodeViewModel d2)
		{
			marketHeader.OnSelected(d2);
			customHeader.OnSelected(d2);
			BetsManager.OnSelected(d2, RunnersControl);
			_ = RunnersControl.PopulateNewMarketAsync(d2);
		}
		public void OnOrdersChanged(String json)
		{
			BetsManager.OnOrderChanged(json);
		}
		public void OnMarketChanged(MarketSnapDto snap)
		{
			RunnersControl?.OnMarketChanged(snap);
			customHeader.inPlay = snap.InPlay;

			if (MarketStatus != snap.Status)
			{
				MarketStatus = snap.Status;
				if (MarketStatus == MarketDefinition.StatusEnum.Open)
				{
					OverlayVisibility = Visibility.Hidden;
				}
				else
				{
					OverlayVisibility = Visibility.Visible;
				}
				NotifyPropertyChanged("");
			}
		}
	}
}
