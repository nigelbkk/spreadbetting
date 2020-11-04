using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpreadTrader
{
	public partial class SliderControl : UserControl, INotifyPropertyChanged
	{
		public SliderControl()
		{
			InitializeComponent();
		}
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
			//for (Int32 i = 0; i < 10; i++)
			//{
			//	Stakes[i] = 10 * Math.Round(value + i, 0);
			//}
			NotifyPropertyChanged("");
		}
		public void MoveOut(Decimal value)
		{
			Decimal p = BetfairAPI.BetfairAPI.NextPrice(value);
			for (Int32 i = 0; i < 10; i++)
			{
				//Prices[i] = p;
				//p = BetfairAPI.BetfairAPI.NextPrice(p);
			}
			NotifyPropertyChanged("");
		}
		private Properties.Settings props = Properties.Settings.Default;
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			Int32 tag = Convert.ToInt32(slider.Tag);
			Decimal intval = Convert.ToDecimal(Math.Round(e.NewValue, 0));
			Decimal value = Convert.ToDecimal(Math.Round(e.NewValue, 2));
			switch (tag)
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
