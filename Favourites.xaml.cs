using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class Favourites : Window
    {
        Properties.Settings props = Properties.Settings.Default;
        public ObservableCollection<EventType> AllEventTypes { get; set; }
        public Favourites(Visual visual, Button b, List<EventTypeResult> eventTypes)
        {
            Point coords = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(b.ActualWidth, b.ActualHeight)));
            Top = coords.Y;
            Left = coords.X;

            AllEventTypes = new ObservableCollection<EventType>();
            if (eventTypes.Count > 0) foreach (EventTypeResult er in eventTypes)
                {
                    AllEventTypes.Add(er.eventType);
                }
            InitializeComponent();
            String[] ids = props.Favourites.Split(',');
            if (AllEventTypes.Count > 0) foreach (EventType e in AllEventTypes)
                {
                    e.IsChecked = ids.Contains(e.id.ToString());
                }
        }
        public void Save()
        {
            props.Favourites = "";
            if (AllEventTypes.Count > 0) foreach (EventType e in AllEventTypes)
                {
                    if (e.IsChecked)
                    {
                        props.Favourites += String.Format("{0},", e.id);
                    }
                }
            props.Save();
        }
        static public bool IsFavourite(Int32 id)
        {
            Properties.Settings props = Properties.Settings.Default;
            String[] ids = props.Favourites.Split(',');
            return ids.Count() == 0 || ids.Contains(id.ToString());
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Save();
        }
    }
}
