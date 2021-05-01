using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class UpdateBet : Window, INotifyPropertyChanged
	{
		private Row Row { get; set; }
		public String BetReference { get; set; }
		public double Profit{ get; set; }
		public double Stake { get; set; }
		public double Odds { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public UpdateBet(Row row)
		{
			this.Row = row;
			BetReference = "Bet Reference: " + row.BetID;
			Stake = row.Stake;
			Odds = row.Odds;
			InitializeComponent();
			UpDown.Value = Odds;
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
		private void ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			UpDownControl control = sender as UpDownControl;
			Odds = Convert.ToDouble(control.Value);
			NotifyPropertyChanged("");
		}
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			BetfairAPI.BetfairAPI betfair = MainWindow.Betfair;
			System.Threading.Thread t = new System.Threading.Thread(() =>
			{
				ReplaceExecutionReport report = betfair.replaceOrder(Row.MarketID, Row.BetID.ToString(), Odds, Stake);
				Debug.Write(report.ErrorCode.ToString() + " : ");
				Debug.WriteLine(report.instructionReports[0].errorCode.ToString());
			});
			t.Start();
		}
	}
}
