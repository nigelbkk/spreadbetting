using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;


namespace SpreadTrader
{
	public partial class EventsTreeControl : UserControl, INotifyPropertyChanged
	{
		public EventsTreeControl()
		{
			InitializeComponent();
		}
		public MarketSelectionDelegate OnMarketSelected;
		public NodeViewModel RootNode { get; set; }
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
			RootNode = new NodeViewModel(MainWindow.Betfair);
			RootNode.OnMarketSelected += (node) =>
			{
				if (OnMarketSelected != null)
				{
					OnMarketSelected(node);
				}
			};
			RootNode.PopulateEventTypes();
		}
		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Populate();
			NotifyPropertyChanged("");
		}
	}
}
