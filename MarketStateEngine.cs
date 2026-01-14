using BetfairAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpreadTrader
{
	public sealed class MarketTelemetry
	{
		public double BackBook { get; set; }
		public double LayBook { get; set; }
		public double TotalMatched { get; set; }
		public double PnL { get; set; }
		public marketStatusEnum Status { get; set; }
	}

	internal class MarketStateEngine
    {
		public event Action<MarketTelemetry> TelemetryAvailable;
		//private Market _market;
		private BetfairAPI.BetfairAPI _bf = MainWindow.Betfair;
		private CancellationTokenSource _cts = new CancellationTokenSource();

		void PushDiffsToUi()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				//foreach (var d in diffs)
				//{
				//	var cell = _cells[d.RunnerId, d.Side, d.Level];
				//	cell.Update(cell.price, d.NewSize, d.NewTraded);
				//}
			});
		}
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
							Status = book.status,
							//PnL = pnl.Sum(x => x.profit)
						};

						TelemetryAvailable?.Invoke(telemetry);


						//Application.Current.Dispatcher.Invoke(() =>
						//{
						//	_backbook = book.BackBook;
						//	_laybook = book.LayBook;
						//	_totalMatched = book.totalMatched;
						//	_status = book.status;
						//	//PnL = pnl.Sum(x => x.profit);
						//});
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}

				await Task.Delay(2000, token);
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
