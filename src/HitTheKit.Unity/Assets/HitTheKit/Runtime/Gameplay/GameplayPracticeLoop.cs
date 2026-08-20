using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class GameplayPracticeRange
    {
        public GameplayPracticeRange(double startSeconds, double endSeconds, string label)
        {
            if (!IsFinite(startSeconds) || startSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(startSeconds));
            if (!IsFinite(endSeconds) || endSeconds <= startSeconds)
                throw new ArgumentOutOfRangeException(nameof(endSeconds));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A practice range label is required.", nameof(label));
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Label = label;
        }

        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public double DurationSeconds => EndSeconds - StartSeconds;
        public string Label { get; }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class GameplayPracticeLoop
    {
        private double? pendingStartSeconds;

        public GameplayPracticeRange Range { get; private set; }
        public bool IsEnabled => Range != null;
        public double? PendingStartSeconds => pendingStartSeconds;

        public void SetStart(double positionSeconds)
        {
            ValidatePosition(positionSeconds, nameof(positionSeconds));
            pendingStartSeconds = positionSeconds;
            Range = null;
        }

        public void SetEnd(double positionSeconds)
        {
            ValidatePosition(positionSeconds, nameof(positionSeconds));
            if (!pendingStartSeconds.HasValue)
                throw new InvalidOperationException("Set practice point A before point B.");
            Range = new GameplayPracticeRange(pendingStartSeconds.Value, positionSeconds, "LOOP A–B");
        }

        public void Select(GameplayPracticeRange range)
        {
            Range = range ?? throw new ArgumentNullException(nameof(range));
            pendingStartSeconds = range.StartSeconds;
        }

        public bool ShouldRestart(double positionSeconds)
        {
            ValidatePosition(positionSeconds, nameof(positionSeconds));
            return Range != null && positionSeconds >= Range.EndSeconds;
        }

        public void Clear()
        {
            Range = null;
            pendingStartSeconds = null;
        }

        private static void ValidatePosition(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public static class GameplayPracticeSections
    {
        public const int DefaultBarsPerSection = 4;

        public static IReadOnlyList<GameplayPracticeRange> Create(
            int bars,
            int beatsPerBar,
            double bpm,
            int barsPerSection = DefaultBarsPerSection)
        {
            if (bars <= 0) throw new ArgumentOutOfRangeException(nameof(bars));
            if (beatsPerBar <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));
            if (double.IsNaN(bpm) || double.IsInfinity(bpm) || bpm <= 0)
                throw new ArgumentOutOfRangeException(nameof(bpm));
            if (barsPerSection <= 0) throw new ArgumentOutOfRangeException(nameof(barsPerSection));

            var result = new List<GameplayPracticeRange>();
            double barDuration = beatsPerBar * 60.0 / bpm;
            for (int firstBar = 1; firstBar <= bars; firstBar += barsPerSection)
            {
                int lastBar = Math.Min(bars, firstBar + barsPerSection - 1);
                double start = (firstBar - 1) * barDuration;
                double end = lastBar * barDuration;
                result.Add(new GameplayPracticeRange(start, end, $"BATTUTE {firstBar}–{lastBar}"));
            }
            return result.AsReadOnly();
        }
    }
}
