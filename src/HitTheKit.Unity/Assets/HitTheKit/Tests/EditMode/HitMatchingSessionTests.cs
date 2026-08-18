using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class HitMatchingSessionTests
    {
        [TestCase(1.000, HitGrade.Perfect)]
        [TestCase(1.060, HitGrade.Good)]
        [TestCase(0.880, HitGrade.Early)]
        [TestCase(1.120, HitGrade.Late)]
        public void Produces_core_grade_for_normalized_input(double hitTime, HitGrade expected)
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));

            Assert.That(session.ProcessInput(Input(DrumPad.Kick, hitTime), out HitResult result), Is.True);
            Assert.That(result.Grade, Is.EqualTo(expected));
            Assert.That(result.Note, Is.SameAs(session.Notes[0]));
        }

        [Test]
        public void Out_of_window_input_is_no_match_without_fake_result()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));
            int resolvedEvents = 0;
            session.HitResolved += _ => resolvedEvents++;

            bool matched = session.ProcessInput(Input(DrumPad.Kick, 0.7), out HitResult result);

            Assert.That(matched, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(session.Snapshot.NoMatchCount, Is.EqualTo(1));
            Assert.That(session.Snapshot.ResolvedNoteCount, Is.Zero);
            Assert.That(resolvedEvents, Is.Zero);
        }

        [Test]
        public void Wrong_pad_is_no_match_and_correct_pad_can_still_resolve_note()
        {
            ChartNote note = Note(1, DrumPad.Kick);
            HitMatchingSession session = Session(note);

            Assert.That(session.ProcessInput(Input(DrumPad.Snare, 1), out _), Is.False);
            Assert.That(session.IsResolved(note), Is.False);
            Assert.That(session.ProcessInput(Input(DrumPad.Kick, 1), out HitResult result), Is.True);
            Assert.That(result.Note, Is.SameAs(note));
        }

        [Test]
        public void Same_note_cannot_be_resolved_twice()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));
            Assert.That(session.ProcessInput(Input(DrumPad.Kick, 1), out _), Is.True);
            Assert.That(session.ProcessInput(Input(DrumPad.Kick, 1), out HitResult second), Is.False);
            Assert.That(second, Is.Null);
            Assert.That(session.Snapshot.PerfectCount, Is.EqualTo(1));
            Assert.That(session.Snapshot.NoMatchCount, Is.EqualTo(1));
        }

        [Test]
        public void Hit_note_does_not_later_become_miss()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));
            session.ProcessInput(Input(DrumPad.Kick, 1), out _);

            Assert.That(session.ProcessMisses(2), Is.Zero);
            Assert.That(session.Snapshot.MissCount, Is.Zero);
        }

        [Test]
        public void Missed_note_cannot_later_be_hit()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));
            Assert.That(session.ProcessMisses(1.151), Is.EqualTo(1));

            Assert.That(session.ProcessInput(Input(DrumPad.Kick, 1), out HitResult result), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(session.Snapshot.MissCount, Is.EqualTo(1));
        }

        [Test]
        public void Miss_is_exclusive_after_outer_window_boundary()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));

            Assert.That(session.ProcessMisses(1.150), Is.Zero);
            Assert.That(session.ProcessMisses(1.150001), Is.EqualTo(1));
            Assert.That(session.ProcessMisses(2), Is.Zero);
            Assert.That(session.Snapshot.MissCount, Is.EqualTo(1));
        }

        [TestCase(0.25, 1.25)]
        [TestCase(-0.25, 0.75)]
        public void Applies_chart_offset_once(double offset, double hitTime)
        {
            ChartNote note = Note(1, DrumPad.Snare);
            HitMatchingSession session = Session(offset, note);

            Assert.That(session.ProcessInput(Input(DrumPad.Snare, hitTime), out HitResult result), Is.True);
            Assert.That(result.Grade, Is.EqualTo(HitGrade.Perfect));
            Assert.That(result.Note, Is.SameAs(note));
        }

        [Test]
        public void Duplicate_notes_remain_distinct_and_resolve_in_order()
        {
            ChartNote first = Note(1, DrumPad.Kick);
            ChartNote second = Note(1, DrumPad.Kick);
            HitMatchingSession session = Session(first, second);

            session.ProcessInput(Input(DrumPad.Kick, 1), out HitResult firstResult);
            session.ProcessInput(Input(DrumPad.Kick, 1), out HitResult secondResult);

            Assert.That(firstResult.Note, Is.SameAs(first));
            Assert.That(secondResult.Note, Is.SameAs(second));
            Assert.That(session.Snapshot.ResolvedNoteCount, Is.EqualTo(2));
        }

        [Test]
        public void Notes_are_a_read_only_snapshot_of_the_original_references()
        {
            ChartNote first = Note(1, DrumPad.Kick);
            ChartNote replacement = Note(2, DrumPad.Snare);
            ChartNote[] source = { first };
            HitMatchingSession session = Session(source);
            source[0] = replacement;

            Assert.That(session.Notes[0], Is.SameAs(first));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ChartNote>)session.Notes)[0] = replacement);
        }

        [Test]
        public void Simultaneous_notes_on_distinct_pads_resolve_independently()
        {
            HitMatchingSession session = Session(
                Note(1, DrumPad.Kick),
                Note(1, DrumPad.Snare));

            Assert.That(session.ProcessInput(Input(DrumPad.Kick, 1), out _), Is.True);
            Assert.That(session.ProcessInput(Input(DrumPad.Snare, 1), out _), Is.True);
            Assert.That(session.Snapshot.ResolvedNoteCount, Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_counts_each_outcome_without_score_or_accuracy()
        {
            HitMatchingSession session = Session(
                Note(1, DrumPad.Kick),
                Note(2, DrumPad.Snare),
                Note(3, DrumPad.HiHat),
                Note(4, DrumPad.Kick),
                Note(5, DrumPad.Snare));

            session.ProcessInput(Input(DrumPad.Kick, 1), out _);
            session.ProcessInput(Input(DrumPad.Snare, 2.06), out _);
            session.ProcessInput(Input(DrumPad.HiHat, 2.88), out _);
            session.ProcessInput(Input(DrumPad.Kick, 4.12), out _);
            session.ProcessInput(Input(DrumPad.HiHat, 4), out _);
            session.ProcessMisses(5.151);

            HitMatchingSnapshot snapshot = session.Snapshot;
            Assert.That(snapshot.PerfectCount, Is.EqualTo(1));
            Assert.That(snapshot.GoodCount, Is.EqualTo(1));
            Assert.That(snapshot.EarlyCount, Is.EqualTo(1));
            Assert.That(snapshot.LateCount, Is.EqualTo(1));
            Assert.That(snapshot.NoMatchCount, Is.EqualTo(1));
            Assert.That(snapshot.MissCount, Is.EqualTo(1));
            Assert.That(snapshot.ResolvedNoteCount, Is.EqualTo(5));
            Assert.That(snapshot.TotalNoteCount, Is.EqualTo(5));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void Result_event_is_emitted_once_and_no_match_only_emits_input_outcome()
        {
            HitMatchingSession session = Session(Note(1, DrumPad.Kick));
            int results = 0;
            int outcomes = 0;
            bool observedNullNoMatch = false;
            session.HitResolved += _ => results++;
            session.InputProcessed += (_, result) =>
            {
                outcomes++;
                if (result == null) observedNullNoMatch = true;
            };

            session.ProcessInput(Input(DrumPad.Snare, 1), out _);
            session.ProcessInput(Input(DrumPad.Kick, 1), out _);

            Assert.That(results, Is.EqualTo(1));
            Assert.That(outcomes, Is.EqualTo(2));
            Assert.That(observedNullNoMatch, Is.True);
        }

        [Test]
        public void Pre_roll_input_can_match_without_creating_premature_miss()
        {
            HitMatchingSession session = Session(Note(0, DrumPad.Kick));

            Assert.That(session.ProcessMisses(-0.5), Is.Zero);
            Assert.That(session.ProcessInput(Input(DrumPad.Kick, -0.02), out HitResult result), Is.True);
            Assert.That(result.Grade, Is.EqualTo(HitGrade.Perfect));
        }

        [Test]
        public void End_of_session_resolves_every_note_once()
        {
            HitMatchingSession session = Session(Note(0.1, DrumPad.Kick), Note(0.2, DrumPad.Snare));
            int results = 0;
            session.HitResolved += _ => results++;

            Assert.That(session.ProcessMisses(1), Is.EqualTo(2));
            Assert.That(session.ProcessMisses(2), Is.Zero);
            Assert.That(session.Snapshot.IsComplete, Is.True);
            Assert.That(results, Is.EqualTo(2));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Rejects_non_finite_miss_position(double position)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Session().ProcessMisses(position));
        }

        private static HitMatchingSession Session(params ChartNote[] notes) => Session(0, notes);

        private static HitMatchingSession Session(double offset, params ChartNote[] notes)
        {
            return new HitMatchingSession(notes, TimingWindows.Default, offset);
        }

        private static ChartNote Note(double time, DrumPad pad) => new ChartNote(time, pad);

        private static DrumInputEvent Input(DrumPad pad, double time) => new DrumInputEvent(pad, 100, time);
    }
}
