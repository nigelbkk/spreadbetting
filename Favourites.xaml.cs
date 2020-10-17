using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class Favourites : Window
	{
		Properties.Settings props = Properties.Settings.Default;
		public ObservableCollection<EventType> AllEventTypes { get; set; }
		public Favourites(List<EventTypeResult> eventTypes)
		{
			AllEventTypes = new ObservableCollection<EventType>();
			foreach (EventTypeResult er in eventTypes)
			{
				AllEventTypes.Add(er.eventType);
			}
			InitializeComponent();
			String[] ids = props.Favourites.Split(',');
			foreach(EventType e in AllEventTypes)
			{
				e.IsChecked = ids.Contains(e.id.ToString());
			}
		}
		public void Save()
		{
			props.Favourites = "";
			foreach (EventType e in AllEventTypes)
			{
				if (e.IsChecked)
				{
					props.Favourites += String.Format("{0},", e.id);
				}
			}
			props.Save();
		}
		public bool IsFavourite(Int32 id)
		{
			String[] ids = props.Favourites.Split(',');
			return ids.Contains(id.ToString());
		}
	}
}
