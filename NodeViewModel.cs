using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BetfairAPI;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SpreadTrader
{
	public class NodeViewModel : ViewModelBase, INotifyPropertyChanged
	{
		private bool _isSelected;
		private bool _IsExpanded;
		public bool IsExpanded { get { return _IsExpanded; } set { _IsExpanded = value; OnItemExpanding(); } }
		public bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				_isSelected = value;
				Console.WriteLine(@"IsSelected set");
				OnItemSelected();
				OnPropertyChanged();
			}
		}
		public delegate void CallBackDelegate();
		public CallBackDelegate Populate = null;
		private void OnItemSelected()
		{
			Market m = Tag as Market;
			if (m != null)
			{
				try
				{
					//MainWindow.LiveRunners.Clear();
					//MarketBook book = EventsModel.Betfair.GetMarketBook(m);
					//foreach (Runner r in book.Runners)
					//{
					//	if (r.removalDate == new DateTime())
					//	{
					//		LiveRunner rl = new LiveRunner(r);
					//		MainWindow.LiveRunners.Add(rl);
					//		if (r.ex.availableToBack.Count > 0)
					//		{
					//		}
					//	}
					//}
					//OnPropertyChanged("");
					//MainWindow.Status = "Ready";
				}
				catch (Exception xe)
				{
					//	MainWindow.Status = "Markets_SelectionChanged: " + String.Format(xe.Message.ToString()); 
				}
			}
		}
		private void OnItemExpanding()
		{
			Console.WriteLine(this.Name + (IsExpanded ? @" Expanded" : @" Collapsed"));
			if (IsExpanded && Populate != null)
			{
				Children.Clear();
				Populate();
			}
		}
		public Object Tag { get; set; }
		public NodeViewModel Parent { get; set; }
		public string Country { get; set; }
		public Int32 Id { get; set; }
		public string Name { get; set; }
		public ObservableCollection<NodeViewModel> Children { get; set; }
		public NodeViewModel(String name)
		{
			Name = name;
			Children = new ObservableCollection<NodeViewModel>();
		}
		public void Add(NodeViewModel child, bool leaf = false)
		{
			child.Parent = this;
			Children.Add(child);
			if (child.Populate != null) child.Children.Add(new NodeViewModel("x"));
			OnPropertyChanged("");
		}
		public void PopulateEventTYpes()
		{
			List<EventTypeResult> eventTypes = EventsModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
			foreach (EventTypeResult ev in eventTypes)
			{
				if (Favourites.IsFavourite(ev.eventType.id))
				{
					NodeViewModel nvm = new NodeViewModel(ev.eventType.name) { Id = ev.eventType.id };
					nvm.Populate = nvm.PopulateCountries;
					Add(nvm);
				}
			}
		}
		public void PopulateCompetitions()
		{
			List<CompetitionResult> competitions = EventsModel.Betfair.GetCompetitions(Id).OrderBy(o => o.competition.name).ToList();
			foreach (CompetitionResult cr in competitions)
			{
				NodeViewModel nvm = new NodeViewModel(cr.competition.name) { Id = this.Id, Country = this.Country };
				nvm.Populate = nvm.PopulateEvents;
				Add(nvm);
			}
		}
		public void PopulateEvents()
		{
			List<Event> events = EventsModel.Betfair.GetEvents(Id, Country).OrderBy(o => o.details.name).ToList();
			foreach (Event ev in events)
			{
				NodeViewModel nvm = new NodeViewModel(ev.details.name) { Id = this.Id, Country = this.Country };
				nvm.Populate = nvm.PopulateMarkets;
				Add(nvm);
			}
		}
		public void PopulateCountries()
		{
			List<CountryCodeResult> countries = EventsModel.Betfair.GetCountries(Id);
			foreach (CountryCodeResult cr in countries)
			{
				NodeViewModel nvm = new NodeViewModel(cr.countryCode) { Id = Id, Country = cr.countryCode };
				nvm.Populate = nvm.PopulateVenues;
				Add(nvm);
			}
		}
		public void PopulateVenues()
		{
			List<VenueResult> venues = EventsModel.Betfair.GetVenues(Id, Country).OrderBy(o => o.venue).ToList();
			foreach (VenueResult v in venues)
			{
				NodeViewModel nvm = new NodeViewModel(v.venue) { Id = this.Id, Country = v.venue };
				nvm.Populate = nvm.PopulateMarkets;
				Add(nvm);
			}
		}
		public void PopulateMarkets()
		{
			List<Market> markets = EventsModel.Betfair.GetMarkets(Id, Country).OrderBy(o => o.marketStartTime).ToList();
			foreach (Market m in markets)
			{
				NodeViewModel nvm = new NodeViewModel(m.ToString()) { Tag = m };
				//nvm.Populate = nvm.PopulateMarketBook;
				Add(nvm);
			}
		}
	}
}
