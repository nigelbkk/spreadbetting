using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class SliderControl : UserControl, INotifyPropertyChanged
	{
		private const Int32 BACKPRICES = 0;
		private const Int32 BACKSTAKES = 10;
		private const Int32 LAYPRICES = 20;
		private const Int32 LAYSTAKES = 30;
		public SliderChangedDelegate OnSliderChanged = null;
		public static Decimal[] Values { get; set; }
		private List<Decimal> AllPrices = null;
		public SliderControl()
		{
			Decimal[] MinValue = { 1.01M, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
			Decimal[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
			Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

			AllPrices = new List<decimal>();
			Decimal m = 1.0M;
			for (Int32 idx = 0; idx < MinValue.Length; idx++)
			{
				while (m < MaxValue[idx])
				{
					m += Increment[idx];
					AllPrices.Add(m);
				}
			}
			BasePrice = props.BasePrice;
			Int32 index = PriceIndex(BasePrice);
			InitializeComponent();
		}
		private Decimal _BasePrice;
		public Decimal BasePrice { get { return _BasePrice; } set { _BasePrice = Math.Max(value, 1.10M); } }
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
		private Int32 PriceIndex(Decimal v)
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
		private Decimal PreviousPrice(Decimal v)
		{
			return AllPrices[PriceIndex(v) - 1];
		}
		private Decimal NextPrice(Decimal v)
		{
			return AllPrices[PriceIndex(v) + 1];
		}
		public void MoveStakes(Int32 newvalue, Int32 oldvalue)
		{
			for (Int32 i = 0; i < 10; i++)
			{
				Decimal vb = Values[BACKSTAKES + i];
				Decimal vl = Values[LAYSTAKES + i];
				if (newvalue > oldvalue)
				{
					vb /= 9; vb *= 10;
					vl /= 9; vl *= 10;
					Values[BACKSTAKES + i] = vb;
					Values[LAYSTAKES + i] = vl;
				}
				else if (oldvalue > newvalue)
				{
					vl /= 10; vl *= 9;
					vb /= 10;	vb *= 9;
					Values[BACKSTAKES + i] = vb;
					Values[LAYSTAKES + i] = vl;
				}
			}
			//Int32 offset = Convert.ToInt32(CutStakes.Value);
			//for (Int32 i = 0; i < 10; i++)
			//{
			//	Values[BACKSTAKES + i] = Math.Round(Values[BACKSTAKES + i] * offset/10, 0);
			//	Values[LAYSTAKES + i] = Values[LAYSTAKES + i] * offset/10;
			//}
			NotifyPropertyChanged("");
		}
		public void SyncPrices()
		{
			try
			{
				if (BasePrice < 1000 && BasePrice > 1.01M)
				{
					for (Int32 side = BACKPRICES; side <= LAYPRICES; side+=20)
					{
						Int32 base_index = PriceIndex(BasePrice) - 9;
						base_index = Math.Max(base_index, 10);
						base_index = Math.Min(base_index, 338);
						Int32 offset = Convert.ToInt32(side == 0 ? MoveBack.Value : MoveLay.Value);
						for (int i = 0; i < 9; i++)
						{
							Values[side+i] = AllPrices[base_index + offset + i];// + BasePrice - 1.01M;
						}
					}
					NotifyPropertyChanged("");
				}
			}
			catch (Exception xe)
			{
			}
		}
		private Properties.Settings props = Properties.Settings.Default;
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			String tag = slider.Tag as String;
			switch (tag)
			{
				case "CutStakes": MoveStakes((Int32) e.NewValue, (Int32) e.OldValue); break;
				case "MoveBack": SyncPrices(); break;
				case "MoveLay": SyncPrices(); break;
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			if (BasePrice < 1000 && BasePrice > 1.01M)
			{
				switch (b.Tag)
				{
					case "-": BasePrice = PreviousPrice(BasePrice); break;
					case "+": BasePrice = NextPrice(BasePrice); break;
					default: return;
				}
				SyncPrices();
			}
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			for (Int32 i = 0; i < 9; i++)
			{
				Values[BACKPRICES + i] = AllPrices[i];
				Values[LAYPRICES + i] = AllPrices[i];
			}
			for (Int32 i = 0; i < 10; i++)
			{
				Values[LAYSTAKES + i] = Values[BACKSTAKES + i] = 20 + i*10;
			}
			SyncPrices();
		}
		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox tx = sender as TextBox;
			if (!String.IsNullOrEmpty(tx.Text))
			{
				BasePrice = BetfairAPI.BetfairAPI.BetfairPrice(Convert.ToDecimal(tx.Text));
				props.BasePrice = BasePrice;
				SyncPrices();
				tx.Text = Convert.ToString(BasePrice);
				NotifyPropertyChanged("");
				props.Save();
			}
		}
	}
}
