using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class SliderControl : UserControl, INotifyPropertyChanged
	{
		private const Int32 BACKPRICES = 0;
		private const Int32 BACKSTAKES = 1;
		private const Int32 LAYPRICES = 2;
		private const Int32 LAYSTAKES = 3;
		public SliderChangedDelegate OnSliderChanged = null;
		private List<List<Decimal>> Values { get; set; }
		public SliderControl()
		{
			Values = new List<List<decimal>>();
			Values.Add(new List<Decimal>());
			Values.Add(new List<Decimal>());
			Values.Add(new List<Decimal>());
			Values.Add(new List<Decimal>());

			Price = 1.52M;
			Decimal p = BetfairAPI.BetfairAPI.PreviousPrice(Price);
			for (Int32 i = 0; i < 9; i++)
			{
				Values[BACKPRICES].Add(p);
				Values[LAYPRICES].Add(p);
				p = BetfairAPI.BetfairAPI.PreviousPrice(p);
			}
			for (Int32 i = 0; i < 10; i++)
			{
				Values[BACKSTAKES].Add(i + 9);
				Values[LAYSTAKES].Add(i + 9);
			}
			InitializeComponent();
			NotifyPropertyChanged("");
		}
		public Decimal Price { get; set; }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (OnSliderChanged != null)
			{
				OnSliderChanged(Values);
			}
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public void CutStakes(Int32 value)
		{
			for (Int32 i = 0; i < 10; i++)
			{
				Values[BACKSTAKES][i] = 10 * (value + i);
				Values[LAYSTAKES][i] = 10 * (value + i);
			}
			NotifyPropertyChanged("");
		}
		public void MovePrices(Int32 side, Int32 value)	
		{
			Decimal p = Price;
			for (int i = 0; i < Math.Abs(value); i++)
			{
				if (value > 0)
					p = BetfairAPI.BetfairAPI.NextPrice(p);
				else
					p = BetfairAPI.BetfairAPI.PreviousPrice(p);
			}
			for (int i = 0; i < Math.Abs(9); i++)
			{
				p = BetfairAPI.BetfairAPI.PreviousPrice(p);
			}
			for (int i = 0; i < 9; i++)
			{
				Values[side][i] = p;
				p = BetfairAPI.BetfairAPI.NextPrice(p);
			}
			NotifyPropertyChanged("");
		}
		private Properties.Settings props = Properties.Settings.Default;
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			String tag = slider.Tag as String;
			Int32 value = Convert.ToInt32(e.NewValue);
			switch (tag)
			{
				case "CutStakes": CutStakes(value+50); break;
				case "MoveBack": MovePrices(BACKPRICES, value); break;
				case "MoveLay": MovePrices(LAYPRICES, value); break;
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
				default: return;
			}
			MovePrices(BACKPRICES, Convert.ToInt32(MoveBack.Value));
			MovePrices(LAYPRICES, Convert.ToInt32(MoveLay.Value));
			NotifyPropertyChanged("");
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			NotifyPropertyChanged("");
		}
	}
}
