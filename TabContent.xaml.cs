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

        private String _MarketStatus;
        public String MarketStatus { get => _MarketStatus;
			
			set {
				if (_MarketStatus != value)
				{
					_MarketStatus = value;
					OnPropertyChanged(nameof(MarketStatus));
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
			OverlayVisibility = Visibility.Hidden;
		}
		private void OnMessageReceived(string messageName, object data)
		{
			if (messageName == "Market Changed")
			{
				dynamic d = data;
				MarketChangeDto change = d.MarketChangeDto;
				OverlayVisibility = change.Status != MarketDefinition.StatusEnum.Open ? Visibility.Visible : Visibility.Hidden;
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
