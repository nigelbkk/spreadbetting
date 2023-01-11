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
        private String SValue { get; set; }
        public double _Value { get; set; }
        public String Value {
            get {
                return SValue; } 
            set { SValue = value;
                NotifyPropertyChanged("");             
            } 
        }
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
            String cs = tb.Text;

            if (cs.EndsWith("."))
                cs += "0";

            if (Double.TryParse(cs, out d))
            {
                _Value = d;
            }
        }
        private BetfairPrices betfairPrices = new BetfairPrices();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb.SelectAll();
        }
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            RepeatButton b = sender as RepeatButton;
            switch (b.Name)
            {
                case "Up": _Value = betfairPrices.Next(_Value); break;
                case "Down": _Value = betfairPrices.Previous(_Value); break;
                default: return;
            }
        }
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemPeriod:
                    String cs = tb.Text;

                    if (!cs.Contains("."))
                    {
                        cs += ".0";
                    }

                    Int32 idx = cs.IndexOf('.');
                    if (idx < 0)
                    {
                        idx = cs.Length;
                    }
                    tb.Text = cs;
                    _Value = Convert.ToDouble(cs);
                    tb.CaretIndex = idx + 1;

                    e.Handled = true;
                    break;
                case Key.Up:
                    _Value = betfairPrices.Next(_Value);
                    e.Handled = true;
                    break;
                case Key.Down:
                    _Value = betfairPrices.Previous(_Value);
                    e.Handled = true;
                    break;
            }
            Debug.WriteLine(tb.CaretIndex);
        }
    }
}
