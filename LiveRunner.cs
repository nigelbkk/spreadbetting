using System;
using System.Collections.Generic;
using System.ComponentModel;
using BetfairAPI;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Betfair.ESAClient.Cache;

namespace SpreadTrader
{
    public class LiveRunner : INotifyPropertyChanged
    {
        public Runner ngrunner { get; set; }
        private BitmapImage _colors = null;
        public BitmapImage Colors { get { return _colors; } set { _colors = value; NotifyPropertyChanged("Colors"); } }
        public String Name { get; set; }
        public Int64 SelectionId { get; set; } 
        public Brush OutComeColor
        {
            get
            {
                if (ifWin > 0.00)
                    return Brushes.Green;
                if (ifWin < 0.00)
                    return Brushes.Red;

                return Brushes.DarkGray;
            }
        }
        private bool _IsFavorite { get; set; }
        public bool IsFavorite { get { return _IsFavorite; } set { _IsFavorite = value; NotifyPropertyChanged(""); } }
        public static LiveRunner Favorite { get; set; }
        public Brush FavoritesColor
        {
            get
            {
                if (IsFavorite)
                    return Brushes.Aquamarine;

                return Brushes.Ivory;
            }
        }
        public double BackStake { get; set; }
        public double LayStake { get; set; }
        public double ifWin { get; set; } 
        public double LevelProfit { get; set; }
        public double LastPriceTraded { get; set; }
        public List<PriceSize> BackValues { get; set; }
        public ObservableCollection<PriceSize> LayValues { get; set; }
        public double BackLayRatio { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        public LiveRunner()
        {
            BackStake = Properties.Settings.Default.BackStake;
            LayStake = Properties.Settings.Default.LayStake;
            BackValues = new List<PriceSize>();
			LayValues = new ObservableCollection<PriceSize>();

			BackValues.Add(new PriceSize());
			BackValues.Add(new PriceSize());
			BackValues.Add(new PriceSize());
			LayValues.Add(new PriceSize());
			LayValues.Add(new PriceSize());
			LayValues.Add(new PriceSize());
		}
        public LiveRunner(Runner r) : this()
        {
            ngrunner = r;
            SetPrices(r);
        }
        public void SetPrices(MarketRunnerSnap r)
		{
            int i = 0;
            foreach (var ps in r.Prices.BestAvailableToBack)
            {
                BackValues[i].price = ps.Price;
                BackValues[i++].size = ps.Size;
            }
            i = 0;
            foreach (var ps in r.Prices.BestAvailableToLay)
            {
                LayValues[i].price = ps.Price;
                LayValues[i++].size = ps.Size;
            }
            //            Name = String.Format("Not available");
            //ifWin = r.Prices.;
            LastPriceTraded = r.Prices.LastTradedPrice;

            // LevelProfit = r.Prices.
            //BackLayRatio = r.BackLayRatio;

            NotifyPropertyChanged("");
        }
        public void SetPrices(Runner r)
        {
            int i = 0;
            if (r.ex != null)
            {
                foreach (var ps in r.ex.availableToBack)
                {
                    BackValues[i].price = ps.price;
                    BackValues[i++].size = ps.size;
                }
                i = 0;
                foreach (var ps in r.ex.availableToLay)
                {
                    LayValues[i].price = ps.price;
                    LayValues[i++].size = ps.size;
                }
            }
            Name = String.Format("{0}{1}", r.Catalog.name, r.handicap == 0 ? "" : " " + r.handicap.ToString()); 
            ifWin = r.ifWin;
            LastPriceTraded = r.lastPriceTraded;
            BackLayRatio = r.BackLayRatio;
            NotifyPropertyChanged("");
        }
		public override string ToString()
		{
			return String.Format("{0}", Name);
		}
    }
}
