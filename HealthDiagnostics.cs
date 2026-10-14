using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SpreadTrader
{
    internal sealed class StreamHealthSnapshot
    {
        public DateTime GeneratedUtc { get; set; }
        public bool Connected { get; set; }
        public string HubUri { get; set; }
        public DateTime? LastConnectedUtc { get; set; }
        public DateTime? LastDisconnectedUtc { get; set; }
        public DateTime? LastMessageUtc { get; set; }
        public DateTime? LastErrorUtc { get; set; }
        public string LastError { get; set; }
        public long ConnectAttempts { get; set; }
        public long Reconnects { get; set; }
        public long MessagesReceived { get; set; }
        public long MarketMessages { get; set; }
        public long OrderMessages { get; set; }
        public long InvalidMessages { get; set; }
        public long DroppedMarketMessages { get; set; }
        public long DroppedOrderMessages { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int MarketQueueCount { get; set; }
        public int OrderQueueCount { get; set; }
    }

    internal sealed class MarketEngineHealthSnapshot
    {
        public string EngineId { get; set; }
        public string MarketId { get; set; }
        public bool Started { get; set; }
        public bool Stopped { get; set; }
        public long PnlLoopStarts { get; set; }
        public long BookLoopStarts { get; set; }
        public long PnlSuccesses { get; set; }
        public long BookSuccesses { get; set; }
        public long PnlErrors { get; set; }
        public long BookErrors { get; set; }
        public DateTime? LastPnlUtc { get; set; }
        public DateTime? LastBookUtc { get; set; }
        public DateTime? LastErrorUtc { get; set; }
        public string LastError { get; set; }
    }

    internal sealed class SpreadTraderHealthDocument
    {
        public DateTime GeneratedUtc { get; set; }
        public int ProcessId { get; set; }
        public StreamHealthSnapshot Stream { get; set; }
        public List<MarketEngineHealthSnapshot> MarketEngines { get; set; }
    }

    internal static class HealthDiagnostics
    {
        private sealed class EngineState
        {
            internal readonly string EngineId;
            internal string MarketId;
            internal long Started;
            internal long Stopped;
            internal long PnlLoopStarts;
            internal long BookLoopStarts;
            internal long PnlSuccesses;
            internal long BookSuccesses;
            internal long PnlErrors;
            internal long BookErrors;
            internal long LastPnlTicks;
            internal long LastBookTicks;
            internal long LastErrorTicks;
            internal string LastError;

            internal EngineState(string engineId) { EngineId = engineId; }
        }

        private static readonly ConcurrentDictionary<string, EngineState> Engines = new ConcurrentDictionary<string, EngineState>();

        internal static void Register(Guid engineId, string marketId)
        {
            var state = Engines.GetOrAdd(engineId.ToString("N"), id => new EngineState(id));
            state.MarketId = marketId;
        }

        internal static void MarkStarted(Guid engineId, string marketId)
        {
            Register(engineId, marketId);
            Interlocked.Exchange(ref Engines[engineId.ToString("N")].Started, 1);
        }

        internal static void MarkStopped(Guid engineId)
        {
            if (Engines.TryGetValue(engineId.ToString("N"), out var state))
                Interlocked.Exchange(ref state.Stopped, 1);
        }

        internal static void MarkPnlLoopStart(Guid engineId) => Increment(engineId, state => Interlocked.Increment(ref state.PnlLoopStarts));
        internal static void MarkBookLoopStart(Guid engineId) => Increment(engineId, state => Interlocked.Increment(ref state.BookLoopStarts));
        internal static void MarkPnlSuccess(Guid engineId) => Increment(engineId, state => { Interlocked.Increment(ref state.PnlSuccesses); Interlocked.Exchange(ref state.LastPnlTicks, DateTime.UtcNow.Ticks); });
        internal static void MarkBookSuccess(Guid engineId) => Increment(engineId, state => { Interlocked.Increment(ref state.BookSuccesses); Interlocked.Exchange(ref state.LastBookTicks, DateTime.UtcNow.Ticks); });
        internal static void MarkPnlError(Guid engineId, Exception ex) => MarkError(engineId, ex, false);
        internal static void MarkBookError(Guid engineId, Exception ex) => MarkError(engineId, ex, true);

        private static void MarkError(Guid engineId, Exception ex, bool book)
        {
            Increment(engineId, state =>
            {
                if (book) Interlocked.Increment(ref state.BookErrors); else Interlocked.Increment(ref state.PnlErrors);
                Interlocked.Exchange(ref state.LastErrorTicks, DateTime.UtcNow.Ticks);
                state.LastError = ex?.Message;
            });
        }

        private static void Increment(Guid engineId, Action<EngineState> action)
        {
            if (Engines.TryGetValue(engineId.ToString("N"), out var state)) action(state);
        }

        internal static List<MarketEngineHealthSnapshot> Snapshot()
        {
            return Engines.Values.Select(state => new MarketEngineHealthSnapshot
            {
                EngineId = state.EngineId,
                MarketId = state.MarketId,
                Started = Volatile.Read(ref state.Started) != 0,
                Stopped = Volatile.Read(ref state.Stopped) != 0,
                PnlLoopStarts = Volatile.Read(ref state.PnlLoopStarts),
                BookLoopStarts = Volatile.Read(ref state.BookLoopStarts),
                PnlSuccesses = Volatile.Read(ref state.PnlSuccesses),
                BookSuccesses = Volatile.Read(ref state.BookSuccesses),
                PnlErrors = Volatile.Read(ref state.PnlErrors),
                BookErrors = Volatile.Read(ref state.BookErrors),
                LastPnlUtc = ToUtc(Volatile.Read(ref state.LastPnlTicks)),
                LastBookUtc = ToUtc(Volatile.Read(ref state.LastBookTicks)),
                LastErrorUtc = ToUtc(Volatile.Read(ref state.LastErrorTicks)),
                LastError = state.LastError
            }).ToList();
        }

        private static DateTime? ToUtc(long ticks) => ticks == 0 ? (DateTime?)null : new DateTime(ticks, DateTimeKind.Utc);

        internal static void Write(StreamHealthSnapshot stream)
        {
            try
            {
                var document = new SpreadTraderHealthDocument
                {
                    GeneratedUtc = DateTime.UtcNow,
                    ProcessId = Process.GetCurrentProcess().Id,
                    Stream = stream,
                    MarketEngines = Snapshot()
                };
                File.WriteAllText(@"C:\Temp\spreadtrader-health.json", JsonConvert.SerializeObject(document, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Health snapshot write failed: {ex.Message}");
            }
        }
    }
}
