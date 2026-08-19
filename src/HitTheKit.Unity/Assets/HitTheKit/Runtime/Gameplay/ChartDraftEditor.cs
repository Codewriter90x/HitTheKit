using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HitTheKit.Core;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class EditableChartNote
    {
        internal EditableChartNote(
            int identity,
            DrumPad pad,
            int velocity,
            double timeSeconds,
            DrumInputSource source)
        {
            Identity = identity;
            Pad = pad;
            Velocity = velocity;
            TimeSeconds = timeSeconds;
            Source = source;
        }

        public int Identity { get; }
        public DrumPad Pad { get; internal set; }
        public int Velocity { get; }
        public double TimeSeconds { get; internal set; }
        public DrumInputSource Source { get; }
    }

    public sealed class ChartDraftEditor
    {
        private readonly double durationSeconds;
        private readonly List<EditableChartNote> notes;
        private readonly ReadOnlyCollection<EditableChartNote> readOnlyNotes;
        private int nextIdentity;

        public ChartDraftEditor(ChartRecordingDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            durationSeconds = draft.DurationSeconds;
            notes = new List<EditableChartNote>(draft.Hits.Count);
            for (int index = 0; index < draft.Hits.Count; index++)
            {
                RecordedChartHit hit = draft.Hits[index];
                notes.Add(new EditableChartNote(nextIdentity++, hit.Pad, hit.Velocity, hit.TimeSeconds, hit.Source));
            }
            Sort();
            readOnlyNotes = notes.AsReadOnly();
        }

        public double DurationSeconds => durationSeconds;
        public IReadOnlyList<EditableChartNote> Notes => readOnlyNotes;

        public int Update(int index, double timeSeconds, DrumPad pad)
        {
            EditableChartNote note = At(index);
            ValidateTime(timeSeconds);
            ValidatePad(pad);
            note.TimeSeconds = timeSeconds;
            note.Pad = pad;
            Sort();
            return notes.IndexOf(note);
        }

        public int Add(double timeSeconds, DrumPad pad, int velocity = 100)
        {
            ValidateTime(timeSeconds);
            ValidatePad(pad);
            if (velocity < 0 || velocity > 127) throw new ArgumentOutOfRangeException(nameof(velocity));
            if (notes.Count >= ChartRecordingSession.MaximumHits)
                throw new InvalidOperationException($"A chart cannot exceed {ChartRecordingSession.MaximumHits} notes.");
            var note = new EditableChartNote(nextIdentity++, pad, velocity, timeSeconds, DrumInputSource.Test);
            notes.Add(note);
            Sort();
            return notes.IndexOf(note);
        }

        public void Delete(int index) => notes.RemoveAt(ValidatedIndex(index));

        public ChartRecordingDraft BuildDraft()
        {
            var hits = new RecordedChartHit[notes.Count];
            for (int index = 0; index < notes.Count; index++)
            {
                EditableChartNote note = notes[index];
                hits[index] = new RecordedChartHit(
                    new DrumInputEvent(note.Pad, note.Velocity, note.TimeSeconds, note.Source),
                    index);
            }
            return new ChartRecordingDraft(durationSeconds, hits);
        }

        private EditableChartNote At(int index) => notes[ValidatedIndex(index)];

        private int ValidatedIndex(int index)
        {
            if (index < 0 || index >= notes.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return index;
        }

        private void ValidateTime(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > durationSeconds)
                throw new ArgumentOutOfRangeException(nameof(value), $"Note time must be between 0 and {durationSeconds:0.###} seconds.");
        }

        private static void ValidatePad(DrumPad pad)
        {
            if (!Enum.IsDefined(typeof(DrumPad), pad)) throw new ArgumentOutOfRangeException(nameof(pad));
        }

        private void Sort() => notes.Sort((left, right) =>
        {
            int byTime = left.TimeSeconds.CompareTo(right.TimeSeconds);
            return byTime != 0 ? byTime : left.Identity.CompareTo(right.Identity);
        });
    }
}
