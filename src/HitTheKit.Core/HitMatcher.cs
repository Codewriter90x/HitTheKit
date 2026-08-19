using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HitTheKit.Core
{
    public sealed class HitMatcher
    {
        private readonly HashSet<ChartNote> _resolvedNotes =
            new HashSet<ChartNote>(ChartNoteReferenceComparer.Instance);
        private readonly TimingWindows _windows;

        public HitMatcher(TimingWindows windows, double offsetSeconds = 0)
        {
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));

            if (!IsFinite(offsetSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offsetSeconds),
                    "The chart offset must be finite.");
            }

            OffsetSeconds = offsetSeconds;
        }

        public double OffsetSeconds { get; }

        public bool TryMatch(
            IReadOnlyList<ChartNote> notes,
            DrumHit hit,
            out HitResult? result)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            ChartNote? bestNote = null;
            double bestDeltaSeconds = 0;
            double bestAbsoluteDelta = 0;

            for (int index = 0; index < notes.Count; index++)
            {
                ChartNote note = notes[index]
                    ?? throw new ArgumentException(
                        "The candidate list cannot contain null notes.",
                        nameof(notes));

                if (_resolvedNotes.Contains(note) || note.Pad != hit.Pad ||
                    note.Articulation != DrumArticulation.Default && note.Articulation != hit.Articulation)
                {
                    continue;
                }

                double deltaSeconds = hit.TimeSeconds - GetEffectiveTime(note);
                double absoluteDelta = Math.Abs(deltaSeconds);

                if (absoluteDelta > _windows.HitSeconds)
                {
                    continue;
                }

                if (bestNote == null
                    || absoluteDelta < bestAbsoluteDelta
                    || (absoluteDelta == bestAbsoluteDelta
                        && note.TimeSeconds < bestNote.TimeSeconds))
                {
                    bestNote = note;
                    bestDeltaSeconds = deltaSeconds;
                    bestAbsoluteDelta = absoluteDelta;
                }
            }

            if (bestNote == null)
            {
                result = null;
                return false;
            }

            HitGrade grade = Grade(bestDeltaSeconds);
            _resolvedNotes.Add(bestNote);
            result = new HitResult(grade, bestNote, hit, bestDeltaSeconds);
            return true;
        }

        public bool TryMarkMissed(
            ChartNote note,
            double currentTimeSeconds,
            out HitResult? result)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            if (!IsFinite(currentTimeSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTimeSeconds),
                    "The current time must be finite.");
            }

            if (_resolvedNotes.Contains(note)
                || currentTimeSeconds <= GetEffectiveTime(note) + _windows.HitSeconds)
            {
                result = null;
                return false;
            }

            _resolvedNotes.Add(note);
            result = new HitResult(HitGrade.Miss, note, hit: null, deltaSeconds: null);
            return true;
        }

        public bool IsResolved(ChartNote note)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            return _resolvedNotes.Contains(note);
        }

        private double GetEffectiveTime(ChartNote note)
        {
            return note.TimeSeconds + OffsetSeconds;
        }

        private HitGrade Grade(double deltaSeconds)
        {
            double absoluteDelta = Math.Abs(deltaSeconds);

            if (absoluteDelta <= _windows.PerfectSeconds)
            {
                return HitGrade.Perfect;
            }

            if (absoluteDelta <= _windows.GoodSeconds)
            {
                return HitGrade.Good;
            }

            return deltaSeconds < 0 ? HitGrade.Early : HitGrade.Late;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class ChartNoteReferenceComparer : IEqualityComparer<ChartNote>
        {
            public static ChartNoteReferenceComparer Instance { get; } =
                new ChartNoteReferenceComparer();

            private ChartNoteReferenceComparer()
            {
            }

            public bool Equals(ChartNote? left, ChartNote? right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(ChartNote note)
            {
                return RuntimeHelpers.GetHashCode(note);
            }
        }
    }
}
