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
		public MarketDefinition.StatusEnum? MarketStatus { get; set; }
		public Visibility OverlayVisibility { get; set; }
		public string MarketName { get { return MarketNode?.MarketName; } }
		public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
            }
        }
		public TabContent() { }
        public TabContent(CustomTabHeader header)
        {
            customHeader = header;
            InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;
			marketHeader.TabContent = this;
			oBettingGrid.sliderControl = SliderControl;
			OverlayVisibility = Visibility.Hidden;
			NotifyPropertyChanged("");
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
						Task.Run(() => OnMarketChanged(change));
					}
				}
			}
		}
		public async Task OnSelected(NodeViewModel d2)
		{
			marketHeader.OnSelected(d2);
			customHeader.OnSelected(d2);
			BetsManager.OnSelected(d2, RunnersControl);
			await RunnersControl.PopulateNewMarketAsync(d2);
		}
		public void OnOrdersChanged(String json)
		{
			BetsManager.OnOrderChanged(json);
		}
		void OnMarketChanged(MarketChangeDto change)
		{
			RunnersControl?.OnMarketChanged(change);
		}
		//void OnMarketChangedOld(MarketSnapDto snap)
		//{
		//	RunnersControl?.OnMarketChanged(snap);
		//	customHeader.inPlay = snap.InPlay;

		//	if (MarketStatus != snap.Status)
		//	{
		//		MarketStatus = snap.Status;
		//		if (MarketStatus == MarketDefinition.StatusEnum.Open)
		//		{
		//			OverlayVisibility = Visibility.Hidden;
		//		}
		//		else
		//		{
		//			OverlayVisibility = Visibility.Visible;
		//		}
		//		NotifyPropertyChanged("");
		//	}
		//}
	}
}
