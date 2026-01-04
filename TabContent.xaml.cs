using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContent : UserControl, INotifyPropertyChanged
    {
        public CustomTabHeader customHeader = null;
        public NodeViewModel MarketNode = null;
        private System.Timers.Timer timer = new System.Timers.Timer();
		private String marketID;

		private String _MarketStatus;
        public String MarketStatus { get => _MarketStatus;
			
			set {
				if (_MarketStatus.ToUpper() != value)
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
			get => (MarketStatus == "OPEN" && MarketStatus == "INACTIVE") ? Visibility.Hidden : Visibility.Visible; 
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
			_ = MarketStatusLoopAsync();
        }
        private async void UnsubscribeAsync(String marketId)
		{
			ControlMessenger.Send("Unsubscribe", new { MarketId = marketId });
		}

		private async Task MarketStatusLoopAsync()
		{
			while (true)
			{
				try
				{
					marketStatusEnum status = await betfair.GetMarketStatusAsync(marketID);
					MarketStatus = status.ToString();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
				await Task.Delay(TimeSpan.FromSeconds(1));
			}
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
		public void OnSelected(NodeViewModel d2)
		{
			marketID = d2.MarketID;
			marketHeader.OnSelected(d2);
			customHeader.OnSelected(d2);
			BetsManager.OnSelected(d2, RunnersControl);
			RunnersControl.PopulateNewMarket(d2);
            OnPropertyChanged(nameof(OverlayVisibility));
            OnPropertyChanged(nameof(MarketStatus));
        }
		public void OnOrdersChanged(String json)
		{
			BetsManager.OnOrderChanged(json);
		}
		void OnMarketChanged(MarketChangeDto change)
		{
			RunnersControl?.OnMarketChanged(change);
		}
	}
}
