using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
		public double Stake   { get; set; }
		public double Odds   { get; set; }
		public  double Liability   { get; set; }
		public double Payout  { get; set; }
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
		public ConfirmationDialog(Visual visual, Button b, String MarketId, LiveRunner runner, String side, double odds)
		{
			runnersControl = visual as RunnersControl;
			ParentObject = visual as DependencyObject;
			Point coords = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(b.ActualWidth, b.ActualHeight)));
			Top = coords.Y;
			Left = coords.X;
			Runner = runner.Name;
			SelectionId = runner.SelectionId;
			Side = side;
			Odds = odds;
			if (props.SafeBets)
			{
				Stake = 2.00;
				Odds = 1.01;
				Side = "Lay";
			}
			this.MarketId = MarketId;
			InitializeComponent();
			UpDown.Value = Odds;
		}
		private void Submit(object sender, RoutedEventArgs e)
		{
			BetfairAPI.BetfairAPI betfair = new BetfairAPI.BetfairAPI();
			Button b = sender as Button;
			String cs = b.Content as String;
			if (cs != null)
			{
				if (cs == "Cancel")
				{
					Close();
					return;
				}
				Int32 stake = (int)Stake;
				if (cs != "Submit")
				{
					stake = Convert.ToInt32(b.Content);
				}
				using (StreamWriter w = File.AppendText("log.csv"))
				{
					String cs2 = String.Format("{1},{2},{3},{4:0},{5:0.00},{6}", "", "WIN", Side, Runner, stake, Odds, DateTime.UtcNow.Millisecond);
					w.WriteLine(cs2);
					Debug.WriteLine(cs2);
				}
				try
				{
					PlaceExecutionReport report = betfair.placeOrder(MarketId, SelectionId, Side == "Lay" ? sideEnum.LAY : sideEnum.BACK, Stake, Odds);
					OrdersStatic.BetID2SelectionID[report.instructionReports[0].betId] = SelectionId;
				}
				catch (Exception xe)
				{
					Debug.WriteLine(xe.Message);
					MainWindow mw = Extensions.FindParentOfType<MainWindow>(this);
					if (mw != null) mw.Status = xe.Message;
				}
			}
		}

		private void ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			UpDownControl control = sender as UpDownControl;
			Odds = Convert.ToDouble(control.Value);
			NotifyPropertyChanged("");
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
