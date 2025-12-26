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
        private String _stringContent { get; set; }
        public double _Value { get; set; }
        public String Value
        {
            get => _stringContent;
            set
            {
				_stringContent = value;
                OnPropertyChanged(nameof(_stringContent));
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
                _Value = d;
            }
        }
        private BetfairPrices betfairPrices = new BetfairPrices();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb.Text = _Value.ToString();
            tb.SelectAll();
        }
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            RepeatButton b = sender as RepeatButton;
            switch (b.Name)
            {
                case "Up":
                    _Value = betfairPrices.Next(_Value);
                    break;
                case "Down":
                    _Value = betfairPrices.Previous(_Value);
                    break;
                default: return;
            }
            _stringContent = _Value.ToString();
            e.Handled = true;
        }
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Int32 caret = tb.CaretIndex;
            switch (e.Key)
            {
                case Key.Up: _Value = betfairPrices.Next(_Value); break;
                case Key.Down: _Value = betfairPrices.Previous(_Value); break;
                default: return;
            }
			_stringContent = _Value.ToString();
            e.Handled = true;
            tb.CaretIndex = caret;
        }
    }
}
