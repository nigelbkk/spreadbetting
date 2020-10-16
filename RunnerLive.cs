using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using APING;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SpreadTrader
{
    public class RunnerLive : INotifyPropertyChanged
    {
        public static Int32 tabindex { get; set; }
        public String SPColour
        {
            get
            {
                switch (tabindex)
                {
                    case 0: return Properties.Settings.Default.BetfairSPColour;
                    case 2: return Properties.Settings.Default.MultiSPColour;
                }
                return "magenta";
            }
        }
        public Runner ngrunner { get; set; }
        private BitmapImage _colors = null;
        public BitmapImage Colors { get { return _colors; } set { _colors = value; NotifyPropertyChanged("Colors"); } }
        public String name { get { return ngrunner.Catalog.name; } }
        public int selectionId { get { return ngrunner.selectionId; } }
        public String saddle_number { get { return ngrunner.Catalog.MetaData("CLOTH_NUMBER"); } }
        public int barrier { get { return Convert.ToInt32(ngrunner.Catalog.MetaData("STALL_DRAW")); } }
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
        public Double actualSP { get { return _prices[0][6].price; } }
        public Double ifWin { get { return tabindex == 2 ? _prices[0][6].size + _prices[1][6].size : _prices[tabindex][6].size; } }
        public List<PriceSize[]> _prices = new List<PriceSize[]>();
        public PriceSize[] prices { get { return tabindex == 2 ? _prices[0] : _prices[tabindex]; } }

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
        private RunnerLive()
        {
            _prices.Clear();
            _prices.Add(new PriceSize[7]);
            _prices.Add(new PriceSize[7]);
            for (int i = 0; i < 7; i++)
            {
                _prices[0][i] = new PriceSize();
                _prices[1][i] = new PriceSize();
            }
        }
        public RunnerLive(Runner r) : this()
        {
            ngrunner = r;
            SetPrices(ngrunner);
        }
        public void SetPrices(Runner r)
        {
            int i = 0;
            //          for (i = 0; i < 7; i++)
            //          {
            /////              _prices[0][i] = new PriceSize();
            //          }
            i = 0;
            foreach (var p in r.ex.availableToBack)
            {
                _prices[0][i++] = p;
            }
            foreach (var p in r.ex.availableToLay)
            {
                _prices[0][i++] = p;
            }
            //_prices[0][6].size = ngrunner.ifWin;
            _prices[0][6].price = r.sp == null ? 0 : (r.sp.nearPrice == 0 ? r.sp.actualSP : r.sp.nearPrice);
            ngrunner.sp = r.sp;
            NotifyPropertyChanged("");
        }
    }
}
