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
		private int _startStarted;
		private int _stopStarted;
		private async Task PNLProcessingLoop(Market _market, CancellationToken token)
		{
			HealthDiagnostics.MarkPnlLoopStart(_engineId);
			Debug.WriteLine($"PNL LOOP START {_market?.marketId} : {_engineId}");
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
							MarketId = _market.marketId,
							ProfitAndLosses = pnl?.FirstOrDefault()?.profitAndLosses?.ToList() ?? new List<RunnerProfitAndLoss>()
						};
						var handler = TelemetryAvailable;
						if (handler != null)
						{
							try { handler(telemetry); }
							catch (Exception handlerError) { Debug.WriteLine($"PNL telemetry handler error: {handlerError}"); }
						}
						HealthDiagnostics.MarkPnlSuccess(_engineId);
					}
					await Task.Delay(Props.props.PNLFrequency, token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					HealthDiagnostics.MarkPnlError(_engineId, ex);
					Debug.WriteLine($"[PNL LOOP ERROR] {ex}");
					// Yield before retrying a failed relay/API call; do not spin.
					try { await Task.Delay(Math.Max(1000, Props.props.PNLFrequency), token); }
					catch (OperationCanceledException) { break; }
				}
				finally
				{
					_pnlLoopRunning = 0;
				}
//				await Task.Delay(Props.props.PNLFrequency, token);
			}
			Debug.WriteLine($"PNL LOOP ENDS {_market?.marketId} : {_engineId}");
		}
		private async Task OrderBookProcessingLoop(Market _market, CancellationToken token)
		{
			HealthDiagnostics.MarkBookLoopStart(_engineId);
			Debug.WriteLine($"BOOK LOOP START {_market?.marketId} : {_engineId}");
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
						HealthDiagnostics.MarkBookSuccess(_engineId);
					}
					await Task.Delay(Props.props.BookFrequency * 1000, token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					HealthDiagnostics.MarkBookError(_engineId, ex);
					Debug.WriteLine($"[BOOK LOOP ERROR] {ex}");
				}
				finally
				{
					_bookLoopRunning = 0;
				}
//				await Task.Delay(Props.props.BookFrequency*1000, token);
			}
			Debug.WriteLine($"BOOK LOOP ENDS {_market?.marketId} : {_engineId}");
		}
		public void Start(Market market)
		{
			if (Volatile.Read(ref _stopStarted) != 0)
			{
				Debug.WriteLine($"MarketStateEngine start skipped after stop {_engineId}");
				return;
			}
			if (market == null)
			{
				Debug.WriteLine($"MarketStateEngine start skipped: market is null {_engineId}");
				return;
			}
			if (Interlocked.Exchange(ref _startStarted, 1) != 0)
			{
				Debug.WriteLine($"MarketStateEngine start skipped: already running {_engineId}");
				return;
			}
			HealthDiagnostics.MarkStarted(_engineId, market.marketId);

			_bookTask = Task.Run(() => OrderBookProcessingLoop(market, _cts.Token));
			_pnlTask = Task.Run(() => PNLProcessingLoop(market, _cts.Token));
		}
		public async Task Stop()
		{
			if (Interlocked.Exchange(ref _stopStarted, 1) != 0)
				return;
			try
			{
				_cts.Cancel();
				TelemetryAvailable = null;
				await Task.WhenAll( _pnlTask ?? Task.CompletedTask, _bookTask ?? Task.CompletedTask);
			}
			catch (ObjectDisposedException ex)
			{
				Debug.WriteLine($"MarketStateEngine already disposed {_engineId}: {ex.Message}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"MarketStateEngine stop failed {_engineId}: {ex}");
			}
			finally
			{
				HealthDiagnostics.MarkStopped(_engineId);
				_cts.Dispose();
			}
		}
	}
}
