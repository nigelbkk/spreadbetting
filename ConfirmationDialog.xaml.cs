using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class ConfirmationDialog : Window, INotifyPropertyChanged
	{
		private RunnersControl runnersControl = null;
		public DependencyObject ParentObject { get; set; }
		public String Side { get; set; }
		public String MarketId { get; set; }
		public String MarketName { get; set; }
		public String Runner { get; set; }
		public Int64 SelectionId { get; set; }
		public double Stake { get; set; }
		public double Odds { get; set; }
		public double Liability { get; set; }
		public double Payout { get; set; }
		public String Header { get { return String.Format("{0} {1} for {2}", Side, Runner, Odds); } }
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public ConfirmationDialog(RunnersControl runnersControl, String MarketId, LiveRunner runner, String side, double odds)
		{
			this.runnersControl = runnersControl;
			//if (props.CDLeft > 0 && props.CDTop > 0)
			//{
			//	Top = props.CDTop + Application.Current.MainWindow.Top;
			//	Left = props.CDLeft + Application.Current.MainWindow.Left;
			//}
			Runner = runner.Name;
			SelectionId = runner.SelectionId;
			Side = side;
			Odds = odds;
			Stake = props.DefaultStake;
			if (props.SafeBets)
			{
				Stake = 2.00;
				Odds = 1.01;
				Side = "Lay";
			}
			this.MarketId = MarketId;
			InitializeComponent();
			UpDown.Value = Odds;
			FocusManager.SetFocusedElement(DockPanel, Submit_button);
			IInputElement focusedElement = FocusManager.GetFocusedElement(DockPanel);
		}
		private void Submit(object sender, RoutedEventArgs _e)
		{
			
			BetfairAPI.BetfairAPI betfair = MainWindow.Betfair;
			Button b = sender as Button;
			String cs = b.Content as String;
			if (cs != null)
			{
				if (cs == "Close")
				{
					Close();
					return;
				}
				Int32 stake = (int)Stake;
				if (cs == "Submit")
				{
					System.Threading.Thread t = new System.Threading.Thread(() =>
					{
						DateTime LastUpdate = DateTime.UtcNow;
						PlaceExecutionReport report = betfair.placeOrder(MarketId, SelectionId, Side == "Lay" ? sideEnum.LAY : sideEnum.BACK, Stake, Odds);
						runnersControl.MarketNode.TurnaroundTime = (Int32)((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
					});
					t.Start();
				}
				else
				{
					Stake = Convert.ToInt32(b.Content);
					NotifyPropertyChanged("");
				}
			}
		}
		private void ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			UpDownControl control = sender as UpDownControl;
			Odds = Convert.ToDouble(control.Value);
			NotifyPropertyChanged("");
		}
		//private void Window_LocationChanged(object sender, EventArgs e)
		//{
		//	props.CDTop = Top - Application.Current.MainWindow.Top;
		//	props.CDLeft = Left - Application.Current.MainWindow.Left;
		//	props.Save();
		//}
		private void DockPanel_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			BetfairPrices betfairPrices = new BetfairPrices();
			switch (e.Key)
			{
				case Key k when ((k >= Key.D0 && k <= Key.D9) || (k >= Key.NumPad0 && k <= Key.NumPad9)):
					return;
				case Key k when (k >= Key.A && k <= Key.Z):
					e.Handled = true;
					return;
				case Key.Up: UpDown.Value = betfairPrices.Next(Odds);
					e.Handled = true;
					break;
				case Key.Down: UpDown.Value = betfairPrices.Previous(Odds);
					e.Handled = true;
					break;
				case Key.Escape: Close(); break;
				case Key.Return: Submit(Submit_button, null); break;
			}
			NotifyPropertyChanged("");
		}

		private void StakeTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (!String.IsNullOrEmpty(tb.Text))
				Stake = Convert.ToDouble(tb.Text);
		}

		private void UpDown_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			UpDownControl control = sender as UpDownControl;
			control.Focus();
		}
	}
	public class UpDownControl : Xceed.Wpf.Toolkit.DoubleUpDown
	{
		private BetfairPrices betfairPrices = new BetfairPrices();
		protected override double IncrementValue(double value, double increment)
		{
			return betfairPrices.Next(value);
		}
		protected override double DecrementValue(double value, double increment)
		{
			return betfairPrices.Previous(value);
		}
	}
}
