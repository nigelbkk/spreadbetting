using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;


public class PriceSize : INotifyPropertyChanged
{
	static List<Brush> BackgroundColors = new List<Brush>
	{
		(Brush) Application.Current.Resources["Back0Color"],
		(Brush) Application.Current.Resources["Back1Color"],
		(Brush) Application.Current.Resources["Back2Color"],
		(Brush) Application.Current.Resources["Lay0Color"],
		(Brush) Application.Current.Resources["Lay1Color"],
		(Brush) Application.Current.Resources["Lay2Color"]
	};

	public PriceSize(double price, double size) { this.price = price; this.size = size; IsChecked = true; }
    public PriceSize() { IsChecked = true; }
    private bool _IsChecked { get; set; }
    private bool _ParentChecked { get; set; }
    public bool ParentChecked { get { return _ParentChecked; } set { _ParentChecked = value; NotifyPropertyChanged(""); } }
    public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; NotifyPropertyChanged(""); } }
	public Double price { get; set; }
	//public Double price { get { return _price; } set { _price = value; NotifyPropertyChanged(""); } }
	public Double size { get; set; }
    //public Double size { get { return _size; } set { _size = value; NotifyPropertyChanged(""); } }
    public SolidColorBrush Color { get; set; }
	public Brush CellBackgroundColor { get; set; }

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
