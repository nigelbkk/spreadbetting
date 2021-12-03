using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace SpreadTrader
{
	public class NodeViewModel : ViewModelBase
	{
		public MarketSelectionDelegate OnMarketSelected;
		public String FullName { get; set; }
		public Market Market { get; set; }
		public String MarketID { get; set; }
		public String MarketName { get; set; }
		private double _TotalMatched { get; set; }
		private double _BackBook { get; set; }
		private double _LayBook { get; set; }
		public double TotalMatched { get { return _TotalMatched; } set { _TotalMatched = value; OnPropertyChanged(""); } }
		public double BackBook { get { return _BackBook; } set { _BackBook = value; OnPropertyChanged(""); } }
		public double LayBook { get { return _LayBook; } set { _LayBook = value; OnPropertyChanged(""); } }
		public bool InPlay { get; set; }
		public marketStatusEnum Status { get; set; }
		private Int32 _UpdateRate { get; set; }
		public double Commission { get; set; }
		private double _TurnaroundTime { get; set; }
		public Int32 UpdateRate { get { return _UpdateRate; } set { _UpdateRate = value; OnPropertyChanged("UpdateRate"); } }
		public double TurnaroundTime { get { return Math.Round(_TurnaroundTime, 5); } set { _TurnaroundTime = value; OnPropertyChanged("TurnaroundTime"); } }
		public SolidColorBrush TimeToGoColor { get { return (Market.description.marketTime - DateTime.UtcNow).TotalSeconds > 0 ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Red; } }
		public String TimeToGo { get { return String.Format("{0}{1}", (Market.description.marketTime - DateTime.UtcNow).TotalSeconds > 0 ? "" : "-", (Market.description.marketTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss")); } }
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
				if (value && !_isSelected)
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
			Market.marketId = "1.175371349";

			OnItemSelected();
		}
		public NodeViewModel(String name)
		{
			Name = name;
			Nodes = new ObservableCollection<NodeViewModel>();
		}
		public List<LiveRunner> LiveRunners = null;// new List<LiveRunner>();

		public List<LiveRunner> GetLiveRunners()
		{
			List<LiveRunner> Runners = new List<LiveRunner>();
			if (Market != null)
			{
				List<MarketProfitAndLoss> pl = Betfair.listMarketProfitAndLoss(MarketID);
				Market.MarketBook = Betfair.GetMarketBook(Market);
				TotalMatched = Market.MarketBook.totalMatched;
				Status = Market.MarketBook.status;
				if (Market.MarketBook.Runners.Count > 0) foreach (Runner r in Market.MarketBook.Runners)
					{
						if (pl.Count > 0)
						{
							if (pl[0].profitAndLosses.Count > 0) foreach (var p in pl[0].profitAndLosses)
								{
									if (p.selectionId == r.selectionId)
									{
										r.ifWin = p.ifWin;
									}
								}
						}
						Runners.Add(new LiveRunner(r));
					}
				if (Parent != null)
				{
					FullName = String.Format("{0} - {1}", Parent.Name, Name);
					MarketName = Parent.Name;
					MarketID = Market.marketId;
				}
			}
			LiveRunners = Runners;
			CalculateLevelProfit();
			return Runners;
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
				if (OnMarketSelected != null)
				{
					OnMarketSelected(this);
				}
			}
		}
		private static Tuple<Double, Double> LevelProfit(LiveRunner runner1, LiveRunner runner2)
		{
			Double G3 = runner1.ifWin;
			Double J3 = runner2.ifWin;

			Double G5 = G3 > 0 ? runner1.LayValues[0].price : runner1.BackValues[0].price;
			Double J5 = J3 > 0 ? runner2.LayValues[0].price : runner2.BackValues[0].price;

			Double D8 = (G3 - J3) / G5;
			Double J8 = (G3 - J3) / J5;

			Double G10 = G3 - (D8 * (G5 - 1));
			Double G13 = (G3 - J3) / J5;

			Double G18 = G3 - (D8 * (G5 - 1));
			Double J18 = J3 + (J8 * (J5 - 1));

			return new Tuple<double, double>(Math.Round(G18, 2), Math.Round(J18, 2));
		}
		private static Tuple<Double, Double, Double> LevelProfit(LiveRunner runner1, LiveRunner runner2, LiveRunner draw)
		{
			Double D5 = runner1.ifWin;
			Double E5 = runner2.ifWin;
			Double F5 = draw.ifWin;

			Double D8 = D5 > 0 ? runner1.LayValues[0].price : runner1.BackValues[0].price;
			Double E8 = E5 > 0 ? runner2.LayValues[0].price : runner2.BackValues[0].price;
			Double F8 = F5 > 0 ? draw.LayValues[0].price : draw.BackValues[0].price;

			Double F11 = (F5 - D5) / D8;
			Double F12 = (F5 - E5) / E8;

			Double D15 = F11 > 0 ? F11 * (D8 - 1) + D5 : F11 * (D8 - 1) + D5;
			Double E15 = F11 > 0 ? E5 - F11 : -F11 + E5;
			Double F15 = F11 > 0 ? -F11 + F5 : -F11 + F5;

			Double D16 = F12 > 0 ? D15 - F12 : -F12 + D15;
			Double E16 = F12 > 0 ? F12 * (E8 - 1) + E15 : F12 * (E8 - 1) + E15;
			Double F16 = F12 > 0 ? F15 - F12 : -F12 + F15;

			return new Tuple<double, double, double>(D16, E16, F16);
		}
		public void CalculateLevelProfit()
		{
			if (LiveRunners.Count > 1)
			{
				List<LiveRunner> runners = new List<LiveRunner>();
				if (LiveRunners.Count > 0) foreach (LiveRunner lr in LiveRunners)
					{
						if (lr.ifWin != 0)
							runners.Add(lr);
					}
				if (LiveRunners.Count == 2)
				{
					Tuple<double, double> s2 = LevelProfit(LiveRunners[0], LiveRunners[1]);
					LiveRunners[0].LevelProfit = s2.Item1;
					LiveRunners[1].LevelProfit = s2.Item2;
				}
				else if (LiveRunners.Count == 3)
				{
					Tuple<double, double, double> s2 = LevelProfit(LiveRunners[0], LiveRunners[1], LiveRunners[2]);
					LiveRunners[0].LevelProfit = s2.Item1;
					LiveRunners[1].LevelProfit = s2.Item2;
					LiveRunners[2].LevelProfit = s2.Item3;
				}
			}
		}
		public String GetRunnerName(Int64 SelectionID)
		{
			if (LiveRunners != null)
			{
				if (LiveRunners.Count > 0) foreach (LiveRunner r in LiveRunners)
					{
						if (r.SelectionId == SelectionID)
						{
							return r.Name;
						}
					}
			}
			return SelectionID.ToString();
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
			node.OnMarketSelected = OnMarketSelected;
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
				NodeViewModel nvm = new NodeViewModel(String.Format("{0:HH:mm} {1}", m.description.marketTime.AddHours(props.TimeOffset), m.marketName)) { Market = m };
				nvm.Commission = m.description.marketBaseRate;
				Add(nvm);
			}
		}
		public override string ToString()
		{
			return String.Format("{0}", FullName);
		}
	}
}
