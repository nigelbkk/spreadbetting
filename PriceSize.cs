using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Security.Cryptography.Certificates;


public sealed class PriceSize : INotifyPropertyChanged
{
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
    public PriceSize(Int32 index) {
        this.Index = index;
		CellDefaultColor = BackgroundColors[index];
		IsChecked = true; 
    }
    private Int32 Index { get; set; }
	private bool _IsChecked { get; set; }
    private bool _ParentChecked { get; set; }
    public bool ParentChecked { get { return _ParentChecked; } set { _ParentChecked = value; OnPropertyChanged(""); } }
    public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; OnPropertyChanged(""); } }

	private Double _price { get; set; }
	private Double _size { get;  set; }
	public Double price => _price;
	public Double size => _size;

	public DateTime last_flash_time;
	public void Update(double newPrice, double newSize)
	{
		bool priceChanged = _price != newPrice;
		bool sizeChanged = _size != newSize;

		if (!priceChanged && !sizeChanged)
			return;

		_price = newPrice;
		_size = newSize;

		if (priceChanged) 
			OnPropertyChanged(nameof(price));
		if (sizeChanged) 
			OnPropertyChanged(nameof(size));
	}
    private DateTime? _lastFlashTime;
    public DateTime? lastFlashTime {get { return _lastFlashTime; } 
		set{ 
			_lastFlashTime = value;
			OnPropertyChanged(nameof(CellBackgroundColor));
		} }
    private const int FLASH_DURATION_MS = 200;
    public SolidColorBrush CellDefaultColor { get; set; }
	public SolidColorBrush CellBackgroundColor {
		get {
			if (_lastFlashTime.HasValue)
			{
				var elapsed = (DateTime.UtcNow - _lastFlashTime.Value).TotalMilliseconds;
				if (elapsed != 0 && elapsed < FLASH_DURATION_MS)
				{
                    return Brushes.Yellow;
				}
				else
				{
					_lastFlashTime = null; // Expired
					return CellDefaultColor;
				}
			}
			return CellDefaultColor;
		}
	}

public override string ToString()
    {
        return String.Format("{0:0.00}:{1:0.00}", price, size);
    }
    public event PropertyChangedEventHandler PropertyChanged;
	private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
