using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Charts
{
    public sealed class ChartTimeline
    {
        private readonly IReadOnlyList<TimelineNote> notes;

        public ChartTimeline(LoadedChart chart, double playbackSpeed = 1.0)
        {
            if (chart == null) throw new ArgumentNullException(nameof(chart));
            if (!IsFinite(playbackSpeed) || playbackSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(playbackSpeed));

            var timelineNotes = new TimelineNote[chart.Notes.Count];
            for (int index = 0; index < timelineNotes.Length; index++)
            {
                double effectiveTime = (chart.Notes[index].TimeSeconds + chart.OffsetSeconds) / playbackSpeed;
                if (!IsFinite(effectiveTime))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(chart),
                        "A chart note effective time must be finite.");
                }

                timelineNotes[index] = new TimelineNote(
                    chart.Notes[index],
                    chart.OriginalIndices[index],
                    effectiveTime);
            }

            notes = Array.AsReadOnly(timelineNotes);
        }

        public IReadOnlyList<TimelineNote> Notes => notes;

        public IReadOnlyList<TimelineNote> GetUpcoming(
            double songPositionSeconds,
            double lookAheadSeconds)
        {
            ValidateFinite(songPositionSeconds, nameof(songPositionSeconds));
            ValidateFinite(lookAheadSeconds, nameof(lookAheadSeconds));
            if (lookAheadSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lookAheadSeconds),
                    "Look-ahead must be non-negative.");
            }

            double upperBound = songPositionSeconds + lookAheadSeconds;
            if (!IsFinite(upperBound))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lookAheadSeconds),
                    "The look-ahead range must have a finite upper bound.");
            }

            var result = new List<TimelineNote>();
            foreach (TimelineNote note in notes)
            {
                if (note.EffectiveTimeSeconds >= songPositionSeconds &&
                    note.EffectiveTimeSeconds <= upperBound)
                {
                    result.Add(note);
                }
            }

            return result.AsReadOnly();
        }

        public IReadOnlyList<TimelineNote> GetElapsed(double songPositionSeconds)
        {
            ValidateFinite(songPositionSeconds, nameof(songPositionSeconds));

            var result = new List<TimelineNote>();
            foreach (TimelineNote note in notes)
            {
                if (note.EffectiveTimeSeconds < songPositionSeconds)
                {
                    result.Add(note);
                }
            }

            return result.AsReadOnly();
        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
