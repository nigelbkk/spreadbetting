using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BetfairAPI;

namespace SpreadTrader
{
	public delegate void SubmitBetsDelegate();
	public partial class BettingGrid : UserControl
	{
		private bool _BackActive { get; set; }
		public bool BackActive
		{
			get { return _BackActive; }
			set
			{
				_BackActive = value;
				foreach (PriceSize o in BackValues)
				{
					o.IsChecked = value;
				}
			}
		}
		private bool _LayActive { get; set; }
		public bool LayActive
		{
			get { return _LayActive; }
			set
			{
				_LayActive = value;
				foreach (PriceSize o in LayValues)
				{
					o.IsChecked = value;
				}
			}
		}
		public PriceSize[] BackValues { get; set; }
		public PriceSize[] LayValues { get; set; }
		public NodeViewModel MarketNode { get; set; }
		public BettingGrid()
		{
			BackValues = SliderControl.BackValues;
			LayValues = SliderControl.LayValues;
			for (int i = 0; i < 3; i++) BackValues[i].Color = Application.Current.FindResource("Lay0Color") as SolidColorBrush;
			for (int i = 3; i < 6; i++) BackValues[i].Color = Application.Current.FindResource("Lay1Color") as SolidColorBrush;
			for (int i = 6; i < 9; i++) BackValues[i].Color = Application.Current.FindResource("Lay2Color") as SolidColorBrush;
			for (int i = 0; i < 3; i++) LayValues[i].Color = Application.Current.FindResource("Back2Color") as SolidColorBrush;
			for (int i = 3; i < 6; i++) LayValues[i].Color = Application.Current.FindResource("Back1Color") as SolidColorBrush;
			for (int i = 6; i < 9; i++) LayValues[i].Color = Application.Current.FindResource("Back0Color") as SolidColorBrush;
			BackActive = LayActive = true;
			InitializeComponent();
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
	}
}