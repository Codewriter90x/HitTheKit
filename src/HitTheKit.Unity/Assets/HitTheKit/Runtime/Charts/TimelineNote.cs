using System;
using HitTheKit.Core;

namespace HitTheKit.Unity.Charts
{
    public sealed class TimelineNote
    {
        internal TimelineNote(ChartNote note, int originalIndex, double effectiveTimeSeconds)
        {
            Note = note ?? throw new ArgumentNullException(nameof(note));
            OriginalIndex = originalIndex;
            EffectiveTimeSeconds = effectiveTimeSeconds;
        }

        public ChartNote Note { get; }
        public int OriginalIndex { get; }
        public double EffectiveTimeSeconds { get; }
    }
}
