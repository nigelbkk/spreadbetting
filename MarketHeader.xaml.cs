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
//        public MarketChangedDelegate OnMarketChanged;
        public TabContent TabContent { get; set; }
        public String FullName { get { return TabContent == null ? "No market selected" : TabContent?.MarketName; } }
        public String TimeToGo { get { return TabContent == null ? "" : TabContent.MarketNode.TimeToGo;  } }
        public double TurnaroundTime { get { return TabContent == null ? 0 :  TabContent.MarketNode.TurnaroundTime; } }
        public Int32 UpdateRate { get { return TabContent == null ? 0 : TabContent.MarketNode.UpdateRate; } }
        public Double? TotalMatched { get { return TabContent == null ? 0 : TabContent.MarketNode.TotalMatched; } }
        
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
            up_visible = Visibility.Visible;
            down_visible = Visibility.Collapsed;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            try
            {
                switch (b.Tag)
                {
                    case "Market Description": 
                        //new MarketDescription(this, b, MarketNode).ShowDialog(); 
                        break;
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
