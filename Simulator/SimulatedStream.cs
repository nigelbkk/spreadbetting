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
		private static readonly Dictionary<string, (double price, double size, double matched, Order.SideEnum side)> _orders = new Dictionary<string, (double price, double size, double matched, Order.SideEnum side)>();

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

		// ── Entry points ─────────────────────────────────────────────────────

		public static List<SequenceEntry> NewRandom( string marketId, long selectionId, double stake)
		{
            var rng = new Random();
            var builder = new SequenceBuilder(marketId, selectionId);

            var betId = Guid.NewGuid().ToString("N");

            var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
            var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

            // 1. submit
            builder.SubImage(betId, side, price, stake);

            builder.DelayMs(rng.NextDouble() * 20);

            var roll = rng.NextDouble();

            if (roll < 0.4)
            {
                // partial → full
                var partial = Math.Round(stake * rng.NextDouble(), 2);
                var remaining = stake - partial;

                builder.PartialFill(betId, side, price, stake, partial, remaining);

                builder.DelayMs(rng.NextDouble() * 20);

                builder.FullMatch(betId, side, price, stake);
            }
            else if (roll < 0.7)
            {
                // straight full
                builder.FullMatch(betId, side, price, stake);
            }
            else
            {
                // cancel (maybe after partial)
                var partial = Math.Round(stake * rng.NextDouble() * 0.5, 2);

                if (partial > 0)
                {
                    builder.PartialFill(betId, side, price, stake, partial, stake - partial);
                    builder.DelayMs(rng.NextDouble() * 20);
                }

                builder.Cancelled(betId, side, price, stake, partial);
            }

            return builder.Build();
		}

		public static List<SequenceEntry> Cancel(string marketId, long selectionId, string betId)
        {
            if (!_orders.TryGetValue(betId, out var o))
                return new List<SequenceEntry>(); // or throw if you prefer

            var builder = new SequenceBuilder(marketId, selectionId);

            builder.Cancelled(betId, o.side, o.price, o.size, o.matched);

            _orders.Remove(betId);

            return builder.Build();
        }
        public static List<SequenceEntry> Partial(string marketId, long selectionId, string betId, double fillSize)
        {
            if (!_orders.TryGetValue(betId, out var o))
                return new List<SequenceEntry>();

            var newMatched = Math.Min(o.size, o.matched + fillSize);
            var remaining = o.size - newMatched;

            var builder = new SequenceBuilder(marketId, selectionId);

            builder.PartialFill(betId, o.side, o.price, o.size, newMatched, remaining);

            _orders[betId] = (o.price, o.size, newMatched, o.side);

            return builder.Build();
        }
        public static List<SequenceEntry> SingleShotRandom(string marketId, long selectionId, int seed = 0)
        {
            var rng = seed == 0 ? new Random() : new Random(seed);

            var builder = new SequenceBuilder(marketId, selectionId);

            var betId = rng.Next(100000, 999999).ToString();
            var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
            var size = Math.Round(1 + rng.NextDouble() * 10, 2);
            var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

            _orders[betId] = (price, size, 0, side);

            // 1. submit
            builder.SubImage(betId, side, price, size);

            builder.DelayMs(rng.NextDouble() * 50); // optional visible delay

            var roll = rng.NextDouble();

            if (roll < 0.4)
            {
                // partial then full
                var partial = Math.Round(size * rng.NextDouble(), 2);
                var remaining = size - partial;

                builder.PartialFill(betId, side, price, size, partial, remaining);
                builder.DelayMs(rng.NextDouble() * 50);

                builder.FullMatch(betId, side, price, size);
            }
            else if (roll < 0.7)
            {
                // straight full match
                builder.FullMatch(betId, side, price, size);
            }
            else
            {
                // cancel (maybe after partial)
                var partial = Math.Round(size * rng.NextDouble() * 0.5, 2);

                if (partial > 0)
                {
                    builder.PartialFill(betId, side, price, size, partial, size - partial);
                    builder.DelayMs(rng.NextDouble() * 50);
                }

                builder.Cancelled(betId, side, price, size, partial);
            }

            return builder.Build();
        }
		public static List<SequenceEntry> Full(string marketId, long selectionId, string betId)
		{
			if (!_orders.TryGetValue(betId, out var o))
				return new List<SequenceEntry>();

			var builder = new SequenceBuilder(marketId, selectionId);

			builder.FullMatch(betId, o.side, o.price, o.size);

			_orders.Remove(betId);

			return builder.Build();
		}
		public Task ReplayNew(String marketId, long selectionId, double stake)
		{
            List<SequenceEntry> sequence = NewRandom(marketId, selectionId, stake);
            return RunAsync(FromSynthetic(sequence), cts.Token);
        }
		public Task ReplaySingleShot(String marketId)
		{
			List<SequenceEntry> sequence = SingleShotRandom(marketId, 59497577);
			return RunAsync(FromSynthetic(sequence), cts.Token);
		}
		public Task ReplaySyntheticAsync(String marketId, long selectionId, CancellationToken ct = default(CancellationToken))
        {
            List<SequenceEntry> sequence = SequenceBuilder.BuildRandom(marketId, selectionId, messageCount: 10);
            return RunAsync(FromSynthetic(sequence), cts.Token);
        }
        public void Stop()
        {
            cts.Cancel();
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

}