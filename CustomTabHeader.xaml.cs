using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class CustomTabHeader : UserControl
    {
        public MainWindow mainWindow { get; set; }
        public String Title { set { TabTitle.Content = value; } }
        public String MarketId { get; set; }
		public Int32 ID { get; set; }
        public TabItem Tab { get; set; }
        private bool inPlay { get; set; }
        public CustomTabHeader()
        {
            InitializeComponent();
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
