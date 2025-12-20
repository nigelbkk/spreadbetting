using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace SpreadTrader
{
    public partial class BettingGrid : UserControl, INotifyPropertyChanged
    {
        private bool _BackActive { get; set; }
        public bool BackActive
        {
            get { return _BackActive; }
            set { _BackActive = value; if (BackValues.Length > 0) foreach (PriceSize o in BackValues) { o.IsChecked = o.ParentChecked = value; } }
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
        public SliderControl sliderControl { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        private void OnMessageReceived(string messageName, object data)
        {
            if (messageName == "Market Selected")
            {
                dynamic d = data;
				NodeViewModel d2 = d.NodeViewModel;
                MarketNode = d2;

				BackValues = sliderControl.BackValues;
				LayValues = sliderControl.LayValues;
				NotifyPropertyChanged("");
			}
            if (messageName == "Execute Bets")
            {
                dynamic d = data;
                Debug.WriteLine($"BettingGrid: {messageName}");
            }
        }

        public BettingGrid()
        {
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;

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

            BackActive = LayActive = true;
        }
    }
}