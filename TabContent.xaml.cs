using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContent : UserControl, INotifyPropertyChanged
    {
        public MarketChangedDelegate OnMarketChanged;
        public CustomTabHeader customHeader = null;
        public MainWindow mainWindow { get; set; }

        public MarketSelectionDelegate OnMarketSelected;
        public Market MarketNode = null;
        public string MarketName { get { return MarketNode?.MarketName; } }
        public string OverlayStatus { get {
                if (MainWindow.Betfair.ConnectionLost)
                    return "Connection Lost";

                return MarketNode?.Status.ToString(); 
            } }
        public System.Windows.Visibility OverlayVisibility { get {
                if (MainWindow.Betfair.ConnectionLost)
                    return System.Windows.Visibility.Visible;

                return MarketNode == null || MarketNode?.Status == marketStatusEnum.OPEN ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;  
            }  }
        public Double? TotalMatched { get { return MarketNode?.TotalMatched;  }  }
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

            OnMarketChanged += (node) => 
            {
                NotifyPropertyChanged("");
                marketHeader.NotifyPropertyChanged("");
            };

            oBettingGrid.sliderControl = SliderControl;

            BetsManager.RunnersControl = RunnersControl;
            SliderControl.OnSubmitBets += BetsManager.OnSubmitBets;
            RunnersControl.OnMarketChanged += OnMarketChanged;
            RunnersControl.OnFavoriteChanged += BetsManager.OnFavoriteChanged;
            RunnersControl.OnFavoriteChanged += SliderControl.OnFavoriteChanged;
            OnMarketSelected += (node) =>
            {
                RunnersControl.OnMarketSelected(node);
                if (mainWindow.TabControl.SelectedContent == this)
                {
                    MarketNode = node;
                    customHeader.Title = node.FullName;
                    marketHeader.NotifyPropertyChanged("");
                }
                BetsManager.OnMarketSelected(node);
                oBettingGrid.OnMarketSelected(node);
            };
            marketHeader.TabContent = this;
        }
    }
}
