using BetfairAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	public delegate void SliderChangedDelegate(Decimal[] values);
	public partial class BettingGrid : UserControl, INotifyPropertyChanged
	{
		public bool?[] CheckBoxes { get; set; }
		public Decimal[] Values { get; set; }
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
			CheckBoxes = new bool?[20];
			Values = new Decimal[40];
		}
		public void OnSliderChanged(Decimal[] values)
		{
			Values = values;
			NotifyPropertyChanged("");
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			ItemsControl ic = BackCheckboxes;
			for (int i = 0; i < ic.Items.Count; i++)
			{
				CheckBox cb = ic.Items[i] as CheckBox;
				if (cb != null)
				{
					cb.Tag = i;// String.Format("BackCheckbox{0}", i);
				}
			}
			ic = LayCheckboxes;
			for (int i = 0; i < ic.Items.Count; i++)
			{
				CheckBox cb = ic.Items[i] as CheckBox;
				if (cb != null)
				{
					cb.Tag = 10+i;
				}
			}
		}
		private void DisableLabel(Int32 idx, bool? value)
		{
			Label label = null;
			switch (idx / 10)
			{
				case 0:
					label = BackPrices.Items[idx] as Label;
					label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
					label = BackStakes.Items[idx] as Label;
					label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
					break;
				case 1:
					label = LayPrices.Items[idx-10] as Label;
					label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
					label = LayStakes.Items[idx-10] as Label;
					label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
					break;
			}
		}
		private void CheckBox_Click(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			Int32 tag = Convert.ToInt32(cb.Tag);
			if (tag == 0 || tag == 10)
			{
				for(int i= 1;i<10; i++)
				{
					CheckBoxes[tag+i] = cb.IsChecked;
					DisableLabel(tag+i, cb.IsChecked);
				}
			}
			DisableLabel(tag, cb.IsChecked);
			NotifyPropertyChanged("");
		}
	}
}
