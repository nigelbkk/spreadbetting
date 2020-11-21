using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BetfairAPI;

namespace SpreadTrader
{
	public partial class SliderControl : UserControl, INotifyPropertyChanged
	{
		public SubmitBetsDelegate SubmitBets = null;
		public double CutStakes { get; set; }
		public double MoveBack { get; set; }
		public double MoveLay { get; set; }
		public static PriceSize[] BackValues { get; set; }
		public static PriceSize[] LayValues { get; set; }
		public static bool AutoBackLay { get; set; }
		private List<double> AllPrices = null;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public SliderControl()
		{
			double[] MinValue = { 1.01, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
			double[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
			Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

			CutStakes = 10;
			MoveBack = 10;
			MoveLay = 22;

			AllPrices = new List<double>();
			Decimal m = 1.0M;
			for (Int32 idx = 0; idx < MinValue.Length; idx++)
			{
				while (m < (Decimal) MaxValue[idx])
				{
					m += Increment[idx];
					AllPrices.Add((double) m);
				}
			}
			BackValues = new PriceSize[9];
			LayValues = new PriceSize[9];
			for (Int32 i = 0; i < 9; i++)
			{
				BackValues[i] = new PriceSize(AllPrices[i], 20 + 1 * 10);
				LayValues[i] = new PriceSize(AllPrices[i], 20 + 1 * 10);
			}
			BasePrice = props.BasePrice;
			InitializeComponent();
		}
		private double _BasePrice;
		public double BasePrice { get { return _BasePrice; } set { _BasePrice = Math.Max(value, 1.10); } }
		private Int32 PriceIndex(double v)
		{
			for (int i = 1; i < AllPrices.Count; i++)
			{
				if (AllPrices[i] == v)
				{
					return i;
				}
			}
			return 1;
		}
		private double PreviousPrice(double v)
		{
			return (double) AllPrices[PriceIndex(v) - 1];
		}
		private double NextPrice(double v)
		{
			return (double) AllPrices[PriceIndex(v) + 1];
		}
		public void MoveStakes(Int32 newvalue, Int32 oldvalue)
		{
			for (Int32 i = 0; i < 9; i++)
			{
				PriceSize vb = BackValues[i];
				PriceSize vl = LayValues[i];
				if (newvalue > oldvalue)
				{
					vb.size /= 9; vb.size *= 10; vl.size /= 9; vl.size *= 10;
					BackValues[i] = vb; 
					LayValues[i] = vl;
				}
				else if (oldvalue > newvalue)
				{
					vl.size /= 10; vl.size *= 9; vb.size /= 10; vb.size *= 9;
					BackValues[i] = vb; 
					LayValues[i] = vl;
				}
			}
		}
		public void SyncPrices()
		{
			if (BackValues != null && BasePrice < 1000 && BasePrice > 1.01)
			{
				Int32 base_index = PriceIndex(BasePrice) - 23;
				base_index = Math.Max(base_index, 10);
				base_index = Math.Min(base_index, 338);
				Int32 offset = Convert.ToInt32(MoveLay);
				for (int i = 0, j = 8; i < 9; i++, j--)
				{
					LayValues[i].price = AllPrices[base_index + offset - j];
				}
				base_index = PriceIndex(BasePrice);
				base_index = Math.Max(base_index, 10);
				base_index = Math.Min(base_index, 338);
				offset = Convert.ToInt32(MoveBack);
				for (int i = 0; i < 9; i++)
				{
					BackValues[i].price = AllPrices[base_index + offset + i];
				}
				NotifyPropertyChanged("");
			}
		}
		private Properties.Settings props = Properties.Settings.Default;
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SyncPrices();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (BasePrice < 1000 && BasePrice > 1.01)
			{
				switch (b.Tag)
				{
					case "-": BasePrice = PreviousPrice(BasePrice); break;
					case "+": BasePrice = NextPrice(BasePrice); break;
					case "Execute":
						if (SubmitBets != null)
						{
							SubmitBets();
						}
						break;
					default: return;
				}
				SyncPrices();
			}
		}
		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox tx = sender as TextBox;
			if (!String.IsNullOrEmpty(tx.Text))
			{
				BasePrice = BetfairAPI.BetfairAPI.BetfairPrice(Convert.ToDouble(tx.Text));
				props.BasePrice = BasePrice;
				SyncPrices();
				tx.Text = Convert.ToString(BasePrice);
				props.Save();
			}
		}
		private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			MoveStakes((Int32)e.NewValue, (Int32)e.OldValue);
		}
	}
}
