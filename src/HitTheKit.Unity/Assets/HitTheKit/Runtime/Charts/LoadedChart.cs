using System;
using System.Collections.Generic;
using HitTheKit.Core;

namespace HitTheKit.Unity.Charts
{
    public sealed class LoadedChart
    {
        private readonly IReadOnlyList<ChartNote> notes;
        private readonly IReadOnlyList<int> originalIndices;

        internal LoadedChart(
            int version,
            string difficulty,
            double offsetSeconds,
            IReadOnlyList<IndexedChartNote> indexedNotes)
        {
            Version = version;
            Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            OffsetSeconds = offsetSeconds;

            var noteArray = new ChartNote[indexedNotes.Count];
            var indexArray = new int[indexedNotes.Count];
            for (int index = 0; index < indexedNotes.Count; index++)
            {
                noteArray[index] = indexedNotes[index].Note;
                indexArray[index] = indexedNotes[index].OriginalIndex;
            }

            notes = Array.AsReadOnly(noteArray);
            originalIndices = Array.AsReadOnly(indexArray);
        }

        public int Version { get; }
        public string Difficulty { get; }
        public double OffsetSeconds { get; }
        public IReadOnlyList<ChartNote> Notes => notes;
        internal IReadOnlyList<int> OriginalIndices => originalIndices;
    }

    internal sealed class IndexedChartNote
    {
        public IndexedChartNote(ChartNote note, int originalIndex)
        {
            Note = note ?? throw new ArgumentNullException(nameof(note));
            OriginalIndex = originalIndex;
        }

        public ChartNote Note { get; }
        public int OriginalIndex { get; }
    }
}
