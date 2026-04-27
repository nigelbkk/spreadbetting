using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Betfair.ESASwagger.Model;

namespace StreamSimulator.Recorder
{
    /// <summary>
    /// Records OrderMarketChange objects as they flow through WebSocketsHub
    /// to disk as a .jsonl file for later replay.
    ///
    /// Setup:
    ///   var recorder = new StreamRecorder(@"recordings\test_bets.jsonl");
    ///   recorder.Start();
    ///   WebSocketsHub.Instance.OnChangeRecorded = recorder.Record;
    ///
    ///   // ... place test bets ...
    ///
    ///   WebSocketsHub.Instance.OnChangeRecorded = null;
    ///   recorder.Stop();
    ///
    /// Output format (.jsonl) — one JSON object per line:
    ///   { "wallClockMs": 1713000000123, "payload": { ...OrderMarketChange... } }
    /// </summary>
    public class StreamRecorder
    {
        private readonly string _outputPath;
        private StreamWriter    _writer;

        public StreamRecorder()
        {
        }

        public void Start()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_outputPath) ?? ".");

            _writer = new StreamWriter(_outputPath, append: false, encoding: Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void Record(OrderMarketChange change)
        {
            if (_writer == null) return;

            var entry = new RecordedMessage
            {
                WallClockMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Payload     = change
            };

            lock (_writer)
            {
                _writer.WriteLine(JsonConvert.SerializeObject(entry));
            }
        }

        public void Stop()
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public class RecordedMessage
    {
        [JsonProperty("wallClockMs")]
        public long WallClockMs { get; set; }

        [JsonProperty("payload")]
        public OrderMarketChange Payload { get; set; }
    }
}

    //public class RecordedMessage
    //{
    //    [JsonProperty("wallClockMs")]
    //    public long WallClockMs { get; set; }

    //    /// <summary>Raw JSON string exactly as received from the hub</summary>
    //    [JsonProperty("payload")]
    //    public string Payload { get; set; }
    //}
//}
