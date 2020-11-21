using System;
using System.ComponentModel;
using System.Windows.Media;

public class PriceSize : INotifyPropertyChanged
{
	public PriceSize(double price, double size) { this.price = price; this.size = size; IsChecked = true; }
	public PriceSize() { IsChecked = true; }
	private bool _IsChecked { get; set; }
	public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; Opacity = value ? 1.0 : 0.4; NotifyPropertyChanged(""); } }
	private Double _price { get; set; }
	public Double price { get { return _price; } set { _price = value; NotifyPropertyChanged(""); } }
	private Double _size { get; set; }
	public Double size { get { return _size; } set { _size = value; NotifyPropertyChanged(""); } }
	public SolidColorBrush Color { get; set; }
	public double Opacity { get; set; }
	public override string ToString()
	{
		return String.Format("{0:0.00}:{1:0.00}", price, size);
	}
	public event PropertyChangedEventHandler PropertyChanged;
	public void NotifyPropertyChanged(String info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}
