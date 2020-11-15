using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BetfairAPI;

namespace SpreadTrader
{
	public class PriceSize : INotifyPropertyChanged
	{
		public PriceSize(double price, double size) { this.price = price; this.size = size; }
		private Double _price { get; set; }
		public Double price { get { return _price; } set { _price = value; NotifyPropertyChanged(""); } }
		private Double _size { get; set; }
		public Double size { get { return _size; } set { _size = value; NotifyPropertyChanged(""); } }
		public override string ToString()
		{
			return String.Format("{0}:{1}", price, size);
		}
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}

	public delegate void SubmitBetsDelegate();
	public partial class BettingGrid : UserControl
	{
		public bool[] CheckBoxes { get; set; }
		public PriceSize[] BackValues { get; set; }
		public PriceSize[] LayValues { get; set; }
		public NodeViewModel MarketNode { get; set; }
		public BettingGrid()
		{
			InitializeComponent();
			BackValues = SliderControl.BackValues;
			LayValues = SliderControl.LayValues;
			CheckBoxes = new bool[20];
			for (int i = 0; i < 20; i++)
			{
				CheckBoxes[i] = true;
			}
		}
		private PlaceExecutionReport placeOrders(String marketId, List<PlaceInstruction> instructions)
		{
			BetfairAPI.BetfairAPI Betfair = new BetfairAPI.BetfairAPI();
			try
			{
				using (StreamWriter w = File.AppendText("log.csv"))
				{
					foreach (PlaceInstruction pi in instructions)
					{
						w.WriteLine(MarketNode.FullName + "," + pi);
						Debug.WriteLine(pi);
					}
				}
			}
			catch (Exception xe) { Debug.WriteLine(xe.Message); }
			//PlaceExecutionReport report = Betfair.placeOrders(bet.MarketId, instructions);
			return new PlaceExecutionReport();
		}
		public void SubmitBets()
		{
			using (StreamWriter sw = File.CreateText("log.csv"))
			{
				sw.WriteLine("Market Name, Order, Market, Side, Runner, Stake, Odds, Time");
			}
			if (MarketNode != null)
			{
				List<PriceSize> laybets = new List<PriceSize>();
				List<PriceSize> backbets = new List<PriceSize>();
				List<LiveRunner> runners = new List<LiveRunner>();
				for (Int32 i = 0; i < 9; i++)
				{
					laybets.Add(LayValues[i]);
					backbets.Add(BackValues[i]);
				}
				laybets.Sort((a, b) => Math.Sign(b.price - a.price));
				backbets.Sort((a, b) => Math.Sign(a.price - b.price));
				for (Int32 i = 0; i < 9; i++)
				{
					List<PlaceInstruction> instructions = new List<PlaceInstruction>();
					foreach (var runner in MarketNode.Market.runners)
					{
						instructions.Add(new PlaceInstruction()
						{
							orderTypeEnum = orderTypeEnum.LIMIT,
							sideEnum = sideEnum.LAY,
							Runner = String.Format("{0} {1}", runner.name, runner.handicap > 0 ? runner.handicap.ToString() : ""),
							marketTypeEnum = marketTypeEnum.WIN,
							selectionId = runner.selectionId,
							limitOrder = new LimitOrder()
							{
								persistenceTypeEnum = persistenceTypeEnum.LAPSE,
								price = laybets[i].price,
								size = laybets[i].size,
							}
						});
					}
					placeOrders(MarketNode.MarketID, instructions);
				}
				for (Int32 i = 0; i < 9; i++)
				{
					List<PlaceInstruction> instructions = new List<PlaceInstruction>();
					foreach (var runner in MarketNode.Market.runners)
					{
						instructions.Add(new PlaceInstruction()
						{
							orderTypeEnum = orderTypeEnum.LIMIT,
							sideEnum = sideEnum.BACK,
							Runner = runner.name,
							marketTypeEnum = marketTypeEnum.WIN,
							selectionId = runner.selectionId,
							limitOrder = new LimitOrder()
							{
								persistenceTypeEnum = persistenceTypeEnum.LAPSE,
								price = backbets[i].price,
								size = backbets[i].size,
							}
						});
					}
					placeOrders(MarketNode.MarketID, instructions);
				}
			}
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			//ItemsControl ic = BackCheckboxes;
			//for (int i = 0; i < ic.Items.Count; i++)
			//{
			//	CheckBox cb = ic.Items[i] as CheckBox;
			//	if (cb != null)
			//	{
			//		cb.Tag = i;
			//	}
			//}
			//ic = LayCheckboxes;
			//for (int i = 0; i < ic.Items.Count; i++)
			//{
			//	CheckBox cb = ic.Items[i] as CheckBox;
			//	if (cb != null)
			//	{
			//		cb.Tag = 10+i;
			//	}
			//}
		}
		private void DisableLabel(Int32 idx, bool value)
		{
			Label label = null;
			TextBox tb = null;
			switch (idx / 10)
			{
				//case 0:
				//	label = BackPrices.Items[idx] as Label;
				//	label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
				//	tb = BackStakes.Items[idx] as TextBox;
				//	if (tb != null) tb.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
				//	break;
				//case 1:
				//	label = LayPrices.Items[idx-10] as Label;
				//	label.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
				//	tb = LayStakes.Items[idx-10] as TextBox;
				//	tb.Foreground = new SolidColorBrush(value == true ? Colors.Black : Colors.LightGray);
				//	break;
			}
		}
		private void CheckBox_Click(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			Int32 tag = Convert.ToInt32(cb.Tag);
			if (tag == 0 || tag == 10)
			{
				for (int i = 1; i < 10; i++)
				{
					DisableLabel(tag + i, cb.IsChecked == true);
				}
			}
			else
			{
				DisableLabel(tag, cb.IsChecked == true);
			}
		}
	}
}
