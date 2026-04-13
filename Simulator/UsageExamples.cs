using System;
using System.Threading;
using System.Threading.Tasks;
using StreamSimulator;
using StreamSimulator.Recorder;
using StreamSimulator.Synthetic;

namespace StreamSimulator.Examples
{
    public static class UsageExamples
    {
        // ── 1. Recording ─────────────────────────────────────────────────────
        //
        // Add OnChangeRecorded hook to WebSocketsHub then Start/Stop the recorder
        // around a couple of small test bets. You just need the SUB_IMAGE.

        public static void StartRecording(string outputPath)
        {
            var recorder = new StreamRecorder(outputPath);
            recorder.Start();
            WebSocketsHub.Instance.OnChangeRecorded = recorder.Record;
        }

        public static void StopRecording()
        {
            WebSocketsHub.Instance.OnChangeRecorded = null;
            // recorder.Stop() called wherever you hold the recorder reference
        }


        // ── 2. Replay a recording ────────────────────────────────────────────
        //
        // Goes through WebSocketsHub.Simulate so BetsManager sees exactly
        // the same dispatch path as live messages.

        public static async Task ReplayRecordedSession(string recordedPath)
        {
            var sim = new SimulatedStream(
                mode:            ReplayMode.WallClockAccurate,
                iterations:      1,
                speedMultiplier: 1.0);

            sim.OnChange = (change) => WebSocketsHub.Instance.Simulate(change);

            sim.SimulationComplete += (sender, e) =>
                Debug.WriteLine(
                    "Replay done - " + e.TotalMessages + " messages in " +
                    e.Elapsed.TotalSeconds.ToString("F3") + "s");

            await sim.ReplayFileAsync(recordedPath);
        }


        // ── 3. Synthetic partial-fill burst ──────────────────────────────────
        //
        // Use real ids from your recording. Tweak DelayMs to find the threshold
        // that triggers the bug. Hammer with iterations to make it deterministic.

        public static async Task SyntheticBurst()
        {
            var seq = new SequenceBuilder(
                    marketId:    "1.256685911",   // from your recording
                    selectionId: 59497577)         // from your recording

                .SubImage   ("425389599231", side: "L", price: 1.01, size: 2.0)
                .DelayMs(0.4)
                .PartialFill("425389599231", side: "L", price: 1.01, size: 2.0, sm: 1.0, sr: 1.0)
                .DelayMs(0.3)
                .FullMatch  ("425389599231", side: "L", price: 1.01, size: 2.0)
                .Build();

            var sim = new SimulatedStream(
                mode:       ReplayMode.PtAccurate,
                iterations: 1000);

            sim.OnChange = (change) => WebSocketsHub.Instance.Simulate(change);

            sim.SimulationComplete += (sender, e) =>
                Debug.WriteLine(
                    "Stress done - " + e.TotalMessages + " in " +
                    e.Elapsed.TotalMilliseconds.ToString("F0") + "ms");

            await sim.ReplaySyntheticAsync(seq);
        }


        // ── 4. Mixed: recorded SUB_IMAGE + synthetic fills ───────────────────
        //
        // Real SUB_IMAGE gives BetsManager the correct initial state.
        // Synthetic fills let you control exactly what happens next.

        public static async Task MixedReplay(string recordedPath)
        {
            var injection = new SequenceBuilder("1.256685911", selectionId: 59497577)
                .PartialFill("425389599231", side: "L", price: 1.01, size: 2.0, sm: 1.0, sr: 1.0)
                .DelayMs(0.3)
                .PartialFill("425389599231", side: "L", price: 1.01, size: 2.0, sm: 1.5, sr: 0.5)
                .DelayMs(0.2)
                .FullMatch  ("425389599231", side: "L", price: 1.01, size: 2.0)
                .Build();

            var sim = new SimulatedStream(mode: ReplayMode.WallClockAccurate);

            sim.OnChange = (change) => WebSocketsHub.Instance.Simulate(change);

            await sim.ReplayMixedAsync(
                recordedPath:    recordedPath,
                triggerMarketId: "1.256685911",
                injection:       injection);
        }
    }
}
