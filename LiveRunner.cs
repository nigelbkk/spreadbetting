using BetfairAPI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpreadTrader
{
    public class LiveRunner : INotifyPropertyChanged
    {
        public Runner ngrunner { get; set; }
        private BitmapImage _colors = null;
        public BitmapImage Colors { get { return _colors; } set { _colors = value; OnPropertyChanged("Colors"); } }
		public String Name { get; set; }
		public Int32 Index { get; set; }
        private double _Width { get; set; }
        public double Width { get { return _Width; } set { _Width = value; OnPropertyChanged("Width"); } }
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
        public bool IsFavorite { get { return _IsFavorite; } set { _IsFavorite = value; OnPropertyChanged(""); } }
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
        public Brush ProfitColor
        {
            get
            {
                if (ifWin >= 0)
                    return Brushes.Black;

                return Brushes.Red;
            }
        }
        public Brush LevelProfitColor
        {
            get
            {
                if (LevelProfit >= 0)
                    return Brushes.Black;

                return Brushes.Red;
            }
        }
        public double BackStake { get; set; }
        public double LayStake { get; set; }
        private double _ifWin { get; set; }
        public double ifWin { get { return _ifWin; } set { _ifWin = value; OnPropertyChanged("ifWin"); } }
        private Double _LevelStake = 0;
        public Double LevelStake { get { return _LevelStake; } set { _LevelStake = value; OnPropertyChanged("LevelStake"); } }
        public sideEnum LevelSide { get; set; }
        
        private double _LevelProfit = 0;
        public double LevelProfit
        {
            get { return _LevelProfit; }
            set
            {
                _LevelProfit = value;
				OnPropertyChanged("LevelProfit");
            }
        }
        private double _LastPriceTraded;
		public double LastPriceTraded
        {
            get => _LastPriceTraded;
            set
            {
                if (_LastPriceTraded != value)
                {
                    _LastPriceTraded = value;
                    OnPropertyChanged(nameof(LastPriceTraded));
                }
            }
        }
        public ObservableCollection<PriceSize> BackValues { get; set; }
        public ObservableCollection<PriceSize> LayValues { get; set; }
        public double BackLayRatio { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
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
            BackValues = new ObservableCollection<PriceSize>();
            LayValues = new ObservableCollection<PriceSize>();

            BackValues.Add(new PriceSize(0));
            BackValues.Add(new PriceSize(1));
            BackValues.Add(new PriceSize(2));
            LayValues.Add(new PriceSize(3));
            LayValues.Add(new PriceSize(4));
            LayValues.Add(new PriceSize(5));
            _Width = 160;
        }
		public LiveRunner(Runner r) : this()
		{
			ngrunner = r;
			SelectionId = r.selectionId;
			SetPrices(r);
			OnPropertyChanged("");
		}
		public void SetPrices(Runner r)
		{
			int i = 0;
			if (r.ex != null)
			{
                if (r.ex.availableToBack.Count > 0)
                {
                    foreach (var ps in r.ex.availableToBack)
                    {
                        UiThread.Run(() => BackValues[i++].Update(ps.price, ps.size));
                    }
                }
				i = 0;
				if (r.ex.availableToLay.Count > 0)
                {
                    foreach (var ps in r.ex.availableToLay)
                    {
                        UiThread.Run(() => LayValues[i++].Update(ps.price, ps.size));
                    }
                }
			}
			Name = String.Format("{0}{1}", r.Catalog.name, r.handicap == 0 ? "" : " " + r.handicap.ToString());
			ifWin = r.ifWin;        ///NH
			LastPriceTraded = r.lastPriceTraded;
			BackLayRatio = r.BackLayRatio;
		}
		public override string ToString()
        {
            return String.Format("{0}, {1}", Name, SelectionId);
        }
    }
}
