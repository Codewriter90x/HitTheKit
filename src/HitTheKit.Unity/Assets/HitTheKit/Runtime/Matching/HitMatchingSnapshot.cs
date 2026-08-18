using HitTheKit.Core;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Matching
{
    public sealed class HitMatchingSnapshot
    {
        internal HitMatchingSnapshot(
            DrumInputEvent? lastInput,
            HitResult lastResult,
            double? lastEventSongTimeSeconds,
            int perfectCount,
            int goodCount,
            int earlyCount,
            int lateCount,
            int missCount,
            int noMatchCount,
            int resolvedNoteCount,
            int totalNoteCount)
        {
            LastInput = lastInput;
            LastResult = lastResult;
            LastEventSongTimeSeconds = lastEventSongTimeSeconds;
            PerfectCount = perfectCount;
            GoodCount = goodCount;
            EarlyCount = earlyCount;
            LateCount = lateCount;
            MissCount = missCount;
            NoMatchCount = noMatchCount;
            ResolvedNoteCount = resolvedNoteCount;
            TotalNoteCount = totalNoteCount;
        }

        public DrumInputEvent? LastInput { get; }
        public HitResult LastResult { get; }
        public double? LastEventSongTimeSeconds { get; }
        public int PerfectCount { get; }
        public int GoodCount { get; }
        public int EarlyCount { get; }
        public int LateCount { get; }
        public int MissCount { get; }
        public int NoMatchCount { get; }
        public int ResolvedNoteCount { get; }
        public int TotalNoteCount { get; }
        public bool IsComplete => ResolvedNoteCount == TotalNoteCount;
    }
}
