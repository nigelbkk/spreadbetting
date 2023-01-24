using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class MarketHeader : UserControl, INotifyPropertyChanged
    {
        public MarketSelectionDelegate OnMarketSelected;
        public TabContentControl TabContent { get; set; }
        public NodeViewModel MarketNode { get; set; }
        public Visibility up_visible { get; set; }
        public Visibility down_visible { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public SolidColorBrush StreamingColor { get { return System.Windows.Media.Brushes.LightGreen; } }
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public MarketHeader()
        {
            InitializeComponent();
            OnMarketSelected += (node) =>
            {
                MarketNode = node;
                NotifyPropertyChanged("");
            };
            up_visible = Visibility.Visible;
            down_visible = Visibility.Collapsed;
            NotifyPropertyChanged("");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            try
            {
                switch (b.Tag)
                {
                    case "Market Description": new MarketDescription(this, b, MarketNode).ShowDialog(); break;
                    case "Hide Grid":
                        down_visible = TabContent.BettingGrid.Visibility;
                        up_visible = TabContent.BettingGrid.Visibility = TabContent.BettingGrid.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                        NotifyPropertyChanged("");
                        break;
                }
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
                Extensions.MainWindow.Status = xe.Message;
            }
        }
    }
}
