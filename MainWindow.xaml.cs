using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.UI.Xaml.Automation;

namespace SpreadTrader
{
    public static class ControlMessenger
    {
        public static event Action<string, object> MessageSent;

        public static void Send(string messageName, object data = null)
        {
            MessageSent?.Invoke(messageName, data);
        }
    }
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
		#region Properties
        private Properties.Settings props = Properties.Settings.Default;
		private WebSocketsHub hub = new WebSocketsHub();
        private static String _Status = "Ready";
        private static String _Notification = "";
        public String Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                Debug.WriteLine(value);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("");
                }));
            }
        }

        public String Notification
        {
            get { return _Notification; }
            set
            {
                _Notification = value;
                Debug.WriteLine(value);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("");
                }));
            }
        }

        public String StakesPreselectTooltip0
        {
            get { return $"Set default stake to ${StakesPreselect0}"; }
        }

        public String StakesPreselectTooltip1
        {
            get { return $"Set default stake to ${StakesPreselect1}"; }
        }

        public String StakesPreselectTooltip2
        {
            get { return $"Set default stake to ${StakesPreselect2}"; }
        }

        public double StakesPreselect0
        {
            get { return Convert.ToDouble(props.StakesPreselect.Split(',')[0]); }
        }

        public double StakesPreselect1
        {
            get { return Convert.ToDouble(props.StakesPreselect.Split(',')[1]); }
        }

        public double StakesPreselect2
        {
            get { return Convert.ToDouble(props.StakesPreselect.Split(',')[2]); }
        }

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
		#endregion Properties
		private Mutex mutex = null;
        public static TabContent SelectedTab {get;set;}

        private void OnMessageReceived(string messageName, object data)
        {
			if (messageName == "Market Selected")
			{
				dynamic d = data;
				NodeViewModel d2 = d.NodeViewModel as NodeViewModel;
				Debug.WriteLine($"MainWindow: {d2.FullName}");
				Task.Run(() => SelectedTab.OnSelected(d2));
			}
//			if (messageName == "Market Changed")
//			{
//				dynamic d = data;
////				MarketSnapDto snap = d.MarketSnapDto;
//				MarketChange change = d.MarketChange;
//			}
			if (messageName == "Orders Changed")
			{
				dynamic d = data;
				String json = d.String;
				Task.Run(() => SelectedTab.OnOrdersChanged(json));
			}
		}

		public MainWindow()
        {
            const string appName = "SpreadTrader";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Debug.WriteLine(appName + " already running Exiting now");
                Environment.Exit(0);
            }

            Debug.WriteLine("Continuing with the application");
            ServicePointManager.DefaultConnectionLimit = 800;
            System.Net.ServicePointManager.Expect100Continue = false;
            InitializeComponent();
			ControlMessenger.MessageSent += OnMessageReceived;

			this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            this.Top = Math.Max(0, props.Top);
            this.Left = Math.Max(0, props.Left);
            this.Height = Math.Max(0, props.Height);
            this.Width = Math.Max(0, props.Width);

            Betfair = new BetfairAPI.BetfairAPI();
            //Betfair.getServerTime();
            //UpdateAccountInformation();
        }
        public void UpdateAccountInformation()
        {
            AccountFundsResponse response = Betfair.getAccountFunds(1);
            Balance = response.availableToBetBalance;
            Exposure = response.exposure;
            Commission = response.retainedCommission - response.discountRate;
            NotifyPropertyChanged("");
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
                    case "Commission":
                        UpdateAccountInformation();
                        break;
                    case "25":
                        props.DefaultStake = Convert.ToDouble(props.StakesPreselect.Split(',')[0]);
                        break;
                    case "50":
                        props.DefaultStake = Convert.ToDouble(props.StakesPreselect.Split(',')[1]);
                        break;
                    case "100":
                        props.DefaultStake = Convert.ToDouble(props.StakesPreselect.Split(',')[2]);
                        break;
                }
                props.Save();
                NotifyPropertyChanged("");
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
        public void RemoveTab(CustomTabHeader e)
        {
            TabControl.Items.Remove(e.Tab);
            TabContent tabContent = e.Tab.Content as TabContent;
            //EventsTree.OnMarketSelected -= tabContent.OnMarketSelected;
            e.Tab.Content = null;
        }
        private void AppendNewTab(String title)
        {
            CustomTabHeader customTabHeader = new CustomTabHeader();
            customTabHeader.mainWindow = this;
            customTabHeader.Title = String.Format("Tab {0}", TabControl.Items.Count);
            customTabHeader.ID = TabControl.Items.Count;

            TabItem tab = new TabItem();
            tab.TabIndex = TabControl.Items.Count;
            tab.PreviewMouseDown += TabItem_PreviewMouseDown2;
            tab.Header = customTabHeader;

            TabContent tabContent = new TabContent(customTabHeader);
            //tabContent.mainWindow = this;
            tabContent.customHeader = customTabHeader;
            tab.Content = tabContent;

            customTabHeader.Tab = tab;

            //EventsTree.OnMarketSelected += tabContent.OnMarketSelected;
            //OnShutdown += tabContent.BetsManager.OnShutdown;
            //OnMarketChanged += tabContent.OnMarketSelected;

            tab.IsSelected = true;
            TabControl.Items.Insert(0, tab);
            TabControl.UpdateLayout();
            Dispatcher.BeginInvoke(new Action(() => { tab.Focus(); }));
            NotifyPropertyChanged("");
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            props.Save();
            //if (OnShutdown != null)
            //{
            //	OnShutdown();
            //};
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
            if (e.AddedItems.Count > 0)
            {
                TabItem ti = e.AddedItems[0] as TabItem;
                if (ti != null)
                {
					SelectedTab = ti.Content as TabContent;

					CustomTabHeader cth = ti.Header as CustomTabHeader;
                    TabContent tc2 = ti.Content as TabContent;
                    BetsManager bm = tc2.BetsManager;
                    RunnersControl rc = tc2.RunnersControl;
				}
				TabContent content = TabControl.SelectedContent as TabContent;
                Commission = content.MarketNode == null ? 0.00 : content.MarketNode.Commission;

                //SliderControl sc = content.SliderControl;
                //BettingGrid bg = content.oBettingGrid;
                //if (bg != null)
                //{
                //	bg.OnSelectionChanged(sender, e);
                //}
            }
        }
        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TabItem;
            var parent = item.Parent as TabControl;
            AppendNewTab("");
        }
        private void TabItem_PreviewMouseDown2(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TabItem;
            var parent = item.Parent as TabControl;
        }
        private void TabControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabControl tc = sender as TabControl;
            //            tc.Items.Remove(tc.SelectedItem);
        }
        private void TabControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tc = sender as TabControl;

        }
    }
}
