using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BetfairAPI;
using System.Text;
using System.Threading.Tasks;

namespace SpreadTrader
{
	public class NodeViewModel : ViewModelBase
	{
		bool _IsExpanded;
		public bool IsExpanded	{	get { return _IsExpanded; }	set	{_IsExpanded = value; Console.WriteLine(this.Name + (IsExpanded ? @" Expanded" : @" Collapsed"));	}		}
		public delegate void CallBackDelegate();
		public CallBackDelegate CallBack = null;
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
}
