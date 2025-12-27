using Betfair.ESAClient.Cache;
using Betfair.ESASwagger.Model;
using BetfairAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
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

        private String _MarketStatus;
        public String MarketStatus { get => _MarketStatus;
			
			set {
				if (_MarketStatus != "Closed" && _MarketStatus != value)
				{
					_MarketStatus = value;
                    Debug.WriteLine($"{_MarketStatus} : {DateTime.UtcNow.ToLongTimeString()}");
                    OnPropertyChanged(nameof(MarketStatus));
                    OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
		}

        private Visibility _OverlayVisibility { get; set; }

		public Visibility OverlayVisibility { get => _OverlayVisibility;
			set {
                if (_OverlayVisibility != value)
                {
                    _OverlayVisibility = value;
					OnPropertyChanged(nameof(OverlayVisibility));
                }
            }
        }
		public string MarketName { get { return MarketNode?.MarketName; } }
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public TabContent() { }
        public TabContent(CustomTabHeader header)
        {
            customHeader = header;
            InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;
			marketHeader.TabContent = this;
			oBettingGrid.sliderControl = SliderControl;
            //OverlayVisibility = Visibility.Visible;
            //OverlayVisibility = Visibility.Hidden;
            timer.Elapsed += (o, e) =>
            {
                OnPropertyChanged(nameof(MarketStatus));
            };
            timer.Interval = 1000;
            timer.Enabled = true;
            timer.Start();
        }
        private async Task<MarketDefinition.StatusEnum> GetMarketStatus(string marketId)
        {
            // Use Betfair's listMarketBook API
            var status = await BetfairAPI.BetfairAPI.GetMarketStatusAsync(marketId);
            return (MarketDefinition.StatusEnum)status;
        }
        private async void CheckMarketStatus()
        {
            var status = await GetMarketStatus(MarketNode.MarketID);

            if (status == MarketDefinition.StatusEnum.Closed)
            {
                MarketStatus = status.ToString();
                OverlayVisibility = Visibility.Visible;
                //StatusMessage = "Market Closed";
            }
        }
        private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Changed")
			{
				dynamic d = data;
				MarketChangeDto change = d.MarketChangeDto;
                //OverlayVisibility = change.Status != MarketDefinition.StatusEnum.Open ? Visibility.Visible : Visibility.Hidden;
                OverlayVisibility = Visibility.Visible; 
				_MarketStatus = change.Status.ToString();
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
