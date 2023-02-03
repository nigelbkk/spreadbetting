using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
    public delegate void MarketSelectionDelegate(NodeViewModel node);
    public delegate void OnShutdownDelegate();
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public OnShutdownDelegate OnShutdown = null;
        private Properties.Settings props = Properties.Settings.Default;
        private static String _Status = "Ready";
        public String Status { get { return _Status; } set { 
                
                _Status = value; 
                Debug.WriteLine(value);
                Dispatcher.BeginInvoke(new Action(() => {
                    NotifyPropertyChanged("");
                }));
            } }
        public double Balance { get; set; }
        public double Exposure { get; set; }
        private double _DiscountRate { get; set; }
        public double DiscountRate { get { return _DiscountRate; } set { _DiscountRate = value; NotifyPropertyChanged("NetCommission"); } }
        private double _Commission { get; set; }
        public double Commission { get { return _Commission; } set { _Commission = value; NotifyPropertyChanged("Commission"); } }
        public double NetCommission { get { return _Commission - DiscountRate; } }
        public static BetfairAPI.BetfairAPI Betfair { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
            }
        }
        public MainWindow()
        {
            ServicePointManager.DefaultConnectionLimit = 800;
            System.Net.ServicePointManager.Expect100Continue = false;
            InitializeComponent();
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            Betfair = new BetfairAPI.BetfairAPI();
            if (!props.UseProxy)
            {
                Betfair.login(props.CertFile, props.CertPassword, props.AppKey, props.BFUser, props.BFPassword);
            }
            UpdateAccountInformation();
            this.Top = props.Top;
            this.Left = props.Left;
            this.Height = props.Height;
            this.Width = props.Width;
        }
        public void UpdateAccountInformation()
        {
            try
            {
                AccountFundsResponse response = Betfair.getAccountFunds(1);
                Balance = response.availableToBetBalance;
                Exposure = response.exposure;
                //Commission = response.retainedCommission - response.discountRate;
                NotifyPropertyChanged("");
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) // move to tab
        {
            Button b = sender as Button;
            try
            {
                switch (b.Tag)
                {
                    case "Settings":
                        new Settings(this, b).ShowDialog();
                        break;
                    case "Refresh":
                        EventsTree.Refresh();
                        break;
                    case "Favourites":
                        if (NodeViewModel.Betfair == null) break;
                        new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()).ShowDialog(); break;
                }
                e.Handled = true;
            }
            catch (Exception xe)
            {
                Status = xe.Message.ToString();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppendNewTab("first");
        }
        private void AppendNewTab(String title)
        {
            TabItem tab = new TabItem();
            tab.Header = String.Format("new header {0}", TabControl.Items.Count);
            TabContent tabContent = new TabContent(tab);
            tabContent.tabItem = tab;

            tab.Content = tabContent;
            EventsTree.OnMarketSelected += tabContent.OnMarketSelected;

            TabControl.Items.Insert(0, tab);
            tab.Focus();
            NotifyPropertyChanged("");
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            props.Save();
            if (OnShutdown != null)
            {
                OnShutdown();
            };
        }
        private void OnUpdateAccount(object sender, MouseButtonEventArgs e)
        {
            UpdateAccountInformation();
            NotifyPropertyChanged("");
        }
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            props.Top = this.Top;
            props.Left = this.Left;
            props.Save();
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            props.Width = this.Width;
            props.Height = this.Height;
            props.Save();
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tc = sender as TabControl;
            TabContent tcc = tc.SelectedContent as TabContent;

            var selected = tc.SelectedItem;
            NotifyPropertyChanged("");

            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0)
            {
                var tcc2 = e.AddedItems[0];
                var ctnew = e.AddedItems[0] as TabItem;
                var tcnew = ctnew.Content;
                //tcc.tabItem.Header = tcc?.MarketName;
                //var v = tcc?.MarketName;
            }
        }

        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item= sender as TabItem;
            var parent = item.Parent as TabControl;
            AppendNewTab("awooga");
        }
    }
}
