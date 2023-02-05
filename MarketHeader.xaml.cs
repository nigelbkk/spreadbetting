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
        private NodeViewModel MarketNode { get 
            {
                return TabContent != null && TabContent.MarketNode != null ? TabContent.MarketNode : null;
            } 
        }
        public TabContent TabContent { get; set; }
        public String FullName { get { return MarketNode == null ? "No Market Selected" : MarketNode.MarketName; } }
        public String TimeToGo { get { return MarketNode == null ? "00:00:00" : MarketNode.TimeToGo;  } }
        public double TurnaroundTime { get { return MarketNode == null ? 0 : MarketNode.TurnaroundTime; } }
        public Int32 UpdateRate { get { return MarketNode == null ? 0 : MarketNode.UpdateRate; } }
        public Double? TotalMatched { get { return MarketNode == null ? 0 : MarketNode.TotalMatched; } }
        
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
                        new MarketDescription(this, b, TabContent.MarketNode).ShowDialog(); 
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
        private double previous_width = 200;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double present_width = Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width.Value;

            if (present_width > 10)
                previous_width = Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width.Value;

            double new_width = present_width >=2 ? 0 : previous_width;
            Extensions.MainWindow.InnerGrid.ColumnDefinitions[0].Width = new System.Windows.GridLength(new_width);
        }
    }
}
