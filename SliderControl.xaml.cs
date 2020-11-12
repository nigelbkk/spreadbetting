using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	public enum sliderEnum
	{
		LAYPRICES = 0,
		LAYSTAKES = 10,
		BACKPRICES = 20,
		BACKSTAKES = 30,
	}
	public partial class SliderControl : UserControl, INotifyPropertyChanged
	{
		public SliderChangedDelegate OnSliderChanged = null;
		public SubmitBetsDelegate SubmitBets = null;
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
		public bool AutoOn { get; set; }
		public Decimal BasePrice { get { return _BasePrice; } set { _BasePrice = Math.Max(value, 1.10M); } }
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (OnSliderChanged != null)
			{
				OnSliderChanged();
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
				Decimal vb = Values[(int) sliderEnum.BACKSTAKES + i]; Decimal vl = Values[(int)sliderEnum.LAYSTAKES + i];
				if (newvalue > oldvalue)
				{
					vb /= 9; vb *= 10; vl /= 9; vl *= 10;
					Values[(int)sliderEnum.BACKSTAKES + i] = vb; Values[(int)sliderEnum.LAYSTAKES + i] = vl;
				}
				else if (oldvalue > newvalue)
				{
					vl /= 10; vl *= 9; vb /= 10; vb *= 9;
					Values[(int)sliderEnum.BACKSTAKES + i] = vb; Values[(int)sliderEnum.LAYSTAKES + i] = vl;
				}
			}
			NotifyPropertyChanged("");
		}
		public void SyncPrices()
		{
			try
			{
				if (Values != null && BasePrice < 1000 && BasePrice > 1.01M)
				{
					Int32 base_index = PriceIndex(BasePrice) - 21;
					Int32 side = (int) sliderEnum.BACKPRICES;
					base_index = Math.Max(base_index, 10);
					base_index = Math.Min(base_index, 338);
					Int32 offset = Convert.ToInt32(MoveLay.Value);
					for (int i = 0; i < 9; i++)
					{
						Values[side + i] = AllPrices[base_index + offset - i];
					}
					side = (int)sliderEnum.LAYPRICES;
					base_index = PriceIndex(BasePrice) + 1;
					base_index = Math.Max(base_index, 10);
					base_index = Math.Min(base_index, 338);
					offset = Convert.ToInt32(MoveBack.Value);
					for (int i = 0; i < 9; i++)
					{
						Values[side + i] = AllPrices[base_index + offset + i];
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
			switch (slider.Name)
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
					case "Auto":
						AutoOn = !AutoOn;
						b.Foreground = new SolidColorBrush(AutoOn ? Colors.Black : Colors.LightGray);
						break;
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
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			for (Int32 i = 0; i < 9; i++)
			{
				Values[(int)sliderEnum.BACKPRICES + i] = AllPrices[i];
				Values[(int)sliderEnum.LAYPRICES + i] = AllPrices[i];
			}
			for (Int32 i = 0; i < 10; i++)
			{
				Values[(int)sliderEnum.LAYSTAKES + i] = Values[(int)sliderEnum.BACKSTAKES + i] = 20 + i*10;
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
