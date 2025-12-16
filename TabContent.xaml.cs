using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class TabContent : UserControl, INotifyPropertyChanged
    {
        public CustomTabHeader customHeader = null;
        public MainWindow mainWindow { get; set; }
        public NodeViewModel MarketNode = null;
        public string MarketName { get { return MarketNode?.MarketName; } }
        //public string OverlayStatus
        //{
        //    get
        //    {
        //        if (MainWindow.Betfair.ConnectionLost)
        //            return "Connection Lost";

        //        return MarketNode?.Status.ToString();
        //    }
        //}
        //public System.Windows.Visibility OverlayVisibility
        //{
        //    get
        //    {
        //        if (MainWindow.Betfair.ConnectionLost)
        //            return System.Windows.Visibility.Visible;

        //        return MarketNode == null || MarketNode?.Status == marketStatusEnum.OPEN ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
        //    }
        //}
        //public Double? TotalMatched { get { return MarketNode?.TotalMatched; } }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
            }
        }

        public TabContent() { }
        public TabContent(CustomTabHeader header)
        {
            customHeader = header;
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;
            marketHeader.TabContent = this;
        }
        private void OnMessageReceived(string messageName, object data)
        {
            if (messageName == "Market Selected")
            {
                dynamic d = data;
                NodeViewModel d2 = d.NodeViewModel as NodeViewModel;
                Debug.WriteLine($"TabContent: {d2.FullName}");
                customHeader.Title = d2.FullName;
            }
        }
    }
}
