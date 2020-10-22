using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SpreadTrader
{
	public class EventsModel : INotifyPropertyChanged
	{
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
		public NodeViewModel Root{ get; set; }
		public ObservableCollection<NodeViewModel> Children { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public EventsModel(BetfairAPI.BetfairAPI Betfair)
		{
			EventsModel.Betfair = Betfair;
			Children = new ObservableCollection<NodeViewModel>(new NodeViewModel[]
			{
				//new NodeViewModel("C1x"),
				//new NodeViewModel("C2x"),
				//new NodeViewModel("C3x"),
			});
			//Children[0].Add(new NodeViewModel("X1"));
			//Children[0].Children[0].Add(new NodeViewModel("X2"));
		
			EventsModel.Betfair = Betfair;

			Root = new NodeViewModel("root");
			Root.Populate = Root.PopulateEventTYpes;
			Root.Add(new NodeViewModel("x"));
			Children.Add(Root);
		}
	}
}
