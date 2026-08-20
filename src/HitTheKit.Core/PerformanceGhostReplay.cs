using System;
using System.Collections.Generic;

namespace HitTheKit.Core
{
    public sealed class GhostReplayHit
    {
        public GhostReplayHit(double timeSeconds, DrumPad pad, int velocity, HitGrade? grade)
        {
            if (!IsFinite(timeSeconds) || timeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            if (!Enum.IsDefined(typeof(DrumPad), pad)) throw new ArgumentOutOfRangeException(nameof(pad));
            if (velocity < 0 || velocity > 127) throw new ArgumentOutOfRangeException(nameof(velocity));
            if (grade.HasValue && !Enum.IsDefined(typeof(HitGrade), grade.Value))
                throw new ArgumentOutOfRangeException(nameof(grade));
            TimeSeconds = timeSeconds;
            Pad = pad;
            Velocity = velocity;
            Grade = grade;
        }

        public double TimeSeconds { get; }
        public DrumPad Pad { get; }
        public int Velocity { get; }
        public HitGrade? Grade { get; }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class PerformanceGhostReplay
    {
        public const int MaximumHitsPerTake = 8192;

        private readonly List<GhostReplayHit> current = new List<GhostReplayHit>();
        private IReadOnlyList<GhostReplayHit> ghost = Array.Empty<GhostReplayHit>();

        public IReadOnlyList<GhostReplayHit> Ghost => ghost;
        public int CurrentHitCount => current.Count;
        public bool HasGhost => ghost.Count > 0;

        public bool Record(double timeSeconds, DrumPad pad, int velocity, HitGrade? grade)
        {
            if (timeSeconds < 0) return false;
            if (current.Count >= MaximumHitsPerTake) return false;
            current.Add(new GhostReplayHit(timeSeconds, pad, velocity, grade));
            return true;
        }

        public bool CommitCurrentTake()
        {
            if (current.Count == 0) return false;
            var copy = current.ToArray();
            Array.Sort(copy, CompareHits);
            ghost = Array.AsReadOnly(copy);
            current.Clear();
            return true;
        }

        public void ResetCurrent() => current.Clear();
        public void ClearGhost() => ghost = Array.Empty<GhostReplayHit>();

        private static int CompareHits(GhostReplayHit left, GhostReplayHit right)
        {
            int byTime = left.TimeSeconds.CompareTo(right.TimeSeconds);
            return byTime != 0 ? byTime : left.Pad.CompareTo(right.Pad);
        }
    }
}
