using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public delegate void SliderChangedDelegate(List<List<Decimal>> values);
	public partial class BettingGrid : UserControl, INotifyPropertyChanged
	{
		public List<List<Decimal>> Values { get; set; }
		public List<Decimal> Prices { get; set; }
		public List<Decimal> Stakes { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public BettingGrid()
		{
			InitializeComponent();
		}
		public void OnSliderChanged(List<List<Decimal>> values)
		{
			Values = values;
			NotifyPropertyChanged("");
		}
		//public void OnSliderChanged(List<Decimal> prices, List<Decimal> stakes)
		//{
		//	Stakes = stakes;
		//	Prices = prices;
		//	NotifyPropertyChanged("");
		//}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			switch (b.Tag)
			{
			}
			NotifyPropertyChanged("");
		}
	}
}
