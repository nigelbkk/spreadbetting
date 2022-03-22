using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace SpreadTrader
{
    public partial class UpDownControl : UserControl, INotifyPropertyChanged
    {
        public double _Value { get; set; }
        public double Value { get { return _Value; } set { _Value = value; NotifyPropertyChanged(""); } }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public UpDownControl()
        {
            InitializeComponent();
            tb.Focus();
        }
        private void tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            double d = 0;

            if (Double.TryParse(tb.Text, out d))
            {
                Value = d;
            }
            NotifyPropertyChanged("");
            Debug.WriteLine(Value, "tb");
        }
        private BetfairPrices betfairPrices = new BetfairPrices();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            Debug.Write(".");
            RepeatButton b = sender as RepeatButton;
            switch (b.Name)
            {
                case "Up": Value = betfairPrices.Next(Value); break;
                case "Down": Value = betfairPrices.Previous(Value); break;
                default: return;
            }
            NotifyPropertyChanged("");
        }
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            BetfairPrices betfairPrices = new BetfairPrices();
            switch (e.Key)
            {
                case Key.Up:
                    Value = betfairPrices.Next(Value);
                    e.Handled = true;
                    break;
                case Key.Down:
                    Value = betfairPrices.Previous(Value);
                    e.Handled = true;
                    break;
            }
            NotifyPropertyChanged("");
        }
    }
}
