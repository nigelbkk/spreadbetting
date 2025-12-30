using BetfairAPI;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
    public partial class ConfirmationDialog : Window, INotifyPropertyChanged
    {
        private RunnersControl runnersControl = null;
        //public DependencyObject ParentObject { get; set; }
        public String Side;// { get; set; }
        public String MarketId;// { get; set; }
        public String MarketName;// { get; set; }
        public String Runner { get; set; }
        public Int64 SelectionId;// { get; set; }
		private double _Stake;
		public String Stake { get => $"{_Stake}";
            set 
            {
				double d = 0;

				if (Double.TryParse(value, out d))
				{
					if (_Stake != d)
                    {
                        _Stake = d;
                        OnPropertyChanged(nameof(Stake));
                    }
                }
            } 
        }
        public double Odds { get; set; }
        //public double Liability { get; set; }
        //public double Payout { get; set; }
        public String Header { get { return String.Format("{0} {1} for {2}", Side, Runner, Odds); } }
        private Properties.Settings props = Properties.Settings.Default;
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public ConfirmationDialog(RunnersControl runnersControl, String MarketId, LiveRunner runner, String side, double odds)
        {
            this.runnersControl = runnersControl;
            if (props.CDLeft > 0 && props.CDTop > 0)
            {
                Top = props.CDTop + Application.Current.MainWindow.Top;
                Left = props.CDLeft + Application.Current.MainWindow.Left;
            }
            Runner = runner.Name;
            SelectionId = runner.SelectionId;
            Side = side;
            Odds = odds;
            _Stake = props.DefaultStake;
            if (props.SafeBets)
            {
                _Stake = 2.00;
                Odds = 1.01;
                Side = "Lay";
            }
            this.MarketId = MarketId;
            InitializeComponent();
            UpDown._value = Odds;
            FocusManager.SetFocusedElement(DockPanel, Submit_button);
            IInputElement focusedElement = FocusManager.GetFocusedElement(DockPanel);
        }
        private void Submit(object sender, RoutedEventArgs _e)
        {
            BetfairAPI.BetfairAPI betfair = MainWindow.Betfair;
            Button b = sender as Button;
            String cs = b.Content as String;
            Odds = UpDown._value;
            if (cs != null)
            {
                if (cs == "Close")
                {
                    Close();
                    return;
                }
                Int32 stake = Math.Max(2, (int)_Stake);
                if (cs == "Submit")
                {
                    //System.Threading.Thread t = new System.Threading.Thread(() =>
                    //{
                    //    DateTime LastUpdate = DateTime.UtcNow;
                    //    PlaceExecutionReport report = betfair.placeOrder(MarketId, SelectionId, Side == "Lay" ? sideEnum.LAY : sideEnum.BACK, Stake, Odds);
                    //    ControlMessenger.Send("Update P&L");
                    //});
                    //t.Start();
                }
                else
                {
                    //Stake = Convert.ToInt32(b.Content);
                    //NotifyPropertyChanged("");
                }
            }
        }
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            props.CDTop = Top - Application.Current.MainWindow.Top;
            props.CDLeft = Left - Application.Current.MainWindow.Left;
            props.Save();
        }
        private void DockPanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: Close(); break;
                case Key.Return: Submit(Submit_button, null); break;
            }
            //NotifyPropertyChanged("");
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
		private void StakeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
			double d = 0;

			if (Double.TryParse(tb.Text, out d))
			{
				if (_Stake != d)
				{
					_Stake = d;
				}
			}
		}
    }
}
