using System;
using System.Collections.Generic;

namespace SpreadTrader
{
    public class BetfairPrices
    {
        public enum MatchTypeEnum
        {
            Nearest, Lower, Higher,
        }
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

        private static Double BetfairPrice(Double v, MatchTypeEnum Type)
        {
            double OriginalPrice = v;
            v = Math.Round(v, 2);
            if (v <= 1.01) return 1.01;
            if (v >= 1000) return 1000;

            Double[] MinValue = { 1.01, 1.20, 2, 3, 4, 6, 10, 20, 30, 50, 100 };
            Double[] MaxValue = { 1.20, 2, 3, 4, 6, 10, 20, 30, 50, 100, 1000 };
            Double[] Increment = { 0.001, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10 };

            Int32 idx = 0;
            for (; idx < MinValue.Length; idx++)
            {
                if (v >= MinValue[idx] && (idx == MinValue.Length - 1 ? v <= MaxValue[idx] : v < MaxValue[idx]))
                {
                    break;
                }
            }

            Double lo = Math.Floor(v / Increment[idx]) * Increment[idx];
            lo = Math.Round(lo, 2, MidpointRounding.AwayFromZero);

            if (lo == v)
                return v;

            Double hi = lo + Increment[idx];
            hi = Math.Round(hi, 2, MidpointRounding.AwayFromZero);

            if (lo == v)
            {
                return v;
            }
            //Double hi = lo + Increment[idx];
            switch (Type)
            {
                case MatchTypeEnum.Lower: return lo;
                case MatchTypeEnum.Nearest: return Math.Abs(lo - OriginalPrice) < Math.Abs(hi - OriginalPrice) ? lo : hi;
                case MatchTypeEnum.Higher: return hi;
            }
            return 0;
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
            double retval = BetfairPrice(v-0.01, MatchTypeEnum.Lower);
            return retval;
            //if (v <= 1.01) return 1.01;
            //return (double)AllPrices[Index(v) - 1];
        }
        public double Next(double v)
        {
            double retval = BetfairPrice(v+0.01, MatchTypeEnum.Higher);
            return retval;
            //if (v >= 1000) return 1000;
            //return (double)AllPrices[Index(v) + 1];
        }
        public double this[int i]
        {
            get => AllPrices[Math.Max(0, Math.Min(AllPrices.Count, i))];
        }
    }
}
