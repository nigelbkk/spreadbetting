using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BetfairAPI;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;

namespace SpreadTrader
{
    public class LiveRunner : INotifyPropertyChanged
    {
        public Runner ngrunner { get; set; }
        private BitmapImage _colors = null;
        public BitmapImage Colors { get { return _colors; } set { _colors = value; NotifyPropertyChanged("Colors"); } }
        public String name { get { return String.Format("{0} {1}",  ngrunner.Catalog.name, ngrunner.handicap == 0 ? "" : ngrunner.handicap.ToString()); } }
        public Int64 selectionId { get { return ngrunner.selectionId; } }
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
        public double ifWin { get { return 1.2; } }
        public double LevelProfit { get { return 1.3; } }
        public double LastPrice { get; set; }
        public List<PriceSize> BackPrices { get; set; }
        public List<PriceSize> LayPrices { get; set; }
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
        }
        public LiveRunner(Runner r) : this()
        {
            ngrunner = r;
            SetPrices(ngrunner);
        }
        public void SetPrices(Runner r)
        {
            BackPrices = r.ex.availableToBack;
            LayPrices = r.ex.availableToLay;
            NotifyPropertyChanged("");
        }
		public override string ToString()
		{
			return String.Format("{0}", name);
		}
    }
}
