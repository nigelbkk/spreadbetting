using BetfairAPI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SpreadTrader
{
    public partial class UpdateBet : Window, INotifyPropertyChanged
    {
        private Row Row { get; set; }
        public String BetReference { get; set; }
        public double Profit { get; set; }
        public String ProfitLiability { get { return String.Format("Profit/Liability: {0:0.00}", Profit); } }
        public Int32 OriginalStake { get; set; }
        public Int32 Stake { get; set; }
        public double Odds { get; set; }
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
            OriginalStake = Stake = (Int32)row.Stake;
            Odds = row.Odds;
            UpDownOdds.Value = Odds;
            UpDownStake.Value = (short)Stake;
            NotifyPropertyChanged("");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Odds = UpDownOdds.Value;
            Stake = UpDownStake.Value.Value;

            BetfairAPI.BetfairAPI betfair = MainWindow.Betfair;
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                if (OriginalStake != Stake)
                {
                    CancelExecutionReport report1 = betfair.cancelOrder(Row.MarketID, Row.BetID);
                    PlaceExecutionReport report2 = betfair.placeOrder(Row.MarketID, Row.SelectionID, Row.Side == "BACK" ? sideEnum.BACK : sideEnum.LAY, Stake, Odds);
                }
                else
                {
                    ReplaceExecutionReport report = betfair.replaceOrder(Row.MarketID, Row.BetID.ToString(), Odds, Stake);
                    if (report.status != ExecutionReportStatusEnum.SUCCESS)
                    {
                        Debug.Write(report.ErrorCode.ToString() + " : ");
                        Debug.WriteLine(report.instructionReports[0].errorCode.ToString());
                        MessageBox.Show(report.instructionReports[0].errorCode.ToString(), "Update Bet", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                NotifyPropertyChanged("");
            });
            t.Start();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            props.BRTop = Top - Application.Current.MainWindow.Top;
            props.BRLeft = Left - Application.Current.MainWindow.Left;
            props.Save();
        }
        private Int32 IncrementStake(Int32 value)
        {
            if (value < 10)
                return value + 1;

            return (value + 10) - (value) % 10;
        }
        private Int32 DecrementStake(Int32 value)
        {
            if (value <= 2)
                return value;
            if (value <= 10)
                return value - 1;

            return (value - 10) - (value % 10);
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            BetfairPrices betfairPrices = new BetfairPrices();
            switch (e.Key)
            {
                case Key.Up:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        Stake = IncrementStake(Stake);
                    else
                        Odds = betfairPrices.Next(Odds); break;
                case Key.Down:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        Stake = DecrementStake(Stake);
                    else
                        Odds = betfairPrices.Previous(Odds); break;
                case Key.Return: Button_Click_1(Update, null); break;
                case Key.Escape: Close(); break;
            }
            UpDownOdds.Value = Odds;
            UpDownStake.Value = Stake;
            e.Handled = true;
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal)
                e.Handled = false;
            NotifyPropertyChanged("");
        }
    }
}
