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
		private bool _IsExpanded;
		public bool IsExpanded { get { return _IsExpanded; } set { _IsExpanded = value; OnItemExpanding(); } }
		public delegate void CallBackDelegate();
		public CallBackDelegate Populate = null;
		private void OnItemExpanding()
		{
			Console.WriteLine(this.Name + (IsExpanded ? @" Expanded" : @" Collapsed"));
			if (IsExpanded && Populate != null)
			{
				Populate();
			}
		}
		public Object Tag { get; set; }
		public NodeViewModel Parent { get; set; }
		public string Country { get; set; }
		public Int32 Id { get; set; }
		public string Name { get; set; }
		public ObservableCollection<NodeViewModel> Children { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
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
			NotifyPropertyChanged("");
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
		public void PopulateEvents()
		{
			List<Event> events = EventsModel.Betfair.GetEvents(Id, Country).OrderBy(o => o.details.name).ToList();
			foreach (Event ev in events)
			{
				NodeViewModel nvm = new NodeViewModel(ev.details.name) { Id = this.Id, Country = this.Country };
				nvm.Populate = null;// nvm.PopulateEvents;
				Add(nvm);
			}
		}
		public void PopulateCountries()
		{
			List<CountryCodeResult> countries = EventsModel.Betfair.GetCountries(7);
			foreach (CountryCodeResult cr in countries)
			{
				NodeViewModel nvm = new NodeViewModel(cr.countryCode) { Id = 7, Country = cr.countryCode };
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
				nvm.Populate = null;// nvm.PopulateEvents;
				Add(nvm);
			}
		}
	}
}
