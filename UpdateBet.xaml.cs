using System.Windows;

namespace SpreadTrader
{
	public partial class UpdateBet : Window
	{
		public UpdateBet(Row row)
		{
			InitializeComponent();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
