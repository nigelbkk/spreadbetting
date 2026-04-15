using Betfair.ESASwagger.Model;
using SpreadTrader;
using StreamSimulator.Synthetic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Replays OrderMarketChange sequences through WebSocketsHub.Instance.Simulate()
    /// so BetsManager receives identical input to the live case.
    ///
    /// Three sources:
    ///   1. Recorded .jsonl files (from StreamRecorder)
    ///   2. Synthetic sequences (from SequenceBuilder)
    ///   3. Mixed: recorded file with synthetic burst injected at a named market
    /// </summary>
    public class SimulatedStream
    {
        /// <summary>
        /// Called for every replayed change. Wire this to WebSocketsHub.Instance.Simulate:
        ///   sim.OnChange = (change) => WebSocketsHub.Instance.Simulate(change);
        /// Or call manager.OnOrderChanged directly if you want to bypass hub routing.
        /// </summary>
        public Action<OrderMarketChange> OnChange { get; set; }

        private readonly ReplayMode _mode;
        private readonly int _iterations;
        private readonly double _speedMultiplier;
        private const double SpinThresholdMs = 2.0;

        // ── Stats ────────────────────────────────────────────────────────────

        public long MessagesDispatched { get; private set; }
        public TimeSpan TotalElapsed { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────

        public event EventHandler<MessageDispatchedEventArgs> MessageDispatched;
        public event EventHandler<SimulationCompleteEventArgs> SimulationComplete;

        // ── Constructor ──────────────────────────────────────────────────────

        public SimulatedStream( ReplayMode mode = ReplayMode.PtAccurate, int iterations = 1, double speedMultiplier = 1.0)
        {
            _mode = mode;
            _iterations = iterations;
            _speedMultiplier = speedMultiplier;
        }

        // ── Mapping ─────────────────────────────────────────────────────

        MarketSnapshot _marketSnapshot;

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

		// ── Entry points ─────────────────────────────────────────────────────

		/// <summary>Replay a synthetic sequence from SequenceBuilder.</summary>
		public Task ReplaySyntheticAsync(String marketId, long selectionId, CancellationToken ct = default(CancellationToken))
        {
			List<SequenceEntry> sequence = SequenceBuilder.BuildRandom(marketId, selectionId, messageCount: 10000);

			//WebSocketsHub.Instance.Simulate(seq);

			return RunAsync(FromSynthetic(sequence), ct);
        }

        // ── Core loop ────────────────────────────────────────────────────────

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

        // ── Timing ───────────────────────────────────────────────────────────

        private async Task DelayAsync( ReplayEntry entry, long lastPt, long lastWallClockMs, CancellationToken ct)
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

        // ── Synthetic conversion ─────────────────────────────────────────────

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

        // ── Mixed merge ──────────────────────────────────────────────────────

        private static List<ReplayEntry> Merge( List<ReplayEntry> recorded, string triggerMarketId, List<ReplayEntry> injection) {
            var result = new List<ReplayEntry>();
            var injected = false;

            foreach (var entry in recorded)
            {
                result.Add(entry);

                if (!injected && entry.Change != null && entry.Change.Id == triggerMarketId)
                {
                    result.AddRange(injection);
                    injected = true;
                }
            }

            return result;
        }
    }

	// ── Supporting types ─────────────────────────────────────────────────────

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
