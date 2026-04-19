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
		private String marketID;

		public String MarketStatus => Status.ToString();
		private marketStatusEnum _Status;
		public marketStatusEnum Status
		{
			get => _Status;

			set
			{
				if (_Status != value)
				{
					Debug.WriteLine(value);
					_Status = value;
					OnPropertyChanged(nameof(Status));
					OnPropertyChanged(nameof(MarketStatus));
					OnPropertyChanged(nameof(OverlayVisibility));
				}
			}
		}
		public Visibility OverlayVisibility
		{
			get => (Status == marketStatusEnum.OPEN || Status == marketStatusEnum.INACTIVE) ? Visibility.Hidden : Visibility.Visible; 
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
			if (messageName == "Market Status Changed")
			{
				//dynamic d = data as NodeViewModel;
				Status = MarketNode.Status;
			}
			if (messageName == "Telemetry Available")
			{
				dynamic d = data as MarketTelemetry;
				String marketid = d.MarketId;
				double totalMatched = d?.TotalMatched;

				if (marketid == this.marketID)
					marketHeader.TotalMatched = totalMatched;

				Status = MarketNode.Status;
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
			Status = marketStatusEnum.INACTIVE;
//			MarketStatus = marketStatusEnum.INACTIVE.ToString();

			OnPropertyChanged(nameof(MarketName));
		}
		public void OnTabRemoved()
		{
			RunnersControl?.OnMarketClosed();
		}
		public void OnTabSelected()
		{
			OnPropertyChanged(nameof(OverlayVisibility));
			OnPropertyChanged(nameof(MarketStatus));

			marketHeader?.OnTabSelected();
		}
	}
}
