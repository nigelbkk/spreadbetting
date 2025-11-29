using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
public class PriceSize : INotifyPropertyChanged
{
    public PriceSize(double price, double size) {
        Initialise();
        this.price = price; this.size = size; IsChecked = true; 
    }
    public PriceSize() {
        IsChecked = true;
        Initialise();
    }
    private bool _IsChecked { get; set; }
    private bool _ParentChecked { get; set; }
    public bool ParentChecked { get { return _ParentChecked; } set { _ParentChecked = value; NotifyPropertyChanged(""); } }
    public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; NotifyPropertyChanged(""); } }
    private Double _price { get; set; }
    public Double price { get { return _price; } set { _price = value; NotifyPropertyChanged(""); } }
    private Double _size { get; set; }
    public Double size { get { return _size; } set { _size = value; NotifyPropertyChanged(""); } }
    public SolidColorBrush Color { get; set; }
    public List<Brush> LayColors { get; set; }
    public List<Brush> BackColors { get; set; }
    public void Flash(int id)
    {
        //BackColors[id] = Brushes.Yellow;
     //   (Brush)Application.Current.Resources["Back0Color"]
    }
    private void Initialise()
    {
    //    BackColors = new List<Brush>
    //        {
    //            (Brush)Application.Current.Resources["Back0Color"],
    //            (Brush)Application.Current.Resources["Back1Color"],
    //            (Brush)Application.Current.Resources["Back2Color"]
    //        };
    //    LayColors = new List<Brush>
    //        {
    //            (Brush)Application.Current.Resources["Lay0Color"],
    //            (Brush)Application.Current.Resources["Lay1Color"],
    //            (Brush)Application.Current.Resources["Lay2Color"]
    //        };
    //    BackColors[0] = Brushes.Yellow;

    }
    //public Brush BackColor
    //{
    //    get
    //    {
    //        return Brushes.Yellow;
    //    }
    //}

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
