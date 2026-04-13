//using System;
//using System.Collections.Generic;
//using Betfair.ESASwagger.Model;

//namespace StreamSimulator.Synthetic
//{
//    /// <summary>
//    /// Builds synthetic OrderMarketChange sequences for replay through
//    /// WebSocketsHub.Instance.Simulate().
//    ///
//    /// Use the real marketId/selectionId/betId from your recording so
//    /// BetsManager recognises the order.
//    ///
//    /// Example:
//    ///   var seq = new SequenceBuilder("1.256685911", selectionId: 59497577)
//    ///       .SubImage   ("425389599231", side: "L", price: 1.01, size: 2.0)
//    ///       .DelayMs(0.4)
//    ///       .PartialFill("425389599231", side: "L", price: 1.01, size: 2.0, sm: 1.0, sr: 1.0)
//    ///       .DelayMs(0.3)
//    ///       .FullMatch  ("425389599231", side: "L", price: 1.01, size: 2.0)
//    ///       .Build();
//    /// </summary>
//    public class SequenceBuilder
//    {
//        private readonly string _marketId;
//        private readonly long   _selectionId;
//        private readonly List<SequenceEntry> _entries = new List<SequenceEntry>();

//        private long _pt;

//        public SequenceBuilder(string marketId, long selectionId, long startPtMs = 0)
//        {
//            _marketId    = marketId;
//            _selectionId = selectionId;
//            _pt          = startPtMs > 0 ? startPtMs
//                                         : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
//        }

//        // ── Message builders ─────────────────────────────────────────────────

//        /// <summary>First message after a bet is placed. fullImage = true, sm = 0, sr = size.</summary>
//        public SequenceBuilder SubImage(
//            string betId,
//            string side,
//            double price,
//            double size)
//        {
//            return AddEntry(betId, side, price, size, sm: 0, sr: size, sc: 0, fullImage: true);
//        }

//        /// <summary>Partial fill — pass cumulative sm/sr values.</summary>
//        public SequenceBuilder PartialFill(
//            string betId,
//            string side,
//            double price,
//            double size,
//            double sm,
//            double sr)
//        {
//            return AddEntry(betId, side, price, size, sm, sr, sc: 0, fullImage: false);
//        }

//        /// <summary>Order fully matched — sr == 0, sm == size.</summary>
//        public SequenceBuilder FullMatch(
//            string betId,
//            string side,
//            double price,
//            double size)
//        {
//            return AddEntry(betId, side, price, size, sm: size, sr: 0, sc: 0, fullImage: false);
//        }

//        /// <summary>Order cancelled — sc == cancelled amount, sr == 0.</summary>
//        public SequenceBuilder Cancelled(
//            string betId,
//            string side,
//            double price,
//            double size,
//            double smBeforeCancel = 0)
//        {
//            return AddEntry(betId, side, price, size,
//                            sm: smBeforeCancel,
//                            sr: 0,
//                            sc: size - smBeforeCancel,
//                            fullImage: false);
//        }

//        // ── Timing ───────────────────────────────────────────────────────────

//        /// <summary>Delay before the next message. Sub-millisecond values supported.</summary>
//        public SequenceBuilder DelayMs(double ms)
//        {
//            if (ms > 0)
//                _entries.Add(new SequenceEntry { Kind = EntryKind.Delay, DelayMs = ms });
//            return this;
//        }

//        public List<SequenceEntry> Build()
//        {
//            return _entries;
//        }

//        // ── Private ──────────────────────────────────────────────────────────

//        private SequenceBuilder AddEntry(
//            string betId,
//            string side,
//            double price,
//            double size,
//            double sm,
//            double sr,
//            double sc,
//            bool   fullImage)
//        {
//            _pt++;

//            var uo = new OrderUnmatchedEntry
//            {
//                Id     = betId,
//                Side   = side,
//                Pt     = "L",
//                Ot     = "L",
//                Status = "E",
//                Sv     = 0.0,
//                P      = price,
//                Sc     = sc,
//                Rc     = "REG_GGC",
//                S      = size,
//                Pd     = _pt,
//                Rac    = "",
//                Sl     = 0.0,
//                Sm     = sm,
//                Sr     = sr
//            };

//            var orc = new OrderRunnerChange
//            {
//                Id        = _selectionId,
//                FullImage = fullImage,
//                Uo        = new List<OrderUnmatchedEntry> { uo }
//            };

//            var change = new OrderMarketChange
//            {
//                Id  = _marketId,
//                Orc = new List<OrderRunnerChange> { orc }
//            };

//            _entries.Add(new SequenceEntry
//            {
//                Kind   = EntryKind.Message,
//                Change = change,
//                Pt     = _pt
//            });

//            return this;
//        }
//    }

//    // ── Supporting types ─────────────────────────────────────────────────────

//    public enum EntryKind { Message, Delay }

//    public class SequenceEntry
//    {
//        public EntryKind         Kind    { get; set; }
//        public OrderMarketChange Change  { get; set; }  // null for Delay
//        public long              Pt      { get; set; }  // 0 for Delay
//        public double            DelayMs { get; set; }  // only for Delay
//    }
//}
