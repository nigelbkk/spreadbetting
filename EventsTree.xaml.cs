using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class EventsTree : UserControl
	{
		public NodeSelectionDelegate NodeCallback = null;
		public NodeViewModel RootNode { get; set; }
		public event PropertyChangedEventHandler PropertyChanged2;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged2 != null)
			{
				PropertyChanged2(this, new PropertyChangedEventArgs(info));
			}
		}
		public EventsTree()
		{
			InitializeComponent();
		}
		public void Refresh()
		{
			RootNode.Clear();
			RootNode.PopulateEventTypes();
		}
		public void Populate()
		{
			RootNode = new NodeViewModel(MainWindow.Betfair);// new BetfairAPI.BetfairAPI());
			RootNode.NodeCallback += (node) =>
			{
				if (NodeCallback != null)
				{
					NodeCallback(node);
				}
			};
			RootNode.PopulateEventTypes();
		}
	}
}
