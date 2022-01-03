using Betfair.ESAClient.Cache;
using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpreadTrader
{
	public class LiveRunner : INotifyPropertyChanged
	{
		public Runner ngrunner { get; set; }
		private BitmapImage _colors = null;
		public BitmapImage Colors { get { return _colors; } set { _colors = value; NotifyPropertyChanged("Colors"); } }
		public String Name { get; set; }
		public String LevelSide { get; set; }
		public Double LevelOdds { get; set; }
		public Double LevelStake { get; set; }
		private double _Width { get; set; }
		public double Width { get { return _Width; } set { _Width = value; NotifyPropertyChanged("Width"); } }
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
		public double ifWin { get { return _ifWin; } set { _ifWin = value; NotifyPropertyChanged(""); } }
		private double _LevelProfit { get; set; }
		public double LevelProfit
		{
			get { return _LevelProfit; }
			set
			{
				_LevelProfit = value;
				NotifyPropertyChanged("");
			}
		}
		//public double LevelProfit { get; set; }
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
			_Width = 160;
		}
		public LiveRunner(Runner r) : this()
		{
			ngrunner = r;
			SelectionId = r.selectionId;
			SetPrices(r);
		}
		public void SetPrices(MarketRunnerSnap r)
		{
			int i = 0;
			if (r.Prices.BestDisplayAvailableToBack.Count > 0) foreach (var ps in r.Prices.BestDisplayAvailableToBack)
				{
					BackValues[i].price = ps.Price;
					BackValues[i++].size = ps.Size;
				}
			i = 0;
			if (r.Prices.BestDisplayAvailableToLay.Count > 0) foreach (var ps in r.Prices.BestDisplayAvailableToLay)
				{
					LayValues[i].price = ps.Price;
					LayValues[i++].size = ps.Size;
				}
			LastPriceTraded = r.Prices.LastTradedPrice;
			NotifyPropertyChanged("");
		}
		public void SetPrices(Runner r)
		{
			int i = 0;
			if (r.ex != null)
			{
				for (int j = 0; j < 3; j++)
				{
					BackValues[j] = new PriceSize();
					LayValues[j] = new PriceSize();
				}
				if (r.ex.availableToBack.Count > 0) foreach (var ps in r.ex.availableToBack)
					{
						BackValues[i].price = ps.price;
						BackValues[i++].size = ps.size;
					}
				i = 0;
				if (r.ex.availableToLay.Count > 0) foreach (var ps in r.ex.availableToLay)
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
			return String.Format("{0}, {1}", Name, SelectionId);
		}
	}
}
