using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public delegate void SubmitBetsDelegate(LiveRunner runner, PriceSize[] lay, PriceSize[] back);
//    public delegate void SubmitBetsDelegate(LiveRunner runner, ItemsControl itemsControl);

    public partial class BettingGrid : UserControl
    {
        private bool _BackActive { get; set; }
        public bool BackActive
        {
            get { return _BackActive; }
            set
            {
                _BackActive = value;
                if (BackValues.Length > 0) foreach (PriceSize o in BackValues)
                    {
                        o.IsChecked = o.ParentChecked = value;
                    }
            }
        }
        private bool _LayActive { get; set; }
        public bool LayActive
        {
            get { return _LayActive; }
            set
            {
                _LayActive = value;
                if (LayValues.Length > 0) foreach (PriceSize o in LayValues)
                    {
                        o.IsChecked = o.ParentChecked = value;
                    }
            }
        }
        public PriceSize[] BackValues { get; set; }
        public PriceSize[] LayValues { get; set; }
        public NodeViewModel MarketNode { get; set; }
        public BettingGrid()
        {
            BackValues = SliderControl.BackValues;
            LayValues = SliderControl.LayValues;

            if (BackValues != null)
            {
                for (int i = 0; i < 3; i++) BackValues[i].Color = Application.Current.FindResource("Back2Color") as SolidColorBrush;
                for (int i = 3; i < 6; i++) BackValues[i].Color = Application.Current.FindResource("Back1Color") as SolidColorBrush;
                for (int i = 6; i < 9; i++) BackValues[i].Color = Application.Current.FindResource("Back0Color") as SolidColorBrush;
                for (int i = 0; i < 3; i++) LayValues[i].Color = Application.Current.FindResource("Lay0Color") as SolidColorBrush;
                for (int i = 3; i < 6; i++) LayValues[i].Color = Application.Current.FindResource("Lay1Color") as SolidColorBrush;
                for (int i = 6; i < 9; i++) LayValues[i].Color = Application.Current.FindResource("Lay2Color") as SolidColorBrush;
                BackActive = LayActive = true;
            }
            InitializeComponent();
        }
    }
}