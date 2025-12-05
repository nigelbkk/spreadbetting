using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class CustomTabHeader : UserControl
    {
        public MainWindow mainWindow { get; set; }
        public String Title { set { TabTitle.Content = value; } }
        public Int32 ID { get; set; }
        public TabItem Tab { get; set; }
        private bool inPlay { get; set; }
        public CustomTabHeader()
        {
            InitializeComponent();
        //    StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
        //    {
        //        inPlay = inplay;
        //        if (marketid != "")         //TODO
        //        {
        //            this.Dispatcher.Invoke(() =>
        //            {
        //                TabTitle.Foreground = inplay ? Brushes.LightGreen : Brushes.DarkSlateGray;
        //            });
        //        }
        //    };
        }
        public void OnMatched()
        {
            if (!Tab.IsSelected)
            {
                TabTitle.Foreground = Brushes.WhiteSmoke;
                TabTitle.Background = Brushes.Orange;
            }
        }
        public void OnSelected()
        {
            TabTitle.Foreground = inPlay ? Brushes.LightGreen : Brushes.DarkSlateGray;
            TabTitle.Background = Brushes.Transparent;
        }
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (mainWindow != null)
            {
                mainWindow.RemoveTab(this);
            }
        }
    }
}
