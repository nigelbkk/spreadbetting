using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace SpreadTrader.Simulator
{
	public static class OcmDiagnostics
	{
		public static long MessagesReceived;
		public static long MessagesProcessed;
		public static long PdOutOfOrder;
		public static long PdDuplicate;
		public static long? LastPd;

		private static readonly ConcurrentDictionary<string, long> _lastPd = new ConcurrentDictionary<string, long>();
		private static readonly ConcurrentDictionary<string, int> _lastThread = new ConcurrentDictionary<string, int>();
		private static ConcurrentDictionary<(string, long), long> _created = new ConcurrentDictionary<(string, long), long>();

		public static void ApplyOcmUpdate(string betId, long pd, int threadId)
		{
			var key = (betId, pd);
			_created[key] = Stopwatch.GetTimestamp();
			var lastPd = _lastPd.GetOrAdd(betId, -1);

			if (lastPd != -1)
			{
				if (pd < lastPd)
				{
					Interlocked.Increment(ref PdOutOfOrder);
					Debug.WriteLine($"[PD-OUT_OF_ORDER] BetId={betId} Pd={pd} LastPd={lastPd} Thread={threadId}");
				}
				else if (pd == lastPd)
				{
					Interlocked.Increment(ref PdDuplicate);
					Debug.WriteLine($"[PD-DUPLICATE] BetId={betId} Pd={pd} Thread={threadId}");
				}
				if (pd < lastPd)
				{
					Interlocked.Increment(ref PdOutOfOrder);

					Debug.WriteLine($"[DROP-STALE] BetId={betId} Pd={pd} LastPd={lastPd}");

					return; // 🔥 IGNORE stale update
				}
			}

			_lastPd[betId] = pd;

			// thread tracking
			var lastThread = _lastThread.GetOrAdd(betId, threadId);

			if (lastThread != threadId)
			{
				Debug.WriteLine($"[THREAD-SWITCH] BetId={betId} Pd={pd} From={lastThread} To={threadId}");
			}

			_lastThread[betId] = threadId;

			Interlocked.Increment(ref MessagesProcessed);
		}

		public static void MeasureLatency(string betId, long pd)
		{
			if (_created.TryGetValue((betId, pd), out var created))
			{
				var now = Stopwatch.GetTimestamp();
				var latencyMs = (now - created) * 1000.0 / Stopwatch.Frequency;

				if (latencyMs > 50)
				{
					Debug.WriteLine(
						$"[LATENCY] {latencyMs:F1}ms BetId={betId} Pd={pd}"
					);
				}

				_created.TryRemove((betId, pd), out _);
			}
		}

		public static void Clear()
		{
			MessagesReceived = 0;
			MessagesProcessed = 0;
			PdOutOfOrder = 0;
			PdDuplicate = 0;
			LastPd = 0;
		}

		public static void Dump()
		{
			Debug.WriteLine(
				$"[OCM] Recv={MessagesReceived} Proc={MessagesProcessed} " +
				$"OutOfOrder={PdOutOfOrder} Dup={PdDuplicate}"
			);
		}
	}
}
