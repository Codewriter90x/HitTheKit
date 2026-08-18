using System;
using System.Collections.Generic;

namespace HitTheKit.Core
{
    public sealed class PadPerformanceSnapshot
    {
        internal PadPerformanceSnapshot(DrumPad pad, int resolved, int successful, int early, int late, int miss)
        {
            Pad = pad;
            Resolved = resolved;
            Successful = successful;
            Early = early;
            Late = late;
            Miss = miss;
        }

        public DrumPad Pad { get; }
        public int Resolved { get; }
        public int Successful { get; }
        public int Early { get; }
        public int Late { get; }
        public int Miss { get; }
        public double Accuracy => Resolved == 0 ? 0 : Successful * 100d / Resolved;
    }

    public sealed class PracticePerformanceAnalyzer
    {
        private readonly Dictionary<DrumPad, MutablePadPerformance> pads = new Dictionary<DrumPad, MutablePadPerformance>();

        public int EarlyCount { get; private set; }
        public int LateCount { get; private set; }

        public void Record(DrumPad pad, HitGrade grade)
        {
            if (!Enum.IsDefined(typeof(DrumPad), pad)) throw new ArgumentOutOfRangeException(nameof(pad));
            if (!Enum.IsDefined(typeof(HitGrade), grade)) throw new ArgumentOutOfRangeException(nameof(grade));
            if (!pads.TryGetValue(pad, out MutablePadPerformance value))
            {
                value = new MutablePadPerformance();
                pads.Add(pad, value);
            }

            value.Resolved++;
            if (grade != HitGrade.Miss) value.Successful++;
            if (grade == HitGrade.Early) { value.Early++; EarlyCount++; }
            if (grade == HitGrade.Late) { value.Late++; LateCount++; }
            if (grade == HitGrade.Miss) value.Miss++;
        }

        public PadPerformanceSnapshot For(DrumPad pad)
        {
            if (!pads.TryGetValue(pad, out MutablePadPerformance value))
                return new PadPerformanceSnapshot(pad, 0, 0, 0, 0, 0);
            return Snapshot(pad, value);
        }

        public PadPerformanceSnapshot? WeakestPad()
        {
            PadPerformanceSnapshot? weakest = null;
            foreach (KeyValuePair<DrumPad, MutablePadPerformance> pair in pads)
            {
                PadPerformanceSnapshot candidate = Snapshot(pair.Key, pair.Value);
                if (candidate.Resolved > 0 &&
                    (weakest == null || candidate.Accuracy < weakest.Accuracy ||
                     (Math.Abs(candidate.Accuracy - weakest.Accuracy) < 0.0001 && candidate.Resolved > weakest.Resolved)))
                    weakest = candidate;
            }
            return weakest;
        }

        public void Reset()
        {
            pads.Clear();
            EarlyCount = 0;
            LateCount = 0;
        }

        private static PadPerformanceSnapshot Snapshot(DrumPad pad, MutablePadPerformance value) =>
            new PadPerformanceSnapshot(pad, value.Resolved, value.Successful, value.Early, value.Late, value.Miss);

        private sealed class MutablePadPerformance
        {
            public int Resolved;
            public int Successful;
            public int Early;
            public int Late;
            public int Miss;
        }
    }
}
