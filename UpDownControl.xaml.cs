using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SpreadTrader
{
    public partial class UpDownControl : UserControl, INotifyPropertyChanged
    {
		private double _NumericValue;
        public double NumericValue
        {
            get => _NumericValue;
            set
            {
                if (_NumericValue != value)
                {
                    _NumericValue = value;
                    _value = $"{_NumericValue}";
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
		private String _value;
        public String Value
        {
            get => $"{_NumericValue}";
            set { }
		}
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
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
                _NumericValue = d;
                //OnPropertyChanged(nameof(Value));
            }
        }
        private BetfairPrices betfairPrices = new BetfairPrices();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb.Text = _value.ToString();
            tb.SelectAll();
        }
		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			TextBox textBox = sender as TextBox;

			if (!char.IsDigit(e.Text, 0))
			{
				if (e.Text == "." && !textBox.Text.Contains("."))
				{
					e.Handled = false; // Allow
				}
				else
				{
					e.Handled = true; // Block
				}
			}
		}
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            RepeatButton b = sender as RepeatButton;
            switch (b.Name)
            {
                case "Up": _NumericValue = betfairPrices.Next(_NumericValue); break;
                case "Down": _NumericValue = betfairPrices.Previous(_NumericValue); break;
                default: return;
            }
            OnPropertyChanged(nameof(Value));
            e.Handled = true;
        }
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
			Int32 caret = tb.CaretIndex;
            switch (e.Key)
            {
                case Key.Up: _NumericValue = betfairPrices.Next(_NumericValue); break;
                case Key.Down: _NumericValue = betfairPrices.Previous(_NumericValue); break;
                default: return;
            }
			OnPropertyChanged(nameof(Value));
			e.Handled = true;
            tb.CaretIndex = caret;
        }
    }
}
