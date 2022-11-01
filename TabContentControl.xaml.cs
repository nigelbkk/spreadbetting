using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContentControl : UserControl
    {
        public MarketSelectionDelegate OnMarketSelected;
        public TabContentControl()
        {
            InitializeComponent();
            BetsManager.RunnersControl = RunnersControl;
            SliderControl.OnSubmitBets += BetsManager.OnSubmitBets;
            RunnersControl.OnFavoriteChanged += BetsManager.OnFavoriteChanged;
            RunnersControl.OnFavoriteChanged += SliderControl.OnFavoriteChanged;
            OnMarketSelected += (node) =>
            {
                RunnersControl.OnMarketSelected(node);
                MarketHeader.OnMarketSelected(node);
                BetsManager.OnMarketSelected(node);
            };
            MarketHeader.TabContent = this;
        }
    }
}
