using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class BettingGrid : UserControl, INotifyPropertyChanged
	{
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
		public void CutStakes(Decimal value)
		{
			for (Int32 i = 0; i < 10; i++)
			{
				Stakes[i] = 10* Math.Round(value+i, 0);
			}
			NotifyPropertyChanged("");
		}
		public void MoveBets(Decimal value)
		{
			Decimal p = BetfairAPI.BetfairAPI.NextPrice(value);
			for (Int32 i = 0; i < 10; i++)
			{
				Prices[i] = p;
				p = BetfairAPI.BetfairAPI.NextPrice(p);
			}
			NotifyPropertyChanged("");
		}
		private Properties.Settings props = Properties.Settings.Default;
		public BettingGrid()
		{
			Stakes = new List<Decimal>();
			Prices = new List<Decimal>();
			Decimal p = BetfairAPI.BetfairAPI.NextPrice(1.01M);
			for (Int32 i = 0; i < 10; i++)
			{
				Prices.Add(p);
				p = BetfairAPI.BetfairAPI.NextPrice(p);
			}
			for (Int32 i = 0; i < 10; i++)
			{
				Stakes.Add(i+10);
			}
			InitializeComponent();
		}
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
