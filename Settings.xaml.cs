using Microsoft.Win32;
using System;
using System.Windows;

namespace SpreadTrader
{
	public partial class Settings : Window
	{
		public Settings()
		{
			InitializeComponent();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
