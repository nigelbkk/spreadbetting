using Betfair.ESASwagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamSimulator.Synthetic
{
	/// <summary>
	/// Builds synthetic OrderMarketChange sequences for replay through
	/// WebSocketsHub.Instance.Simulate().
	///
	/// Use the real marketId/selectionId/betId from your recording so
	/// BetsManager recognises the order.
	///
	/// Example:
	///   var seq = new SequenceBuilder("1.256685911", selectionId: 59497577)
	///       .SubImage   ("425389599231", side: Order.SideEnum.L, price: 1.01, size: 2.0)
	///       .DelayMs(0.4)
	///       .PartialFill("425389599231", side: Order.SideEnum.L, price: 1.01, size: 2.0, sm: 1.0, sr: 1.0)
	///       .DelayMs(0.3)
	///       .FullMatch  ("425389599231", side: Order.SideEnum.L, price: 1.01, size: 2.0)
	///       .Build();
	/// </summary>
	public class SequenceBuilder
	{
		private readonly string _marketId;
		private readonly long _selectionId;
		private readonly List<SequenceEntry> _entries = new List<SequenceEntry>();

		private long _pt;

		public SequenceBuilder(string marketId, long selectionId, long startPtMs = 0)
		{
			_marketId = marketId;
			_selectionId = selectionId;
			_pt = startPtMs > 0 ? startPtMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}

		public static List<SequenceEntry> BuildRandom( string marketId, long selectionId, int messageCount, int seed = 0)
		{
			var rng = seed == 0 ? new Random() : new Random(seed);

			var builder = new SequenceBuilder(marketId, selectionId);

			var active = new Dictionary<string, (double price, double size, double matched, Order.SideEnum side)>();
			long betCounter = 1000;

			for (int i = 0; i < messageCount; i++)
			{
				// pick action
				bool createNew = active.Count == 0 || rng.NextDouble() < 0.4;

				if (createNew)
				{
					var betId = (++betCounter).ToString();
					var price = Math.Round(1.01 + rng.NextDouble() * 5, 2);
					var size = Math.Round(1 + rng.NextDouble() * 10, 2);
					var side = rng.Next(2) == 0 ? Order.SideEnum.L : Order.SideEnum.B;

					active[betId] = (price, size, 0, side);

					builder.SubImage(betId, side, price, size);
				}
				else
				{
					// pick existing
					var idx = rng.Next(active.Count);
					var kv = active.ElementAt(idx);

					var betId = kv.Key;
					var (price, size, matched, side) = kv.Value;

					var roll = rng.NextDouble();
					SimOrder o = new SimOrder()
					{
						marketId = marketId,
						selectionId = selectionId,
						Price = price,
						Size = size,
						Matched = matched,
						Side = side
					};

					if (roll < 0.5) // partial
					{
						var fill = Math.Min(size - matched, Math.Round(rng.NextDouble() * size * 0.5, 2));
						matched += fill;

						var remaining = size - matched;

						o = new SimOrder
						{
							BetId = betId,
							marketId = marketId,
							selectionId = selectionId,
							Price = price,
							Size = size,
							Matched = matched,
							Side = side
						}; 
						
						active[betId] = (price, size, matched, side);

						builder.PartialFill(o, matched, remaining);
					}
					else if (roll < 0.8) // full
					{
						builder.FullMatch(o);
						active.Remove(betId);
					}
					else // cancel
					{
						builder.Cancelled(o);
						active.Remove(betId);
					}
				}

				// small jitter
				builder.DelayMs(rng.NextDouble() * 2.0);
			}

			return builder.Build();
		}

		// ── Message builders ─────────────────────────────────────────────────

		/// <summary>First message after a bet is placed. fullImage = true, sm = 0, sr = size.</summary>
		public SequenceBuilder SubImage( string betId, Order.SideEnum side, double price, double size)
		{
			return AddEntry(betId, side, price, size, sm: 0, sr: size, sc: 0, fullImage: true);
		}

		/// <summary>Partial fill — pass cumulative sm/sr values.</summary>
		public SequenceBuilder PartialFill(SimOrder so, double sm, double sr)
		{
			return AddEntry(so, sm, sr, sc: 0, fullImage: false);
		}

		/// <summary>Order fully matched — sr == 0, sm == size.</summary>
		public SequenceBuilder FullMatch(SimOrder so)
		{
			return AddEntry(so, sm: so.Size, sr: 0, sc: 0, fullImage: false);
		}

		/// <summary>Order cancelled — sc == cancelled amount, sr == 0.</summary>
		public SequenceBuilder Cancelled(SimOrder so)
		{
			return AddEntry(so, sm: so.Matched, sr: 0, sc: so.Size - so.Matched, fullImage: false);
		}

		// ── Timing ───────────────────────────────────────────────────────────

		/// <summary>Delay before the next message. Sub-millisecond values supported.</summary>
		public SequenceBuilder DelayMs(double ms)
		{
			if (ms > 0)
				_entries.Add(new SequenceEntry { Kind = EntryKind.Delay, DelayMs = ms });
			return this;
		}

		public List<SequenceEntry> Build()
		{
			return _entries;
		}

		// ── Private ──────────────────────────────────────────────────────────

		private SequenceBuilder AddEntry( SimOrder so, double sm, double sr, double sc, bool fullImage) {
			return AddEntry( so.BetId, so.Side, so.Price, so.Size, sm, sr, sc, fullImage);
		}
		private SequenceBuilder AddEntry( string betId, Order.SideEnum side, double price, double size, double sm, double sr, double sc, bool fullImage)
		{
			_pt++;

			var order = new Order( Side: side, Pt: Order.PtEnum.L, Ot: Order.OtEnum.L, Status: Order.StatusEnum.E, Id: betId, P: price, S: size, Sm: sm, Sr: sr, Sc: sc, Sl: 0.0, Sv: 0.0, Pd: _pt, Rc: "REG_GGC", Rac: "" );
			var orc = new OrderRunnerChange( Id: _selectionId, FullImage: fullImage, Uo: new List<Order> { order } );
			var change = new OrderMarketChange( Id: _marketId, Orc: new List<OrderRunnerChange> { orc } );
			_entries.Add(new SequenceEntry { Kind = EntryKind.Message, Change = change, Pt = _pt });

			return this;
		}
	}

	// ── Supporting types ─────────────────────────────────────────────────────

	public enum EntryKind { Message, Delay }

	public class SequenceEntry
	{
		public EntryKind Kind { get; set; }
		public OrderMarketChange Change { get; set; }  // null for Delay
		public long Pt { get; set; }  // 0 for Delay
		public double DelayMs { get; set; }  // only for Delay
	}
}