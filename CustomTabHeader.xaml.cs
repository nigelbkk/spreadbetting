using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class CustomTabHeader : UserControl, INotifyPropertyChanged
	{
        public MainWindow mainWindow { get; set; }
		public String Title { set { Dispatcher.BeginInvoke(new Action(() => { TabTitle.Content = value; })); } }
		public SolidColorBrush ForegroundColour { get { return inPlay ? Brushes.Green : Brushes.Black; } }
		public String MarketId { get; set; }
		public Int32 ID { get; set; }
        public TabItem Tab { get; set; }
		private bool _inPlay { get; set; }
		public bool inPlay { get { return _inPlay; } set { _inPlay = value; NotifyPropertyChanged(""); } }
        public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}

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
		public void OnMarketSelected(NodeViewModel d2)
		{
			//MarketNode = d2;
			Title = d2.FullName;
			MarketId = d2.MarketID;
			//_ = RunnersControl.PopulateNewMarketAsync(d2);
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
