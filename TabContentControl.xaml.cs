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
