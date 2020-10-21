using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetfairAPI;
using OurTuple = System.Tuple<string, int>;

namespace SpreadTrader
{
	public class TreeViewModel
	{
		public ObservableCollection<NodeViewModel> Items { get; set; }
		public TreeViewModel()
		{
			Items = new ObservableCollection<NodeViewModel>();
		}
	}
	public class NodeViewModel
	{
		public delegate void CallBackDelegate();
		public CallBackDelegate CallBack = null;
		public bool Empty { get { return Children.Count == 0;  } }
		public Object Tag { get; set; }
		public NodeViewModel Parent { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public ObservableCollection<NodeViewModel> Children { get; set; }
		public NodeViewModel(String name)
		{
			Name = name;
			Children = new ObservableCollection<NodeViewModel>();
		}
		public void Add(NodeViewModel child)
		{
			child.Parent = this;
			Children.Add(child);
			child.Children.Add(new NodeViewModel("x"));
		}
		public void PopulateCountries()
		{
			List<CountryCodeResult> countries = EventsModel.Betfair.GetCountries(7);
			foreach (CountryCodeResult cr in countries)
			{
				Add(new NodeViewModel(cr.countryCode));
				CallBack = PopulateVenues;
			}
		}
		public void PopulateVenues()
		{
			List<VenueResult> venues = EventsModel.Betfair.GetVenues(7, this.Name).OrderBy(o => o.venue).ToList();
			foreach (VenueResult v in venues)
			{
				Add(new NodeViewModel(v.venue));
			}
		}
	}
	public class EventsModel : INotifyPropertyChanged
	{
		public ICommand ExpandingCommand { get; set; }
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
		public TreeViewModel TreeModel { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public void ExpandNode(TreeViewItem tvi)
		{
			NodeViewModel nvm = tvi.Items[0] as NodeViewModel;
			if (!tvi.IsExpanded && nvm.Parent.Children.Count == 1)// && nvm.Name == "x")
			{
				nvm.Parent.Children.Clear();
				if (nvm.Parent.CallBack != null)
				{
					nvm.Parent.CallBack();
				}
				//List<CountryCodeResult> countries = Betfair.GetCountries(7);
				//foreach (CountryCodeResult cr in countries)
				//{
				//	nvm.Parent.Add(new NodeViewModel(cr.countryCode));
				//}
			}
		}
		public void OnItemExpanding(object sender, MouseButtonEventArgs e)
		{

		}
		public EventsModel(BetfairAPI.BetfairAPI Betfair)
		{
			EventsModel.Betfair = Betfair;
			TreeModel = new TreeViewModel();

			List<EventTypeResult> eventTypes = Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
			foreach (EventTypeResult ev in eventTypes)
			{
				if (Favourites.IsFavourite(ev.eventType.id))
				{
					NodeViewModel root = new NodeViewModel(ev.eventType.name);
					root.CallBack = root.PopulateCountries;
					TreeModel.Items.Add(root);
					root.Add(new NodeViewModel("x"));
					List<CompetitionResult> competitions = Betfair.GetCompetitions(ev.eventType.id);

					return;

					if (competitions.Count == 0)
					{
						List<CountryCodeResult> countries = Betfair.GetCountries(ev.eventType.id);
						foreach (CountryCodeResult cr in countries)
						{
							NodeViewModel node2 = new NodeViewModel(cr.countryCode);
							List<VenueResult> venues = Betfair.GetVenues(ev.eventType.id, cr.countryCode).OrderBy(o => o.venue).ToList();

							if (venues.Count == 0)
								continue;
							root.Children.Add(node2);

							foreach (VenueResult v in venues)
							{
								NodeViewModel node3 = new NodeViewModel(v.venue);
								List<Event> events = Betfair.GetEvents(ev.eventType.id, cr.countryCode).OrderBy(o => o.details.name).ToList();

								if (events.Count == 0)
									continue;

								node2.Children.Add(node3);

								foreach (Event e2 in events)
								{
									NodeViewModel node4 = new NodeViewModel(e2.details.name);

									//List<Market> markets = Betfair.GetMarkets(e2.details.id, cr.countryCode).OrderBy(o => o.marketName).ToList();
									//if (markets.Count == 0)
									//	continue;

									//node2.Children.Add(node4);
									//foreach (Market m in markets)
									//{
									//	NodeViewModel node5 = new NodeViewModel(m.marketName) { Tag = m, Id = m.marketId };
									//	node4.Children.Add(node5);
									//}
								}
							}
						}
					}
					else
					{
						foreach (CompetitionResult cr in competitions)
						{
							NodeViewModel node2 = new NodeViewModel(cr.competition.name);
							root.Children.Add(node2);
							List<Event> events = Betfair.GetEvents(cr.competition.id).OrderBy(o => o.details.name).ToList();
							foreach (Event e2 in events)
							{
								NodeViewModel node3 = new NodeViewModel(e2.details.name);
								node2.Children.Add(node3);

								List<Market> markets = Betfair.GetMarkets(e2.details.id).OrderBy(o => o.marketName).ToList();
								foreach (Market m in markets)
								{
									NodeViewModel node4 = new NodeViewModel(m.marketName) { Tag = m, Id = m.marketId, Parent = null };
									node3.Children.Add(node4);
								}
							}
						}
					}
				}
			}

			//TreeModel.Items.Add(new NodeViewModel() { Name = "American Football" });
			//TreeModel.Items.Add(new NodeViewModel() { Name = "Gaelic Football" });
			//TreeModel.Items[0].Children.Add(new NodeViewModel() { Name = "NFL" });
			//TreeModel.Items[1].Children.Add(new NodeViewModel() { Name = "GAA" });
		}
	}
}
