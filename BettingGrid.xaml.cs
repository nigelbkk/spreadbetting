using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	public partial class BettingGrid : UserControl
	{
		public class Row : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged(String info)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(info));
				}
			}
			public List<String> Values { get; set; }
			public String Name { get; set; }
			public Row()
			{
				Values = new List<String>();
			}
			public void MoveLeft(Int32 index, Int32 value)
			{
				for (Int32 i = 0; i < 10; i++)
				{
					switch (index)
					{
						case 0: Values[i] = String.Format("{0} ticks or {1}", 10 * value * i, 1.01); break;
						case 1: Values[i] = String.Format("{0}", 50 * value * i); break;
					}
				}
				NotifyPropertyChanged("");
			}
		}
		private Properties.Settings props = Properties.Settings.Default;
		public List<Row> Items { get; set; }
		public BettingGrid()
		{
			Items = new List<Row>();
			Row a = new Row() { Name = "Prices" };
			for (Int32 i = 0; i < 10; i++)
			{
				a.Values.Add(String.Format("{0} ticks or {1}", 10 * i, 1.01));
			}
			Items.Add(a);
			a = new Row() { Name = "Stakes" };
			for (Int32 i = 0; i < 10; i++)
			{
				a.Values.Add(String.Format("{0}", 50 * i));
			}
			Items.Add(a);
			InitializeComponent();
		}
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			Slider slider = sender as Slider;
			Int32 idx = Convert.ToInt32(slider.Tag);
			Items[idx].MoveLeft(idx, Convert.ToInt32(e.NewValue));
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
	}
}
