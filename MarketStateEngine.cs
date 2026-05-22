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
		private int _pnlLoopRunning;
		private int _bookLoopRunning;
		private readonly Guid _engineId = Guid.NewGuid();
		private Task _pnlTask;
		private Task _bookTask;
		private async Task PNLProcessingLoop(Market _market, CancellationToken token)
		{
			Debug.WriteLine($"PNL LOOP START {_market.marketId} : {_engineId}");
			if (Interlocked.Exchange(ref _pnlLoopRunning, 1) == 1)
			{
				Debug.WriteLine($"PNL LOOP ALREADY RUNNING {_market?.marketId}");
				return;
			}
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
				catch (OperationCanceledException)
				{
					Debug.WriteLine($"PNL LOOP CANCELLED {_engineId}");
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[PNL LOOP ERROR] {ex}");
				}
				finally
				{
					_pnlLoopRunning = 0;
				}
				await Task.Delay(Props.props.PNLFrequency, token);
			}
			Debug.WriteLine($"PNL LOOP ENDS {_market.marketId} : {_engineId}");
		}
		private async Task OrderBookProcessingLoop(Market _market, CancellationToken token)
		{
			Debug.WriteLine($"BOOK LOOP START {_market.marketId} : {_engineId}");
			if (Interlocked.Exchange(ref _bookLoopRunning, 1) == 1)
			{
				Debug.WriteLine($"BOOK LOOP ALREADY RUNNING {_market?.marketId}");
				return;
			} 
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
						var handler = TelemetryAvailable;				// Decouple downstream code from task loop

						if (handler != null)
						{
							try
							{
								handler(telemetry);
							}
							catch (Exception ex)
							{
								Debug.WriteLine($"Telemetry handler error: {ex}");
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					Debug.WriteLine($"BOOK LOOP CANCELLED {_engineId}");
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[BOOK LOOP ERROR] {ex}");
				}
				finally
				{
					_bookLoopRunning = 0;
				}
				await Task.Delay(Props.props.BookFrequency*1000, token);
			}
			Debug.WriteLine($"BOOK LOOP ENDS {_market.marketId} : {_engineId}");
		}
		public void Start(Market market)
		{
			_bookTask = Task.Run(() => OrderBookProcessingLoop(market, _cts.Token));
			_pnlTask = Task.Run(() => PNLProcessingLoop(market, _cts.Token));
		}
		public async Task Stop()
		{
			_cts.Cancel();
			TelemetryAvailable = null;
			await Task.WhenAll( _pnlTask ?? Task.CompletedTask, _bookTask ?? Task.CompletedTask);
			_cts.Dispose();
		}
	}
}
