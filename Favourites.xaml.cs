using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class Favourites : Window
	{
		Properties.Settings props = Properties.Settings.Default;
		public ObservableCollection<EventType> AllEventTypes { get; set; }
		public Favourites()
		{
			AllEventTypes = new ObservableCollection<EventType>();
			InitializeComponent();
			AllEventTypes.Add(new EventType() { name = "American Football", id = 1});
			AllEventTypes.Add(new EventType() { name = "Horses", id = 2 });
			AllEventTypes.Add(new EventType() { name = "Dogs", id = 3 });

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
	}
}
