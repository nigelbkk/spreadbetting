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
                if (ifWin > 0.00M)
                    return Brushes.Green;
                if (ifWin < 0.00M)
                    return Brushes.Red;

                return Brushes.DarkGray;
            }
        }
        public String BackStake2 { get { return BackStake.ToString(); } set { BackStake = Convert.ToDecimal(value); NotifyPropertyChanged(""); } }
        public String LayStake2 { get { return LayStake.ToString(); } set { LayStake = Convert.ToDecimal(value); NotifyPropertyChanged(""); } }
        public Decimal BackStake { get; set; }
        public Decimal LayStake { get; set; }
        public Decimal Profit { get { return 1.2M; } }
        public Decimal LevelProfit { get { return 1.3M; } }
        public double LastPrice { get; set; }
        public Decimal actualSP { get { return Convert.ToDecimal(_prices[0][6].price); } }
        public Decimal ifWin { get { return Convert.ToDecimal(_prices[0][6].size); } }
        public List<PriceSize[]> _prices = new List<PriceSize[]>();
        public PriceSize[] prices { get { return _prices[0]; } }
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
            _prices.Clear();
            _prices.Add(new PriceSize[7]);
            _prices.Add(new PriceSize[7]);
            for (int i = 0; i < 7; i++)
            {
                _prices[0][i] = new PriceSize();
                _prices[1][i] = new PriceSize();
            }
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
            int i = 0;
            foreach (var p in r.ex.availableToBack)
            {
                _prices[0][i++] = p;
            }
            i = 3;      // bug in scbng
            foreach (var p in r.ex.availableToLay)
            {
                _prices[0][i++] = p;
            }
            //_prices[0][6].size = ngrunner.ifWin;
            _prices[0][6].price = r.sp == null ? 0 : (r.sp.nearPrice == 0 ? r.sp.actualSP : r.sp.nearPrice);
            ngrunner.sp = r.sp;
            NotifyPropertyChanged("");
        }
		public override string ToString()
		{
			return String.Format("{0}", name);
		}
    }
}
