using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
//using BetfairAPI;

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
			Children = new ObservableCollection<NodeViewModel>();
			Children = new ObservableCollection<NodeViewModel>(new NodeViewModel[] { new NodeViewModel("") { Name = "1" }, new NodeViewModel("") { Name = "2" }, new NodeViewModel("") { Name = "3" } });
			Children = new ObservableCollection<NodeViewModel>(new NodeViewModel[]
			{
               new NodeViewModel("C1x"),
               new NodeViewModel("C2x"),
               new NodeViewModel("C3x"),
			});
			Children[0].Add(new NodeViewModel("X1"));
			Children[0].Children[0].Add(new NodeViewModel("X2")); 
			
			EventsModel.Betfair = Betfair;

			Root = new NodeViewModel("root");
			Root.Populate = Root.PopulateEventTYpes;
			Root.Add(new NodeViewModel("root"));
			Children.Add(Root);
			Children.Add(new NodeViewModel("American Football") { Name = "American Football" });
			//TreeModel.Items.Add(new NodeViewModel() { Name = "Gaelic Football" });
			//TreeModel.Items[0].Children.Add(new NodeViewModel() { Name = "NFL" });
			//TreeModel.Items[1].Children.Add(new NodeViewModel() { Name = "GAA" });

			NotifyPropertyChanged("");

			return;

////			List<EventTypeResult> eventTypes = Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
//			foreach (EventTypeResult ev in eventTypes)
//			{
//				if (Favourites.IsFavourite(ev.eventType.id))
//				{
//					NodeViewModel root = new NodeViewModel(ev.eventType.name) { Id = ev.eventType.id };
//					root.Populate = root.PopulateCountries;
//					Children.Add(root);
//					root.Add(new NodeViewModel("x"));
//					List<CompetitionResult> competitions = Betfair.GetCompetitions(ev.eventType.id);

//					if (competitions.Count == 0)
//					{
//						List<CountryCodeResult> countries = Betfair.GetCountries(ev.eventType.id);
//						foreach (CountryCodeResult cr in countries)
//						{
//							NodeViewModel node2 = new NodeViewModel(cr.countryCode);
//							node2.Populate = node2.PopulateVenues;
//							root.Add(node2);

//							//foreach (VenueResult v in venues)
//							//{
//							//	NodeViewModel node3 = new NodeViewModel(v.venue);
//							//	List<Event> events = Betfair.GetEvents(ev.eventType.id, cr.countryCode).OrderBy(o => o.details.name).ToList();

//							//	if (events.Count == 0)
//							//		continue;

//							//	node2.Children.Add(node3);

//							//	foreach (Event e2 in events)
//							//	{
//							//		NodeViewModel node4 = new NodeViewModel(e2.details.name);

//							//		//List<Market> markets = Betfair.GetMarkets(e2.details.id, cr.countryCode).OrderBy(o => o.marketName).ToList();
//							//		//if (markets.Count == 0)
//							//		//	continue;

//							//		//node2.Children.Add(node4);
//							//		//foreach (Market m in markets)
//							//		//{
//							//		//	NodeViewModel node5 = new NodeViewModel(m.marketName) { Tag = m, Id = m.marketId };
//							//		//	node4.Children.Add(node5);
//							//		//}
//							//	}
//							//}
//						}
//					}
//					else
//					{
//						foreach (CompetitionResult cr in competitions)
//						{
//							NodeViewModel node2 = new NodeViewModel(cr.competition.name);
//							//root.Children.Add(node2);
//							List<Event> events = Betfair.GetEvents(cr.competition.id).OrderBy(o => o.details.name).ToList();
//							foreach (Event e2 in events)
//							{
//								NodeViewModel node3 = new NodeViewModel(e2.details.name);
//								node2.Children.Add(node3);

//								List<Market> markets = Betfair.GetMarkets(e2.details.id).OrderBy(o => o.marketName).ToList();
//								foreach (Market m in markets)
//								{
//									NodeViewModel node4 = new NodeViewModel(m.marketName) { Tag = m, Id = m.marketId, Parent = null };
//									node3.Children.Add(node4);
//								}
//							}
//						}
//					}
//				}
//			}

			//TreeModel.Items.Add(new NodeViewModel() { Name = "American Football" });
			//TreeModel.Items.Add(new NodeViewModel() { Name = "Gaelic Football" });
			//TreeModel.Items[0].Children.Add(new NodeViewModel() { Name = "NFL" });
			//TreeModel.Items[1].Children.Add(new NodeViewModel() { Name = "GAA" });
		}
	}
}
