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
		//private static Decimal ticksize(Decimal odds)
		//{
		//	if (odds < 2) return 0.01M;
		//	if (odds < 3) return 0.02M;
		//	if (odds < 4) return 0.05M;
		//	if (odds < 6) return 0.1M;
		//	if (odds < 10) return 0.2M;
		//	if (odds < 20) return 0.5M;
		//	if (odds < 30) return 1;
		//	if (odds < 50) return 2;
		//	if (odds < 100) return 5;
		//	return 10;
		//}
		//private static Decimal PreviousPrice(Decimal v)
		//{
		//	Decimal[] MinValue = { 1.01M, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
		//	Decimal[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
		//	Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

		//	if (v > 1000)
		//		return 1000;

		//	for (Int32 idx = 0; idx < MaxValue.Length; idx++)
		//	{
		//		if (v <= MaxValue[idx] && v > MinValue[idx])
		//		{
		//			Decimal lo = (v / Increment[idx]) * Increment[idx];
		//			lo = Math.Round(lo, 2);
		//			lo -= Increment[idx];
		//			return BetfairPrice(lo);
		//		}
		//	}
		//	return v;
		//}

		//private static Decimal NextPrice(Decimal v)
		//{
		//	Decimal[] MinValue = { 1.01M, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
		//	Decimal[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
		//	Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };
			
		//	if (v < MinValue[0])
		//		return MinValue[0];
			
		//	Int32 idx = 0;
		//	for (; idx < MinValue.Length; idx++)
		//	{
		//		if (v >= MinValue[idx] && v <= MaxValue[idx])
		//		{
		//			break;
		//		}
		//	}
		//	Decimal lo = (v / Increment[idx]) * Increment[idx];
		//	lo = Math.Round(lo, 2);
		//	lo += Increment[idx];
		//	return BetfairPrice(lo);
		//}
		//private static Decimal BetfairPrice(Decimal v)
		//{
		//	Decimal OriginalPrice = v;
		//	v = Math.Round(v, 2);
		//	if (v <= 1.01M) return 1.01M;
		//	if (v >= 1000) return 1000;

		//	Decimal[] MinValue = { 1.01M, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
		//	Decimal[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
		//	Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

		//	Int32 idx = 0;
		//	for (; idx < MinValue.Length; idx++)
		//	{
		//		if (v >= MinValue[idx] && v <= MaxValue[idx])
		//		{
		//			break;
		//		}
		//	}
		//	Decimal lo = (Int32)(v / Increment[idx]) * Increment[idx];
		//	lo = Math.Round(lo, 2);

		//	if (lo == v)
		//	{
		//		return Convert.ToDecimal(v);
		//	}
		//	Decimal hi = lo + Increment[idx];
		//	return Math.Abs(lo - OriginalPrice) < Math.Abs(hi - OriginalPrice) ? lo : hi; 
		//}
		public List<Decimal> Prices { get; set; }
		public List<Decimal> Stakes { get; set; }
		public Decimal Price { get; set; }
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
		public void MoveOut(Decimal value)
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
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			Int32 tag = Convert.ToInt32(slider.Tag);
			Decimal intval = Convert.ToDecimal(Math.Round(e.NewValue, 0));
			Decimal value = Convert.ToDecimal(Math.Round(e.NewValue, 2));
			switch(tag)
			{
				case 0: CutStakes(intval); break;
				default: MoveOut(value); break;
			}
			NotifyPropertyChanged("");
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			switch (b.Tag)
			{
				case "-": Price = BetfairAPI.BetfairAPI.PreviousPrice(Price); break;
				case "+": Price = BetfairAPI.BetfairAPI.NextPrice(Price); break;
			}
			NotifyPropertyChanged("");
		}
	}
}
