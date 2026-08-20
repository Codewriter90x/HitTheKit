using System;
using System.Collections.Generic;

namespace HitTheKit.Core
{
    public sealed class PracticeSectionDefinition
    {
        public PracticeSectionDefinition(int index, string label, double startSeconds, double endSeconds)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Section label is required.", nameof(label));
            if (!IsFinite(startSeconds) || startSeconds < 0) throw new ArgumentOutOfRangeException(nameof(startSeconds));
            if (!IsFinite(endSeconds) || endSeconds <= startSeconds) throw new ArgumentOutOfRangeException(nameof(endSeconds));
            Index = index;
            Label = label;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
        }

        public int Index { get; }
        public string Label { get; }
        public double StartSeconds { get; }
        public double EndSeconds { get; }

        public bool Contains(double noteTimeSeconds) =>
            noteTimeSeconds >= StartSeconds && noteTimeSeconds < EndSeconds;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class PracticeErrorCell
    {
        internal PracticeErrorCell(
            PracticeSectionDefinition section,
            DrumPad pad,
            int perfect,
            int good,
            int early,
            int late,
            int miss)
        {
            Section = section;
            Pad = pad;
            Perfect = perfect;
            Good = good;
            Early = early;
            Late = late;
            Miss = miss;
        }

        public PracticeSectionDefinition Section { get; }
        public DrumPad Pad { get; }
        public int Perfect { get; }
        public int Good { get; }
        public int Early { get; }
        public int Late { get; }
        public int Miss { get; }
        public int Resolved => Perfect + Good + Early + Late + Miss;
        public double Accuracy => Resolved == 0
            ? 0
            : (Perfect + Good * 0.75 + (Early + Late) * 0.5) * 100.0 / Resolved;
    }

    public sealed class PracticeErrorMapAnalyzer
    {
        private readonly IReadOnlyList<PracticeSectionDefinition> sections;
        private readonly Dictionary<CellKey, MutableCell> cells = new Dictionary<CellKey, MutableCell>();

        public PracticeErrorMapAnalyzer(IReadOnlyList<PracticeSectionDefinition> sections)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            if (sections.Count == 0) throw new ArgumentException("At least one practice section is required.", nameof(sections));
            var copy = new PracticeSectionDefinition[sections.Count];
            for (int index = 0; index < sections.Count; index++)
            {
                PracticeSectionDefinition section = sections[index]
                    ?? throw new ArgumentException("Practice sections cannot contain null entries.", nameof(sections));
                if (section.Index != index)
                    throw new ArgumentException("Practice section indices must be contiguous and ordered.", nameof(sections));
                if (index > 0 && Math.Abs(copy[index - 1].EndSeconds - section.StartSeconds) > 0.000001)
                    throw new ArgumentException("Practice sections must be contiguous.", nameof(sections));
                copy[index] = section;
            }
            this.sections = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<PracticeSectionDefinition> Sections => sections;

        public void Record(HitResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            PracticeSectionDefinition? section = FindSection(result.Note.TimeSeconds);
            if (section == null) return;
            var key = new CellKey(section.Index, result.Note.Pad);
            if (!cells.TryGetValue(key, out MutableCell cell))
            {
                cell = new MutableCell();
                cells.Add(key, cell);
            }
            switch (result.Grade)
            {
                case HitGrade.Perfect: cell.Perfect++; break;
                case HitGrade.Good: cell.Good++; break;
                case HitGrade.Early: cell.Early++; break;
                case HitGrade.Late: cell.Late++; break;
                case HitGrade.Miss: cell.Miss++; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public IReadOnlyList<PracticeErrorCell> Snapshot()
        {
            var result = new List<PracticeErrorCell>(cells.Count);
            foreach (KeyValuePair<CellKey, MutableCell> pair in cells)
            {
                MutableCell value = pair.Value;
                result.Add(new PracticeErrorCell(
                    sections[pair.Key.SectionIndex],
                    pair.Key.Pad,
                    value.Perfect,
                    value.Good,
                    value.Early,
                    value.Late,
                    value.Miss));
            }
            result.Sort(CompareCells);
            return result.AsReadOnly();
        }

        public PracticeErrorCell? Weakest()
        {
            IReadOnlyList<PracticeErrorCell> snapshot = Snapshot();
            if (snapshot.Count == 0) return null;
            PracticeErrorCell weakest = snapshot[0];
            for (int index = 1; index < snapshot.Count; index++)
            {
                PracticeErrorCell candidate = snapshot[index];
                if (candidate.Accuracy < weakest.Accuracy ||
                    (Math.Abs(candidate.Accuracy - weakest.Accuracy) < 0.0001 && candidate.Resolved > weakest.Resolved))
                    weakest = candidate;
            }
            return weakest;
        }

        public void Reset() => cells.Clear();

        private PracticeSectionDefinition? FindSection(double noteTimeSeconds)
        {
            for (int index = 0; index < sections.Count; index++)
                if (sections[index].Contains(noteTimeSeconds)) return sections[index];
            return null;
        }

        private static int CompareCells(PracticeErrorCell left, PracticeErrorCell right)
        {
            int bySection = left.Section.Index.CompareTo(right.Section.Index);
            return bySection != 0 ? bySection : left.Pad.CompareTo(right.Pad);
        }

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int sectionIndex, DrumPad pad)
            {
                SectionIndex = sectionIndex;
                Pad = pad;
            }

            public int SectionIndex { get; }
            public DrumPad Pad { get; }
            public bool Equals(CellKey other) => SectionIndex == other.SectionIndex && Pad == other.Pad;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode() => (SectionIndex * 397) ^ (int)Pad;
        }

        private sealed class MutableCell
        {
            public int Perfect;
            public int Good;
            public int Early;
            public int Late;
            public int Miss;
        }
    }
}
