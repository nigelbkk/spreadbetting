using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BetfairAPI;

namespace SpreadTrader
{
	public class NodeViewModel : ViewModelBase
	{
		public NodeSelectionDelegate NodeCallback = null;
		public String FullName { get; set; }
		public Market Market { get; set; }
		public String MarketID { get; set; }
		public String MarketName { get; set; }
		private Int32 _UpdateRate { get; set; }
		public Int32 UpdateRate { get { return _UpdateRate; } set { _UpdateRate = value; OnPropertyChanged("UpdateRate"); } }
		List<EventTypeResult> EventTypes { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		public static BetfairAPI.BetfairAPI Betfair { get; set; }
		private bool _isSelected;
		private bool _IsExpanded;
		public bool IsExpanded { get { return _IsExpanded; } set { _IsExpanded = value; OnItemExpanding(); } }
		public bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				if (value)// && !_isSelected) 
					OnItemSelected();
				_isSelected = value;
			}
		}
		public delegate void CallBackDelegate();
		public CallBackDelegate Populate = null;
		public NodeViewModel Parent { get; set; }
		public Int32 ID { get; set; }
		public string Name { get; set; }
		public string Tag { get; set; }
		public ObservableCollection<NodeViewModel> Nodes { get; set; }
		public NodeViewModel(BetfairAPI.BetfairAPI Betfair)
		{
			NodeViewModel.Betfair = Betfair;
			Nodes = new ObservableCollection<NodeViewModel>();
			Market = new Market();
			Market.marketId = "1.175065466";

			OnItemSelected();
		}
		public NodeViewModel(String name)
		{
			Name = name;
			Nodes = new ObservableCollection<NodeViewModel>();
		}
		public List<LiveRunner> GetLiveRunners()
		{
			List<LiveRunner> LiveRunners = new List<LiveRunner>();
			if (Market != null)
			{
				try
				{
					Market.MarketBook = Betfair.GetMarketBook(Market);
					foreach (Runner r in Market.MarketBook.Runners)
					{
						if (r.removalDate == new DateTime())
						{
							LiveRunner rl = new LiveRunner(r);
							LiveRunners.Add(rl);
							if (r.ex.availableToBack.Count > 0) { }
						}
					}
					if (Parent != null)
					{
						FullName = String.Format("{0} - {1}", Parent.Name, Name);
						MarketName = Parent.Name;
						MarketID = Market.marketId;
					}
				}
				catch (Exception xe)
				{
					Debug.WriteLine(xe.Message);
				}
			}
			return LiveRunners;
		}
		private void OnItemSelected()
		{
			if (Market != null)
			{
				if (Parent != null)
				{
					FullName = String.Format("{0} - {1}", Parent.Name, Name);
					MarketName = Parent.Name;
					MarketID = Market.marketId;
				}
				if (NodeCallback != null)
				{
					NodeCallback(this);
				}
			}
		}
		private void OnItemExpanding()
		{
			if (IsExpanded && Populate != null && Nodes.Count <= 1)
			{
				Nodes.Clear();
				Populate();
			}
		}
		public void Clear()
		{
			Nodes.Clear();
		}
		public void Add(NodeViewModel node, bool leaf = false)
		{
			node.Parent = this;
			node.NodeCallback = NodeCallback;
			Nodes.Add(node);
			if (node.Populate != null) node.Nodes.Add(new NodeViewModel("x"));
		}
		public void PopulateEventTypes()
		{
			EventTypes = Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList();
			foreach (EventTypeResult ev in EventTypes)
			{
				if (Favourites.IsFavourite(ev.eventType.id))
				{
					NodeViewModel nvm = new NodeViewModel(ev.eventType.name) { ID = ev.eventType.id };
					nvm.Populate = nvm.PopulateCompetitionsOrCountries;
					Add(nvm);
				}
			}
			Populate = PopulateCompetitionsOrCountries;
		}
		public void PopulateEvents()
		{
			List<Event> events = Betfair.GetEvents(ID, Tag as String).OrderBy(o => o.details.name).ToList();
			foreach (Event ev in events)
			{
				NodeViewModel nvm = new NodeViewModel(ev.details.name) { ID = ev.details.id, Tag = this.ID.ToString() };
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
			Int32 event_type = Convert.ToInt32(Tag);
			List<Market> markets = Betfair.GetMarkets(event_type, ID).OrderBy(o => o.marketStartTime).ToList();
			foreach (Market m in markets)
			{
				NodeViewModel nvm = new NodeViewModel(m.ToString()) { Market = m};
				//nvm.Populate = nvm.PopulateMarketBook;
				Add(nvm);
			}
		}
		public override string ToString()
		{
			return String.Format("{0}", FullName);
		}
	}
}
