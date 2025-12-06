using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class EventsTreeControl : UserControl, INotifyPropertyChanged
    {
        public String Status
        {
            set
            {
                Dispatcher.BeginInvoke(new Action(() => { Extensions.MainWindow.Status = value; }));
            }
        }

        public EventsTreeControl()
        {
            InitializeComponent();
        }
        public Market RootNode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public void Refresh()
        {
            RootNode.Clear();
            RootNode.PopulateEventTypes();
        }
        public void Populate()
        {
            RootNode = new Market(MainWindow.Betfair);
            RootNode.PopulateEventTypes();
        }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Populate();
                NotifyPropertyChanged("");
            }
            catch (Exception xe)
            {
                _ = xe.Message;
            }
        }
    }
}
