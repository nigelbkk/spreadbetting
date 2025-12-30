using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static System.Windows.Forms.AxHost;

namespace SpreadTrader
{
    public partial class UpDownControl : UserControl, INotifyPropertyChanged
    {
        public double _value;// { get; set; }
        public String Value
        {
            get => $"{_value}";
            set {
				double d = 0;
				if (Double.TryParse(value, out d))
				{
					if (_value != d)
					{
						_value = d;
						OnPropertyChanged(nameof(Value));
					}
				}
			}
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
                _value = d;
                OnPropertyChanged(nameof(Value));
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
			// Allow only digits and one decimal point
			TextBox textBox = sender as TextBox;

			// Check if input is a digit
			if (!char.IsDigit(e.Text, 0))
			{
				// Allow decimal point only if there isn't one already
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
                case "Up":
                    _value = betfairPrices.Next(_value);
                    OnPropertyChanged(nameof(Value));
                    break;
                case "Down":
                    _value = betfairPrices.Previous(_value);
                    OnPropertyChanged(nameof(Value));
                    break;
                default: return;
            }
            e.Handled = true;
        }
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Int32 caret = tb.CaretIndex;
            switch (e.Key)
            {
                case Key.Up: _value = betfairPrices.Next(_value); break;
                case Key.Down: _value = betfairPrices.Previous(_value); break;
                default: return;
            }
			OnPropertyChanged(nameof(Value));
			e.Handled = true;
            tb.CaretIndex = caret;
        }
    }
}
