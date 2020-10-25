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
		public NotificationDelegate NotificationCallback = null;
		public SelectedNodeDelegate SelectedCallback = null;
		public ObservableCollection<LiveRunner> LiveRunners { get; set; }
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
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
			}
		}
		public delegate void CallBackDelegate();
		public CallBackDelegate Populate = null;
		public Object Tag { get; set; }
		public NodeViewModel Parent { get; set; }
		public Int32 ID { get; set; }
		public string Name { get; set; }
		public ObservableCollection<NodeViewModel> Nodes { get; set; }
		public NodeViewModel(BetfairAPI.BetfairAPI Betfair, NotificationDelegate Callback, SelectedNodeDelegate SelectedCallback)
		{
			NodeViewModel.Betfair = Betfair;
			Nodes = new ObservableCollection<NodeViewModel>();
			this.NotificationCallback = Callback;
			this.SelectedCallback = SelectedCallback;
			PopulateEventTYpes();
			Populate = PopulateCompetitionsOrCountries;
			NotificationCallback("Ready");

			Market m = new Market();
			m.marketId = "1.174361561";
			m.marketId = "1.174361562";
			Tag = m;
			OnItemSelected();
		}
		public NodeViewModel(String name)
		{
			Name = name;
			Nodes = new ObservableCollection<NodeViewModel>();
		}
		private void OnItemSelected()
		{
			Market m = Tag as Market;
			if (m != null)
			{
				try
				{
					LiveRunners = new ObservableCollection<LiveRunner>();
					MarketBook book = Betfair.GetMarketBook(m);
					book.Runners[0].Catalog.name = "Barcelona";
					book.Runners[1].Catalog.name = "Andorra";
					foreach (Runner r in book.Runners)
					{
						if (r.removalDate == new DateTime())
						{
							LiveRunner rl = new LiveRunner(r);
							LiveRunners.Add(rl);
							if (r.ex.availableToBack.Count > 0)
							{
							}
						}
					}
					SelectedCallback(this);
					NotificationCallback("Selection Changed: " + String.Format(m.ToString()));
				}
				catch (Exception xe)
				{
					NotificationCallback("Markets_SelectionChanged: " + String.Format(xe.Message.ToString()));
				}
			}
		}
		private void OnItemExpanding()
		{
			Console.WriteLine(this.Name + (IsExpanded ? @" Expanded" : @" Collapsed"));
			if (IsExpanded && Populate != null)
			{
				Nodes.Clear();
				Populate();
			}
		}
		public void Add(NodeViewModel node, bool leaf = false)
		{
			node.Parent = this;
			node.NotificationCallback = NotificationCallback;
			node.SelectedCallback = SelectedCallback;
			Nodes.Add(node);
			if (node.Populate != null) node.Nodes.Add(new NodeViewModel("x"));
		}
		public void PopulateEventTYpes()
		{
			List<EventTypeResult> eventTypes = Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
			foreach (EventTypeResult ev in eventTypes)
			{
				if (Favourites.IsFavourite(ev.eventType.id))
				{
					NodeViewModel nvm = new NodeViewModel(ev.eventType.name) { ID = ev.eventType.id };
					nvm.Populate = nvm.PopulateCompetitionsOrCountries;
					Add(nvm);
				}
			}
		}
		public void PopulateEvents()
		{
			List<Event> events = Betfair.GetEvents(ID, Tag as String).OrderBy(o => o.details.name).ToList();
			foreach (Event ev in events)
			{
				NodeViewModel nvm = new NodeViewModel(ev.details.name) { ID = ev.details.id, Tag = this.Tag };
				nvm.Populate = nvm.PopulateMarkets;
				Add(nvm);
			}
		}
		public void PopulateEventsForCompetition()
		{
			List<Event> events = Betfair.GetEventsForCompetition(ID).OrderBy(o => o.details.name).ToList();
			foreach (Event ev in events)
			{
				NodeViewModel nvm = new NodeViewModel(ev.details.name) { ID = ev.details.id };
				nvm.Populate = nvm.PopulateMarkets;
				Add(nvm);
			}
		}
		public void PopulateCompetitionsOrCountries()
		{
			List<CompetitionResult> competitions = Betfair.GetCompetitions(ID).OrderBy(o => o.competition.name).ToList();
			if (competitions.Count > 0)
			{
				foreach (CompetitionResult cr in competitions)
				{
					NodeViewModel nvm = new NodeViewModel(cr.competition.name) { ID = cr.competition.id };
					nvm.Populate = nvm.PopulateEventsForCompetition;
					Add(nvm);
				}
				return;
			}
			List<CountryCodeResult> countries = Betfair.GetCountries(ID);
			foreach (CountryCodeResult cr in countries)
			{
				NodeViewModel nvm = new NodeViewModel(cr.countryCode) { ID = ID, Tag = cr.countryCode };
				nvm.Populate = nvm.PopulateVenues;
				Add(nvm);
			}
		}
		public void PopulateVenues()
		{
			List<VenueResult> venues = Betfair.GetVenues(ID, Tag as String).OrderBy(o => o.venue).ToList();
			foreach (VenueResult v in venues)
			{
				NodeViewModel nvm = new NodeViewModel(v.venue) { ID = this.ID, Tag = v.venue };
				nvm.Populate = nvm.PopulateEvents;
				Add(nvm);
			}
		}
		public void PopulateMarkets()
		{
			List<Market> markets = Betfair.GetMarkets(ID).OrderBy(o => o.marketStartTime).ToList();
			foreach (Market m in markets)
			{
				NodeViewModel nvm = new NodeViewModel(m.ToString()) { Tag = m };
				//nvm.Populate = nvm.PopulateMarketBook;
				Add(nvm);
			}
		}
		public override string ToString()
		{
			return String.Format("{0}", Name);
		}
	}
}
