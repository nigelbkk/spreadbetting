using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContent : UserControl
    {
        public MarketChangedDelegate OnMarketChanged;
        public CustomTabHeader customHeader = null;
        public MainWindow mainWindow { get; set; }

        public MarketSelectionDelegate OnMarketSelected;
        public NodeViewModel MarketNode = null;
        public string MarketName { get { return MarketNode?.MarketName;  }  }
        public Double? TotalMatched { get { return MarketNode?.TotalMatched;  }  }
        public TabContent() { }

        public TabContent(CustomTabHeader header)
        {
            customHeader = header;
            InitializeComponent();

            OnMarketChanged += (node) => 
            {
                marketHeader.NotifyPropertyChanged("");
            };

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
            };
            marketHeader.TabContent = this;
        }
    }
}
