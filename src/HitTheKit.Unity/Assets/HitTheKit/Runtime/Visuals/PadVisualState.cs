using System;
using HitTheKit.Unity.Charts;

namespace HitTheKit.Unity.Visuals
{
    public readonly struct PadVisualState
    {
        public static PadVisualState Inactive => new PadVisualState(null, 0f);

        public PadVisualState(TimelineNote nextNote, float intensity)
        {
            if (float.IsNaN(intensity) || float.IsInfinity(intensity))
            {
                throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be finite.");
            }

            NextNote = nextNote;
            Intensity = intensity;
        }

        public bool IsActive => NextNote != null;
        public float Intensity { get; }
        public TimelineNote NextNote { get; }
        public double? NextNoteEffectiveTimeSeconds => NextNote?.EffectiveTimeSeconds;
    }
}
