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
        //private System.Timers.Timer timer = new System.Timers.Timer();
		private String marketID;

		private String _MarketStatus = "";
        public String MarketStatus
		{
			get => _MarketStatus;

			set
			{
				if (_MarketStatus != value)
				{
					Debug.WriteLine(value);
					_MarketStatus = value;
					OnPropertyChanged(nameof(MarketStatus));
					OnPropertyChanged(nameof(OverlayVisibility));
				}
			}
		}
		public Visibility OverlayVisibility
		{
			get => (MarketStatus.ToUpper() == "OPEN" || MarketStatus == "INACTIVE") ? Visibility.Hidden : Visibility.Visible; 
		}
		public string MarketName { get { return MarketNode?.MarketName; } }
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		private BetfairAPI.BetfairAPI betfair;
		public TabContent() { }
        public TabContent(CustomTabHeader header)
        {
			betfair = MainWindow.Betfair;

			customHeader = header;
            InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;
			marketHeader.TabContent = this;
			oBettingGrid.sliderControl = SliderControl;
        }
        private async void UnsubscribeAsync(String marketId)
		{
			ControlMessenger.Send("Unsubscribe", new { MarketId = marketId });
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Changed")
			{
				dynamic d = data;
				MarketChangeDto change = d.MarketChangeDto;
				if (RunnersControl != null && RunnersControl.MarketNode != null)
				{
					String name = RunnersControl?.MarketNode.Market.marketName;
                    
					if (RunnersControl.MarketNode.MarketID == change.MarketId)
					{
						OnMarketChanged(change);
					}
				}
			}
		}
		public void OnMarketSelected(NodeViewModel d2)
		{
			MarketNode = d2;
			marketID = d2.MarketID;
			marketHeader.OnMarketSelected(d2);
			customHeader.OnMarketSelected(d2);
			BetsManager.OnMarketSelected(d2, RunnersControl);
			RunnersControl.PopulateNewMarket(d2);
			OnPropertyChanged(nameof(OverlayVisibility));
			OnPropertyChanged(nameof(MarketStatus));
		}
		public void OnTabSelected()
		{
			OnPropertyChanged(nameof(OverlayVisibility));
			OnPropertyChanged(nameof(MarketStatus));

			marketHeader?.OnTabSelected();
		}
		public void OnOrdersChanged(String json)
		{
			BetsManager.OnOrderChanged(json);
		}
		void OnMarketChanged(MarketChangeDto change)
		{
			MarketStatus = change.Status.ToString();
			marketHeader.TotalMatched = change.Tv ?? 0;
			RunnersControl?.OnMarketChanged(change);
		}
	}
}
