using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BetfairAPI;

namespace SpreadTrader
{
	public class TreeViewModel
	{
		public ObservableCollection<NodeViewModel> Items { get; set; }
	}
	public class NodeViewModel
	{
		public Object item { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public ObservableCollection<NodeViewModel> Children { get; set; }
	}
	public class EventsModel : INotifyPropertyChanged
	{
		public TreeViewModel TreeModel { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public EventsModel(BetfairAPI.BetfairAPI ng)
		{
			TreeModel = new TreeViewModel();
			TreeModel.Items = new ObservableCollection<NodeViewModel>();

			List<EventTypeResult> eventTypes = ng.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
			foreach (EventTypeResult ev in eventTypes)
			{
				if (!Favourites.IsFavourite(ev.eventType.id))
					continue;

				NodeViewModel node = new NodeViewModel() { Name = ev.eventType.name, Children = new ObservableCollection<NodeViewModel>() };
				TreeModel.Items.Add(node);
				List<CompetitionResult> competitions = ng.GetCompetitions(ev.eventType.id);
				foreach (CompetitionResult cr in competitions)
				{
					NodeViewModel node2 = new NodeViewModel() { Name = cr.competition.name, Children = new ObservableCollection<NodeViewModel>() };
					node.Children.Add(node2);
					List<Event> events = ng.GetEvents(ev.eventType.id).OrderBy(o => o.details.name).ToList();
					foreach (Event e2 in events)
					{
						NodeViewModel node3 = new NodeViewModel() { Name = e2.details.name, Children = new ObservableCollection<NodeViewModel>() };
						node2.Children.Add(node3);

						List<Market> markets = ng.GetMarkets(e2.details.id).OrderBy(o => o.marketName).ToList();
						foreach (Market m in markets)
						{
							NodeViewModel node4 = new NodeViewModel() { Name = m.details.name, item=m, Id = m.marketId, Children = new ObservableCollection<NodeViewModel>() };
							node3.Children.Add(node4);
						}
					}
				}
			}

			//TreeModel.Items.Add(new NodeViewModel() { Name = "American Football", Children = new ObservableCollection<NodeViewModel>() });
			//TreeModel.Items.Add(new NodeViewModel() { Name = "Gaelic Football", Children = new ObservableCollection<NodeViewModel>() });

			//TreeModel.Items[0].Children.Add(new NodeViewModel() { Name = "NFL", Children = new ObservableCollection<NodeViewModel>() });
			//TreeModel.Items[1].Children.Add(new NodeViewModel() { Name = "GAA", Children = new ObservableCollection<NodeViewModel>() });
		}
	}
}
