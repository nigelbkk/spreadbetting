using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
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

    public PriceSize(double price, double size)
    {
        Index = 0;
        CellBackgroundColor = BackgroundColors[Index];
        this.price = price; this.size = size; IsChecked = true;
    }
    public PriceSize(int index, double price, double size)
    {
        Index = index;
        CellBackgroundColor = BackgroundColors[index];
        this.price = price; this.size = size; IsChecked = true;
    }
	public PriceSize(int index)
	{
		IsChecked = true;
		Index = index;
		CellBackgroundColor = BackgroundColors[index];
	}
	public PriceSize()
	{
		CellBackgroundColor = BackgroundColors[0];
	}
	private int Index { get; set; }
    private bool _IsChecked { get; set; }
    private bool _ParentChecked { get; set; }
    public bool ParentChecked { get { return _ParentChecked; } set { _ParentChecked = value; NotifyPropertyChanged(""); } }
    public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; NotifyPropertyChanged(""); } }
    private Double _price { get; set; }
    public Double price { get { return _price; } set { _price = value; NotifyPropertyChanged(""); } }
    private Double _size { get; set; }
    public Double size { get { return _size; } set { _size = value; NotifyPropertyChanged(""); } }
    public SolidColorBrush Color { get; set; }
    //private object _timer;
    private Brush _CellBackgroundColor { get; set; }
    public Brush CellBackgroundColor {
        get { return _CellBackgroundColor; }
        set {
            _CellBackgroundColor = value;
            //var timer = new System.Timers.Timer(1000);

            //// assign it to the field so it never gets GC’ed
            //_timer = timer;

            //((System.Timers.Timer)_timer).Elapsed += (_, __) => 
            //{
            //    CellBackgroundColor = BackgroundColors[Index];
            //};
            //((System.Timers.Timer)_timer).Start();
        }
    }
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
