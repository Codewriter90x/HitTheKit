using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Visuals;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class PadVisualStateCalculatorTests
    {
        private readonly PadVisualStateCalculator calculator = new PadVisualStateCalculator();

        [Test]
        public void Rejects_null_upcoming_notes()
        {
            Assert.Throws<ArgumentNullException>(() =>
                calculator.Calculate(DrumPad.Kick, 0, 1, null));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Rejects_non_finite_song_position(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.Calculate(DrumPad.Kick, value, 1, Array.Empty<TimelineNote>()));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Rejects_non_finite_look_ahead(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.Calculate(DrumPad.Kick, 0, value, Array.Empty<TimelineNote>()));
        }

        [Test]
        public void Rejects_negative_look_ahead()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                calculator.Calculate(DrumPad.Kick, 0, -0.1, Array.Empty<TimelineNote>()));
        }

        [Test]
        public void Rejects_null_note_entry()
        {
            Assert.Throws<ArgumentException>(() =>
                calculator.Calculate(DrumPad.Kick, 0, 1, new TimelineNote[] { null }));
        }

        [Test]
        public void No_matching_note_is_inactive()
        {
            PadVisualState state = Calculate(DrumPad.Kick, 0, 1, Notes((0.5, "snare")));

            Assert.That(state.IsActive, Is.False);
            Assert.That(state.Intensity, Is.Zero);
            Assert.That(state.NextNote, Is.Null);
        }

        [TestCase(0.0, 0.0)]
        [TestCase(0.5, 0.5)]
        [TestCase(1.0, 1.0)]
        public void Intensity_tracks_progress_through_look_ahead(double songPosition, double expected)
        {
            PadVisualState state = Calculate(DrumPad.Kick, songPosition, 1, Notes((1.0, "kick")));

            Assert.That(state.IsActive, Is.True);
            Assert.That(state.Intensity, Is.EqualTo(expected).Within(0.000001));
        }

        [Test]
        public void Result_is_clamped_to_normalized_range_for_small_window()
        {
            PadVisualState state = Calculate(
                DrumPad.Kick,
                0.9999999999999999,
                0.0000000000000002,
                Notes((1.0, "kick")));

            Assert.That(state.Intensity, Is.InRange(0f, 1f));
        }

        [Test]
        public void Ignores_note_for_another_pad()
        {
            Assert.That(Calculate(DrumPad.Kick, 0.5, 1, Notes((1.0, "hiHat"))).IsActive, Is.False);
        }

        [Test]
        public void Uses_earliest_upcoming_note_for_same_pad()
        {
            IReadOnlyList<TimelineNote> notes = Notes((0.8, "kick"), (0.5, "kick"));

            PadVisualState state = Calculate(DrumPad.Kick, 0, 1, notes);

            Assert.That(state.NextNoteEffectiveTimeSeconds, Is.EqualTo(0.5));
            Assert.That(state.Intensity, Is.EqualTo(0.5).Within(0.000001));
        }

        [Test]
        public void Simultaneous_notes_activate_independent_pads()
        {
            IReadOnlyList<TimelineNote> notes = Notes((1.0, "kick"), (1.0, "snare"));

            Assert.That(Calculate(DrumPad.Kick, 0.5, 1, notes).IsActive, Is.True);
            Assert.That(Calculate(DrumPad.Snare, 0.5, 1, notes).IsActive, Is.True);
            Assert.That(Calculate(DrumPad.HiHat, 0.5, 1, notes).IsActive, Is.False);
        }

        [Test]
        public void Equal_time_notes_preserve_timeline_order()
        {
            IReadOnlyList<TimelineNote> notes = Notes((1.0, "kick"), (1.0, "kick"));

            PadVisualState state = Calculate(DrumPad.Kick, 0.5, 1, notes);

            Assert.That(state.NextNote, Is.SameAs(notes[0]));
            Assert.That(state.NextNote.OriginalIndex, Is.EqualTo(0));
        }

        [Test]
        public void Zero_look_ahead_matches_exact_note_at_full_intensity()
        {
            PadVisualState state = Calculate(DrumPad.Kick, 1, 0, Notes((1.0, "kick")));

            Assert.That(state.IsActive, Is.True);
            Assert.That(state.Intensity, Is.EqualTo(1));
        }

        [Test]
        public void Zero_look_ahead_rejects_non_exact_note()
        {
            Assert.That(Calculate(DrumPad.Kick, 0.999, 0, Notes((1.0, "kick"))).IsActive, Is.False);
        }

        [Test]
        public void Supports_negative_pre_roll()
        {
            PadVisualState state = Calculate(DrumPad.Kick, -0.5, 1, Notes((0.0, "kick")));

            Assert.That(state.Intensity, Is.EqualTo(0.5).Within(0.000001));
        }

        [Test]
        public void Uses_effective_time_without_reapplying_chart_offset()
        {
            IReadOnlyList<TimelineNote> notes = NotesWithOffset(0.25, (1.0, "kick"));

            PadVisualState state = Calculate(DrumPad.Kick, 0.25, 1, notes);

            Assert.That(state.NextNoteEffectiveTimeSeconds, Is.EqualTo(1.25));
            Assert.That(state.Intensity, Is.Zero);
        }

        [Test]
        public void Repeated_calculation_is_deterministic()
        {
            IReadOnlyList<TimelineNote> notes = Notes((1.0, "kick"));

            PadVisualState first = Calculate(DrumPad.Kick, 0.4, 1, notes);
            PadVisualState second = Calculate(DrumPad.Kick, 0.4, 1, notes);

            Assert.That(second.NextNote, Is.SameAs(first.NextNote));
            Assert.That(second.Intensity, Is.EqualTo(first.Intensity));
        }

        private PadVisualState Calculate(
            DrumPad pad,
            double songPosition,
            double lookAhead,
            IReadOnlyList<TimelineNote> notes)
        {
            return calculator.Calculate(pad, songPosition, lookAhead, notes);
        }

        private static IReadOnlyList<TimelineNote> Notes(params (double time, string pad)[] notes)
        {
            return NotesWithOffset(0, notes);
        }

        private static IReadOnlyList<TimelineNote> NotesWithOffset(
            double offset,
            params (double time, string pad)[] notes)
        {
            string noteJson = string.Join(",", notes.Select(note =>
                $"{{\"time\":{note.time.ToString(CultureInfo.InvariantCulture)},\"pad\":\"{note.pad}\"}}"));
            string json = $"{{\"version\":1,\"offsetSeconds\":{offset.ToString(CultureInfo.InvariantCulture)}," +
                $"\"difficulties\":{{\"easy\":[{noteJson}]}}}}";
            return new ChartTimeline(new ChartLoader().Load(json, "easy")).Notes;
        }
    }
}
