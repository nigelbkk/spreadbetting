using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;


public sealed class PriceSize : INotifyPropertyChanged
{
	private SpreadTrader.Properties.Settings props = SpreadTrader.Properties.Settings.Default;
	static List<SolidColorBrush> BackgroundColors = new List<SolidColorBrush>
	{
		(SolidColorBrush) Application.Current.Resources["Back0Color"],
		(SolidColorBrush) Application.Current.Resources["Back1Color"],
		(SolidColorBrush) Application.Current.Resources["Back2Color"],
		(SolidColorBrush) Application.Current.Resources["Lay0Color"],
		(SolidColorBrush) Application.Current.Resources["Lay1Color"],
		(SolidColorBrush) Application.Current.Resources["Lay2Color"]
	};

	public PriceSize(double price, double size) {
		Update(price, size);
		IsChecked = true;
	}
	private Int32 Index;
	public PriceSize(Int32 index) {
		this.Index = index;
		_cellBackground = CellDefaultColor = BackgroundColors[index];
		IsChecked = true;
	}
	private bool _IsChecked { get; set; }
	private bool _ParentChecked { get; set; }
	public bool ParentChecked { get { return _ParentChecked; } set {
			if (_ParentChecked == value)
				return;
			_ParentChecked = value; OnPropertyChanged();
		} }
	public bool IsChecked { get { return _IsChecked; } set { if (_IsChecked == value) return; _IsChecked = value; OnPropertyChanged(); } }

	private Double _price { get; set; }
	private Double _size { get; set; }
	private Double _traded { get; set; }
	public Double price => _price;
	public Double size => _size;
	public Double traded => _traded;

	public string PriceText
	{
		get
		{
			String format = "0";

			if (_price < 4)
				format = "0.00";
			else if (_price < 10)
				format = "0.0";

			return _price == 0 ? "" : _price.ToString(format).TrimEnd('0').TrimEnd('.');
		}
	}
	public string SizeText => _size == 0 ? "" : _size.ToString("0");

	public DateTime last_flash_time;

	private double _tradedVolume;
	public double TradedVolume
	{
		get => _tradedVolume; 
		set
		{
			if (_tradedVolume != value)
			{ 
				_tradedVolume = value;
				OnPropertyChanged();
				if (props.FlashYellow)
					Flash();
			}
		}
	}


public void Update(double newPrice, double newSize)
	{
		bool priceChanged = _price != newPrice;
		bool sizeChanged = _size != newSize;
		//bool tradedChanged = _traded != newTraded;

		if (!priceChanged && !sizeChanged)
			return;

		_price = newPrice;
		_size = newSize;

		if (priceChanged)
		{
			OnPropertyChanged(nameof(price));
			OnPropertyChanged(nameof(PriceText));
		}
		if (sizeChanged) 
		{ 
			OnPropertyChanged(nameof(size));
			OnPropertyChanged(nameof(SizeText));
		}
		//if (tradedChanged) OnPropertyChanged(nameof(TradedText));
		//if (tradedChanged)
		//	if (props.FlashYellow)
		//		Flash();
	}

	private CancellationTokenSource _flashCts;

	private async void Flash()
	{
		_flashCts?.Cancel();
		var cts = _flashCts = new CancellationTokenSource();

		CellBackgroundColor = Brushes.Yellow;

		try
		{
			await Task.Delay(FLASH_DURATION_MS, cts.Token);
			CellBackgroundColor = CellDefaultColor;
		}
		catch (TaskCanceledException) { }
	}

	private const int FLASH_DURATION_MS = 200;
	public SolidColorBrush CellDefaultColor { get; set; }

	private SolidColorBrush _cellBackground;
	public SolidColorBrush CellBackgroundColor
	{
		get => _cellBackground;
		private set
		{
			if (_cellBackground != value)
			{
				_cellBackground = value;
				OnPropertyChanged();
			}
		}
	}
	public override string ToString()
    {
        return String.Format("{0:0.00}:{1:0.00}", price, size);
    }
    public event PropertyChangedEventHandler PropertyChanged;
	void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
