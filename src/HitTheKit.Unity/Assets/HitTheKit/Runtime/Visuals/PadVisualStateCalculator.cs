using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;

namespace HitTheKit.Unity.Visuals
{
    public sealed class PadVisualStateCalculator
    {
        public PadVisualState Calculate(
            DrumPad pad,
            double songPositionSeconds,
            double lookAheadSeconds,
            IReadOnlyList<TimelineNote> upcomingNotes)
        {
            if (upcomingNotes == null) throw new ArgumentNullException(nameof(upcomingNotes));
            EnsureFinite(songPositionSeconds, nameof(songPositionSeconds));
            EnsureFinite(lookAheadSeconds, nameof(lookAheadSeconds));
            if (lookAheadSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lookAheadSeconds),
                    "Look-ahead must be non-negative.");
            }

            TimelineNote nextNote = null;
            double nextTime = double.PositiveInfinity;
            for (int index = 0; index < upcomingNotes.Count; index++)
            {
                TimelineNote note = upcomingNotes[index];
                if (note == null)
                {
                    throw new ArgumentException("Upcoming notes must not contain null entries.", nameof(upcomingNotes));
                }

                double timeUntilNote = note.EffectiveTimeSeconds - songPositionSeconds;
                if (note.Note.Pad != pad || timeUntilNote < 0 || timeUntilNote > lookAheadSeconds)
                {
                    continue;
                }

                if (note.EffectiveTimeSeconds < nextTime)
                {
                    nextNote = note;
                    nextTime = note.EffectiveTimeSeconds;
                }
            }

            if (nextNote == null)
            {
                return PadVisualState.Inactive;
            }

            double remaining = nextTime - songPositionSeconds;
            double intensity = lookAheadSeconds == 0
                ? 1.0
                : 1.0 - remaining / lookAheadSeconds;

            return new PadVisualState(nextNote, (float)Math.Max(0.0, Math.Min(1.0, intensity)));
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }
    }
}
