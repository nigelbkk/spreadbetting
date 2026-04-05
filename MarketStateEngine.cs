using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static BetfairAPI.MarketProfitAndLoss;

namespace SpreadTrader
{
	public sealed class MarketTelemetry
	{
		public String MarketId { get; set; }
		public double BackBook { get; set; }
		public double LayBook { get; set; }
		public double TotalMatched { get; set; }
		public List<RunnerProfitAndLoss> ProfitAndLosses { get; set; }
	}

	internal class MarketStateEngine
    {
		public event Action<MarketTelemetry> TelemetryAvailable;
		private BetfairAPI.BetfairAPI _bf = MainWindow.Betfair;
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private async Task PNLProcessingLoop(Market _market, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (_market != null)
					{
						List<MarketProfitAndLoss> pnl = _bf.listMarketProfitAndLoss(_market.marketId);
						var telemetry = new MarketTelemetry
						{
							ProfitAndLosses = pnl?.FirstOrDefault()?.profitAndLosses?.ToList() ?? new List<RunnerProfitAndLoss>()
						};
						TelemetryAvailable?.Invoke(telemetry);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}

				await Task.Delay(Props.props.PNLFrequency, token);
			}
		}
		private async Task OrderBookProcessingLoop(Market _market, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (_market != null)
					{
						MarketBook book = _bf.GetMarketBook(_market);

						var telemetry = new MarketTelemetry
						{
							MarketId = book.marketId,
							BackBook = book.BackBook,
							LayBook = book.LayBook,
							TotalMatched = book.totalMatched,
							ProfitAndLosses = null
						};
						TelemetryAvailable?.Invoke(telemetry);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}

				await Task.Delay(Props.props.TotalMatchedFrequency*1000, token);
			}
		}
		public void Start(Market market)
		{
			_ = Task.Run(() => OrderBookProcessingLoop(market, _cts.Token));
			_ = Task.Run(() => PNLProcessingLoop(market, _cts.Token));
		}
		public void Stop()
		{
			_cts.Cancel();
			TelemetryAvailable = null;
		}
	}
}
