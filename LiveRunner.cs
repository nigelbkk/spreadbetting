using System;
using System.Collections.Generic;
using System.ComponentModel;
using BetfairAPI;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace SpreadTrader
{
    public class LiveRunner : INotifyPropertyChanged
    {
        public Runner ngrunner { get; set; }
        private BitmapImage _colors = null;
        public BitmapImage Colors { get { return _colors; } set { _colors = value; NotifyPropertyChanged("Colors"); } }
        public String Name { get { return String.Format("{0} {1}",  ngrunner.Catalog.name, ngrunner.handicap == 0 ? "" : ngrunner.handicap.ToString()); } }
        public Int64 SelectionId { get { return ngrunner.selectionId; } }
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
        public double BackStake { get; set; }
        public double LayStake { get; set; }
        public double ifWin { get { return ngrunner.ifWin; } }
        public double LevelProfit { get; set; }
        public double LastPriceTraded { get { return ngrunner.lastPriceTraded;  } }
        public List<PriceSize> BackValues { get; set; }
        public ObservableCollection<PriceSize> LayValues { get; set; }
        public double BackLayRatio
        {
            get
            {
                if (ngrunner.ex.availableToLay.Count > 0 && ngrunner.ex.availableToBack.Count > 0)
                {
                    double BackLayRatio = Math.Abs(ngrunner.ex.availableToBack[0].price - ngrunner.ex.availableToLay[0].price);

                    BackLayRatio /= ngrunner.ex.availableToBack[0].price;
                    return BackLayRatio *= 100;
                }
                return 0;
            }
        }
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
            SetPrices(ngrunner);
        }
        public void SetPrices(Runner r)
        {
            int i = 0;
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
            NotifyPropertyChanged("");
        }
		public override string ToString()
		{
			return String.Format("{0}", Name);
		}
    }
}
