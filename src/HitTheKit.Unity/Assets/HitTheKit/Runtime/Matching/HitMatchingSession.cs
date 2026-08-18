using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Matching
{
    public sealed class HitMatchingSession
    {
        private readonly ChartNote[] notes;
        private readonly IReadOnlyList<ChartNote> readOnlyNotes;
        private readonly HitMatcher matcher;
        private DrumInputEvent? lastInput;
        private HitResult lastResult;
        private double? lastEventSongTimeSeconds;
        private int perfectCount;
        private int goodCount;
        private int earlyCount;
        private int lateCount;
        private int missCount;
        private int noMatchCount;
        private int resolvedNoteCount;

        public HitMatchingSession(
            IReadOnlyList<ChartNote> chartNotes,
            TimingWindows timingWindows,
            double offsetSeconds)
        {
            if (chartNotes == null) throw new ArgumentNullException(nameof(chartNotes));
            matcher = new HitMatcher(
                timingWindows ?? throw new ArgumentNullException(nameof(timingWindows)),
                offsetSeconds);

            notes = new ChartNote[chartNotes.Count];
            for (int index = 0; index < notes.Length; index++)
            {
                notes[index] = chartNotes[index]
                    ?? throw new ArgumentException("Chart notes must not contain null entries.", nameof(chartNotes));
            }

            readOnlyNotes = Array.AsReadOnly(notes);
            Snapshot = CreateSnapshot();
        }

        public event Action<HitResult> HitResolved;
        public event Action<DrumInputEvent, HitResult> InputProcessed;

        public IReadOnlyList<ChartNote> Notes => readOnlyNotes;
        public HitMatchingSnapshot Snapshot { get; private set; }

        public bool ProcessInput(DrumInputEvent input, out HitResult result)
        {
            lastInput = input;
            lastEventSongTimeSeconds = input.SongTimeSeconds;
            var hit = new DrumHit(input.Pad, input.SongTimeSeconds, input.Velocity);
            bool matched = matcher.TryMatch(notes, hit, out result);
            if (matched)
            {
                lastResult = result;
                resolvedNoteCount++;
                IncrementGrade(result.Grade);
                Snapshot = CreateSnapshot();
                HitResolved?.Invoke(result);
            }
            else
            {
                lastResult = null;
                noMatchCount++;
                Snapshot = CreateSnapshot();
            }

            InputProcessed?.Invoke(input, result);
            return matched;
        }

        public int ProcessMisses(double currentSongPositionSeconds)
        {
            if (double.IsNaN(currentSongPositionSeconds) || double.IsInfinity(currentSongPositionSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentSongPositionSeconds),
                    "Song position must be finite.");
            }

            int newlyMissed = 0;
            for (int index = 0; index < notes.Length; index++)
            {
                if (!matcher.TryMarkMissed(notes[index], currentSongPositionSeconds, out HitResult result))
                {
                    continue;
                }

                newlyMissed++;
                resolvedNoteCount++;
                missCount++;
                lastResult = result;
                lastEventSongTimeSeconds = currentSongPositionSeconds;
                Snapshot = CreateSnapshot();
                HitResolved?.Invoke(result);
            }

            return newlyMissed;
        }

        public bool IsResolved(ChartNote note) => matcher.IsResolved(note);

        private void IncrementGrade(HitGrade grade)
        {
            switch (grade)
            {
                case HitGrade.Perfect: perfectCount++; break;
                case HitGrade.Good: goodCount++; break;
                case HitGrade.Early: earlyCount++; break;
                case HitGrade.Late: lateCount++; break;
                default:
                    throw new InvalidOperationException("Input matching cannot produce a Miss result.");
            }
        }

        private HitMatchingSnapshot CreateSnapshot()
        {
            return new HitMatchingSnapshot(
                lastInput,
                lastResult,
                lastEventSongTimeSeconds,
                perfectCount,
                goodCount,
                earlyCount,
                lateCount,
                missCount,
                noMatchCount,
                resolvedNoteCount,
                notes.Length);
        }
    }
}
