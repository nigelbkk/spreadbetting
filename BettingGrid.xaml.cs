using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public delegate void SubmitBetsDelegate(LiveRunner runner, PriceSize[] lay, PriceSize[] back);

    public partial class BettingGrid : UserControl, INotifyPropertyChanged
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
        public Market MarketNode { get; set; }
        public MarketSelectionDelegate OnMarketSelected;
        public SliderControl sliderControl { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public BettingGrid()
        {
            BetfairPrices betfairPrices = new BetfairPrices();
            BackValues = new PriceSize[9];
            LayValues = new PriceSize[9];
            for (Int32 i = 0; i < 9; i++)
            {
                BackValues[i] = new PriceSize(betfairPrices[i], 20 + 1 * 10);
                LayValues[i] = new PriceSize(betfairPrices[i], 20 + 1 * 10);
            }
            for (int i = 0; i < 3; i++) BackValues[i].Color = Application.Current.FindResource("Back2Color") as SolidColorBrush;
            for (int i = 3; i < 6; i++) BackValues[i].Color = Application.Current.FindResource("Back1Color") as SolidColorBrush;
            for (int i = 6; i < 9; i++) BackValues[i].Color = Application.Current.FindResource("Back0Color") as SolidColorBrush;
            for (int i = 0; i < 3; i++) LayValues[i].Color = Application.Current.FindResource("Lay0Color") as SolidColorBrush;
            for (int i = 3; i < 6; i++) LayValues[i].Color = Application.Current.FindResource("Lay1Color") as SolidColorBrush;
            for (int i = 6; i < 9; i++) LayValues[i].Color = Application.Current.FindResource("Lay2Color") as SolidColorBrush;

            OnMarketSelected += (node) =>
            {
                if (IsLoaded)
                {
                    MarketNode = node;

                    BackValues = sliderControl.BackValues;
                    LayValues = sliderControl.LayValues;
                    NotifyPropertyChanged("");
				}
            };
            InitializeComponent();
            BackActive = LayActive = true;
        }
    }
}