using System;
using System.Collections.Generic;

namespace SpreadTrader
{
	public class BetfairPrices
	{
		private List<double> AllPrices = null;
		public BetfairPrices()
		{
			if (AllPrices == null)
			{
				double[] MinValue = { 1.01, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
				double[] MaxValue = { 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
				Decimal[] Increment = { 0.01M, 0.02M, 0.05M, 0.1M, 0.2M, 0.5M, 1, 2, 5, 10 };

				AllPrices = new List<double>();
				Decimal m = 1.0M;
				for (Int32 idx = 0; idx < MinValue.Length; idx++)
				{
					while (m < (Decimal)MaxValue[idx])
					{
						m += Increment[idx];
						AllPrices.Add((double)m);
					}
				}
			}
		}
		public Int32 Index(double v)
		{
			v = BetfairAPI.BetfairAPI.BetfairPrice(v);
			for (int i = 0; i < AllPrices.Count; i++)
			{
				if (AllPrices[i] == v)
				{
					return i;
				}
			}
			return 1;
		}
		public double Previous(double v)
		{
			if (v <= 1.01) return 1.01;
			return (double)AllPrices[Index(v) - 1];
		}
		public double Next(double v)
		{
			if (v >= 1000) return 1000;
			return (double)AllPrices[Index(v) + 1];
		}
		public double this[int i]
		{
			get => AllPrices[Math.Max(0, Math.Min(AllPrices.Count, i))];
		}
	}
}
