using Betfair.ESASwagger.Model;
using Newtonsoft.Json.Linq;
using SpreadTrader;
using StreamSimulator.Synthetic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit;

namespace StreamSimulator
{
	public enum ReplayMode
	{
		/// <summary>Replay using pt delta. Reproduces Betfair-side publish cadence.</summary>
		PtAccurate,

		/// <summary>Replay using wall-clock arrival gaps. Closest to what your client saw.</summary>
		WallClockAccurate,

		/// <summary>No delay. Maximum throughput stress test.</summary>
		Burst,
	}

	public class SimulatedStream
	{
		public Action<OrderMarketChange> OnChange { get; set; }
		private MarketSnapshot _marketSnapshot;
		private CancellationTokenSource cts = new CancellationTokenSource();
		public static Dictionary<string, SimOrder> _orders;

		private readonly ReplayMode _mode;
		private readonly int _iterations;
		private readonly double _speedMultiplier;
		private const double SpinThresholdMs = 2.0;

		// ── Stats ────────────────────────────────────────────────────────────

		public long MessagesDispatched { get; private set; }
		public TimeSpan TotalElapsed { get; private set; }
		public event EventHandler<MessageDispatchedEventArgs> MessageDispatched;
		public event EventHandler<SimulationCompleteEventArgs> SimulationComplete;

		public SimulatedStream(ReplayMode mode = ReplayMode.PtAccurate, int iterations = 1, double speedMultiplier = 1.0)
		{
			_mode = mode;
			_iterations = iterations;
			_speedMultiplier = speedMultiplier;
			_orders = new Dictionary<string, SimOrder>();
		}

		// ── Sequence generators ─────────────────────────────────────────────────────

		private static List<SequenceEntry> BuildFullyMatchedSequence(string betId)		// needs work
		{
			var rng = new Random();

			if (!_orders.TryGetValue(betId, out var o))
				return new List<SequenceEntry>(); // or throw if you prefer

			var builder = new SequenceBuilder(o.marketId, o.selectionId);

			var newBetId = rng.Next(100000, 999999).ToString();

			var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
			var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

			double stake = Math.Round(1 + rng.NextDouble() * 5, 2);/////////////////

			return builder.Build();
		}

		private static List<SequenceEntry> BuildNewBetSequence(string marketId, long selectionId, double stake)
		{
			var rng = new Random();
			var builder = new SequenceBuilder(marketId, selectionId);

			var betId = rng.Next(100000, 999999).ToString();

			var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
			var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

			_orders[betId] = new SimOrder
			{
				marketId = marketId,
				selectionId = selectionId,
				BetId = betId,
				Price = price,
				Size = stake,
				Matched = 0,
				Side = side
			};

			builder.SubImage(betId, side, price, stake);
			return builder.Build();
		}

		private static List<SequenceEntry> BuildCancelSequence(string betId)
		{
			if (!_orders.TryGetValue(betId, out var o))
				return new List<SequenceEntry>(); // or throw if you prefer

			var builder = new SequenceBuilder(o.marketId, o.selectionId);

			builder.Cancelled(o);
			_orders.Remove(betId);

			return builder.Build();
		}

		private static List<SequenceEntry> BuildPartialMatchSequence(string betId, double fillAmount, double? matchPrice = null)
		{
			if (!_orders.TryGetValue(betId, out var o))
				return new List<SequenceEntry>();

			var builder = new SequenceBuilder(o.marketId, o.selectionId);

			var newMatched = Math.Min(o.Size, o.Matched + fillAmount);
			var actualFill = newMatched - o.Matched;
			var remaining = o.Size - newMatched;

			// ✅ record execution BEFORE updating state
			if (actualFill > 0)
			{
				var price = matchPrice ?? o.Price; // however you're sourcing it
				o.Matches.Add((price, actualFill));
			}

			// update state
			o.Matched = newMatched;

			builder.PartialFill(o, newMatched, remaining);

			// update state
			o.Matched = newMatched;

			// lifecycle check (optional here, usually not needed for partial)
			if (remaining == 0)
				_orders.Remove(betId);

			return builder.Build();
		}

		private static List<SequenceEntry> BuildRandomSequence(string marketId, long selectionId, int messageCount)
		{
			var rng = new Random();
			var sequence = new List<SequenceEntry>();
			var betIds = new List<string>();

			// --- 1. Create bets ---
			for (int i = 0; i < messageCount; i++)
			{
				var betId = rng.Next(100000, 999999).ToString();
				var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
				var stake = 25.0;
				var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

				// register state (ONLY place this happens for new bets)
				_orders[betId] = new SimOrder
				{
					BetId = betId,
					marketId = marketId,
					selectionId = selectionId,
					Price = price,
					Size = stake,
					Matched = 0,
					Side = side
				};

				var builder = new SequenceBuilder(marketId, selectionId);
				builder.SubImage(betId, side, price, stake);

				sequence.AddRange(builder.Build());
				betIds.Add(betId);
			}

			// --- 2. Apply random lifecycle ---
			foreach (var betId in betIds)
			{
				var roll = rng.NextDouble();

				if (roll < 0.4)
				{
					sequence.AddRange(BuildPartialMatchSequence(betId, rng.NextDouble() * 2));
				}
				else if (roll < 0.7)
				{
					sequence.AddRange(BuildFullyMatchedSequence(betId));
				}
				else
				{
					sequence.AddRange(BuildCancelSequence(betId));
				}
			}

			return sequence;
		}

		// ── Entry points ─────────────────────────────────────────────────────

		public void Stop()
		{
			cts.Cancel();
		}
		
		public void MapRealMarket(SpreadTrader.NodeViewModel nvm)
		{
			_marketSnapshot = new MarketSnapshot
			{
				MarketId = nvm.MarketID,
				Runners = nvm.LiveRunners
						.Select(r => new RunnerSnapshot
						{
							SelectionId = r.SelectionId,
							Name = r.Name // if available
						})
						.ToList()
			};
		}

		public Task SimulateNewBet(String marketId, long selectionId, double stake)
		{
			List<SequenceEntry> sequence = BuildNewBetSequence(marketId, selectionId, stake);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}

		public Task SimulateFull(String betId)
		{
			List<SequenceEntry> sequence = BuildFullyMatchedSequence(betId);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}

		public Task SimulatePartial(String betId, double fillAmount)
		{
			List<SequenceEntry> sequence = BuildPartialMatchSequence(betId, fillAmount);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}

		public Task SimulateCancel(String betId)
		{
			List<SequenceEntry> sequence = BuildCancelSequence(betId);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}
		
		public Task SimulateRandomBurst(String marketId, long selectionId, Int32 messageCount)
		{
			List<SequenceEntry> sequence = BuildRandomSequence(marketId, selectionId, messageCount);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}

		// ── Entry points ─────────────────────────────────────────────────────

		private Task ReplaySyntheticAsync(string marketId, long selectionId, CancellationToken ct = default)
		{
			var sequence = new List<SequenceEntry>();

			for (int i = 0; i < 10; i++)
			{
				var rng = new Random();
				var betId = rng.Next(100000, 999999).ToString();
				var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
				var size = Math.Round(1 + rng.NextDouble() * 10, 2);
				var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

				// register in your real state
				_orders[betId] = new SimOrder
				{
					BetId = betId,
					marketId = marketId,
					selectionId = selectionId,
					Price = price,
					Size = size,
					Matched = 0,
					Side = side
				};

				var builder = new SequenceBuilder(marketId, selectionId);
				builder.SubImage(betId, side, price, size);

				sequence.AddRange(builder.Build());
			}

			return RunAsync(FromSynthetic(sequence), ct);
		}
		

		private async Task RunAsync(List<ReplayEntry> entries, CancellationToken ct)
		{
			var sw = Stopwatch.StartNew();

			for (int iter = 0; iter < _iterations && !ct.IsCancellationRequested; iter++)
			{
				long lastPt = 0;
				long lastWallClockMs = 0;
				bool first = true;

				foreach (var entry in entries)
				{
					ct.ThrowIfCancellationRequested();

					if (!first)
						await DelayAsync(entry, lastPt, lastWallClockMs, ct);

					lastPt = entry.Pt;
					lastWallClockMs = entry.WallClockMs;
					first = false;

					if (OnChange != null)
					{
						OnChange(entry.Change);
						MessagesDispatched++;

						var handler = MessageDispatched;
						if (handler != null)
							handler(this, new MessageDispatchedEventArgs(entry, iter));
					}
				}
			}

			sw.Stop();
			TotalElapsed = sw.Elapsed;

			var completeHandler = SimulationComplete;
			if (completeHandler != null)
				completeHandler(this, new SimulationCompleteEventArgs(MessagesDispatched, TotalElapsed));
		}

		private async Task DelayAsync(ReplayEntry entry, long lastPt, long lastWallClockMs, CancellationToken ct)
		{
			double rawDelayMs;

			if (_mode == ReplayMode.PtAccurate)
			{
				rawDelayMs = entry.Pt > 0 && lastPt > 0
					? (double)(entry.Pt - lastPt)
					: 0;
			}
			else if (_mode == ReplayMode.WallClockAccurate)
			{
				rawDelayMs = entry.WallClockMs > 0 && lastWallClockMs > 0
					? (double)(entry.WallClockMs - lastWallClockMs)
					: 0;
			}
			else
			{
				rawDelayMs = 0;  // Burst
			}

			var delayMs = rawDelayMs / _speedMultiplier;
			if (delayMs <= 0) return;

			if (delayMs >= SpinThresholdMs)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
			}
			else
			{
				var ticksNeeded = (long)(delayMs / 1000.0 * Stopwatch.Frequency);
				var deadline = Stopwatch.GetTimestamp() + ticksNeeded;
				while (Stopwatch.GetTimestamp() < deadline)
					Thread.SpinWait(1);
			}
		}

		private static List<ReplayEntry> FromSynthetic(List<SequenceEntry> seq)
		{
			var entries = new List<ReplayEntry>();
			var runningPt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			var accumulatedDelayMs = 0.0;

			foreach (var s in seq)
			{
				if (s.Kind == EntryKind.Delay)
				{
					accumulatedDelayMs += s.DelayMs;
					runningPt += (long)Math.Ceiling(s.DelayMs);
					continue;
				}

				entries.Add(new ReplayEntry
				{
					Change = s.Change,
					Pt = s.Pt > 0 ? s.Pt : runningPt,
					WallClockMs = runningPt,
					IsSynthetic = true,
					DelayMsFromPrior = accumulatedDelayMs
				});
				runningPt = s.Pt > 0 ? s.Pt : runningPt;
				accumulatedDelayMs = 0;
			}
			return entries;
		}

		// ── Supporting types ─────────────────────────────────────────────────────

		//public class SimOrder
		//{
		//	public string marketId;
		//	public long selectionId;
		//	public string BetId;
		//	public double Price;
		//	public double Size;
		//	public double Matched;
		//	public Order.SideEnum Side;
		//}

		public class ReplayEntry
		{
			public OrderMarketChange Change { get; set; }
			public long Pt { get; set; }
			public long WallClockMs { get; set; }
			public bool IsSynthetic { get; set; }
			public double DelayMsFromPrior { get; set; }
		}

		public class MessageDispatchedEventArgs : EventArgs
		{
			public ReplayEntry Entry { get; }
			public int Iteration { get; }

			public MessageDispatchedEventArgs(ReplayEntry entry, int iteration)
			{
				Entry = entry;
				Iteration = iteration;
			}
		}

		public class SimulationCompleteEventArgs : EventArgs
		{
			public long TotalMessages { get; }
			public TimeSpan Elapsed { get; }

			public SimulationCompleteEventArgs(long totalMessages, TimeSpan elapsed)
			{
				TotalMessages = totalMessages;
				Elapsed = elapsed;
			}
		}

		class MarketSnapshot
		{
			public string MarketId { get; set; }
			public List<RunnerSnapshot> Runners { get; set; } = new List<RunnerSnapshot>();
		}

		class RunnerSnapshot
		{
			public long SelectionId { get; set; }
			public string Name { get; set; }   // optional but useful for logs

			public override string ToString()
			{
				return $"{Name} : {SelectionId}";
			}
		}
	}

	public class SimOrder
	{
		public string marketId;
		public long selectionId;
		public string BetId;

		public double Price;
		public double Size;
		public double Matched;

		public Order.SideEnum Side;
		
		public List<(double price, double size)> Matches { get; } = new List<(double price, double size)>();
	}

}