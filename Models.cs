using Betfair.ESASwagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpreadTrader
{
	public class MarketChangeDto
	{
		public string MarketId { get; set; }
		public DateTime Time { get; set; }
		public double? Tv { get; set; }

		public MarketDefinition.StatusEnum? Status { get; set; }
		public List<RunnerChangeDto> Runners { get; set; }
	}

	public class RunnerChangeDto
	{
		public long Id { get; set; }
		public double? Ltp { get; set; }
		public double? Tv { get; set; }
        public List<List<double?>> Trd { get; set; }
        public List<PriceLevelDto> Bdatb { get; set; }
		public List<PriceLevelDto> Bdatl { get; set; }
        public override string ToString()
        {
			return $"{Id} : {Bdatb} : {Bdatb}";
        }
	}

	public class PriceLevelDto
	{
		public int Level { get; set; }
		public double Price { get; set; }
		public double Size { get; set; }
        public override string ToString()
        {
            return $"{Level} : {Price} : {Size}";
        }
    }
    public class MarketSnapDto
	{
		public String MarketId { get; set; }
		public bool InPlay { get; set; }
		public DateTime Time { get; set; }
		public MarketDefinition.StatusEnum? Status { get; set; }
		public List<MarketRunnerSnapDto> Runners { get; set; }
	}
	public class MarketRunnerSnapDto
	{
		public long SelectionId { get; set; }
		public MarketRunnerPricesDto Prices { get; set; }
	}
	public class MarketRunnerPricesDto
	{
		public List<PriceDto> Back { get; set; }
		public List<PriceDto> Lay { get; set; }
	}
	public class PriceDto
	{
		public double Price { get; set; }
		public double Size { get; set; }
	}

}
