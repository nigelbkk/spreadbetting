using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class UpdateBet : Window, INotifyPropertyChanged
	{
		private Row Row { get; set; }
		public String BetReference { get; set; }
		public double Profit { get; set; }
		public String ProfitLiability{ get {return String.Format("Profit/Liability: {0:0.00}", Profit); }}
		public double Stake { get; set; }
		public double Odds { get; set; }
		public DependencyObject ParentObject { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
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
			InitializeComponent();

			if (props.BRLeft >= 0 && props.BRTop >= 0)
			{
				Top = props.BRTop + Application.Current.MainWindow.Top;
				Left = props.BRLeft + Application.Current.MainWindow.Left;
			}
			this.Row = row;
			BetReference = "Bet Reference: " + row.BetID;
			Stake = row.Stake;
			Odds = row.Odds;
//			Profit = 5.1;				///NH
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
				if (report.status != ExecutionReportStatusEnum.SUCCESS)
				{
					Debug.Write(report.ErrorCode.ToString() + " : ");
					Debug.WriteLine(report.instructionReports[0].errorCode.ToString());
					MessageBox.Show(report.instructionReports[0].errorCode.ToString(), "Update Bet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			});
			t.Start();
		}
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			props.BRTop = Top - Application.Current.MainWindow.Top;
			props.BRLeft = Left - Application.Current.MainWindow.Left;
			props.Save();
		}
	}
}
