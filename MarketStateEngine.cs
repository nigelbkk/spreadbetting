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
		private async Task OrderBookProcessingLoop(Market _market, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (_market != null)
					{
						List<MarketProfitAndLoss> pnl = _bf.listMarketProfitAndLoss(_market.marketId);
						MarketBook book = _bf.GetMarketBook(_market);

						var telemetry = new MarketTelemetry
						{
							BackBook = book.BackBook,
							LayBook = book.LayBook,
							TotalMatched = book.totalMatched,
							ProfitAndLosses = pnl.Count > 0 ? pnl[0].profitAndLosses : new List<RunnerProfitAndLoss>()
						};
						TelemetryAvailable?.Invoke(telemetry);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}

				await Task.Delay(500, token);
			}
		}
		public void Start(Market market)
		{
			_ = Task.Run(() => OrderBookProcessingLoop(market, _cts.Token));
		}
		public void Stop()
		{
			_cts.Cancel();
		}
	}
}
