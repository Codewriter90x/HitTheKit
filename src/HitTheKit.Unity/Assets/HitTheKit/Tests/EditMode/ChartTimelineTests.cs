using System;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class ChartTimelineTests
    {
        [Test]
        public void Upcoming_supports_negative_pre_roll_and_inclusive_boundaries()
        {
            ChartTimeline timeline = Timeline(offset: -0.5, notes: Notes(
                (0.0, "kick"),
                (0.5, "snare"),
                (1.0, "hiHat"),
                (1.01, "kick")));

            var upcoming = timeline.GetUpcoming(-0.5, 1.0);

            Assert.That(upcoming.Select(note => note.EffectiveTimeSeconds),
                Is.EqualTo(new[] { -0.5, 0.0, 0.5 }));
        }

        [Test]
        public void Upcoming_excludes_notes_outside_window()
        {
            Assert.That(Timeline(notes: Notes((1.01, "kick"))).GetUpcoming(0, 1), Is.Empty);
        }

        [Test]
        public void Elapsed_uses_exclusive_boundary()
        {
            ChartTimeline timeline = Timeline(notes: Notes((1.0, "kick"), (1.5, "snare")));

            Assert.That(timeline.GetElapsed(1.0), Is.Empty);
            Assert.That(timeline.GetElapsed(1.0001), Has.Count.EqualTo(1));
        }

        [TestCase(0.25, 1.25)]
        [TestCase(-0.25, 0.75)]
        public void Applies_offset_once(double offset, double expectedEffectiveTime)
        {
            ChartTimeline timeline = Timeline(offset, Notes((1.0, "kick")));
            Assert.That(timeline.Notes[0].EffectiveTimeSeconds, Is.EqualTo(expectedEffectiveTime));
        }

        [Test]
        public void Zero_look_ahead_matches_only_exact_position()
        {
            ChartTimeline timeline = Timeline(notes: Notes((1.0, "kick"), (1.1, "snare")));
            Assert.That(timeline.GetUpcoming(1.0, 0), Has.Count.EqualTo(1));
        }

        [Test]
        public void Rejects_negative_look_ahead()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Timeline().GetUpcoming(0, -0.1));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Rejects_non_finite_song_position(double value)
        {
            ChartTimeline timeline = Timeline();
            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.GetUpcoming(value, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.GetElapsed(value));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Rejects_non_finite_look_ahead(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Timeline().GetUpcoming(0, value));
        }

        [Test]
        public void Rejects_overflowing_window_upper_bound()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Timeline().GetUpcoming(double.MaxValue, double.MaxValue));
        }

        [Test]
        public void Preserves_deterministic_order_for_simultaneous_and_duplicate_notes()
        {
            ChartTimeline timeline = Timeline(notes: Notes(
                (1.0, "snare"),
                (1.0, "kick"),
                (1.0, "kick")));

            var result = timeline.GetUpcoming(1.0, 0);
            Assert.That(result.Select(note => note.Note.Pad),
                Is.EqualTo(new[] { DrumPad.Snare, DrumPad.Kick, DrumPad.Kick }));
            Assert.That(result.Select(note => note.OriginalIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(ReferenceEquals(result[1].Note, result[2].Note), Is.False);
        }

        [Test]
        public void Repeated_queries_do_not_mutate_timeline()
        {
            ChartTimeline timeline = Timeline(notes: Notes((2.0, "kick"), (1.0, "snare")));
            TimelineNote[] before = timeline.Notes.ToArray();

            var first = timeline.GetUpcoming(0, 2);
            var second = timeline.GetUpcoming(0, 2);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(timeline.Notes, Is.EqualTo(before));
        }

        [Test]
        public void Empty_timeline_returns_empty_queries()
        {
            ChartTimeline timeline = Timeline();
            Assert.That(timeline.Notes, Is.Empty);
            Assert.That(timeline.GetUpcoming(-1, 2), Is.Empty);
            Assert.That(timeline.GetElapsed(10), Is.Empty);
        }

        private static ChartTimeline Timeline(double offset = 0, string notes = "")
        {
            string json = $"{{\"version\":1,\"offsetSeconds\":{offset.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"\"difficulties\":{{\"easy\":[{notes}]}}}}";
            return new ChartTimeline(new ChartLoader().Load(json, "easy"));
        }

        private static string Notes(params (double time, string pad)[] notes)
        {
            return string.Join(",", notes.Select(note =>
                $"{{\"time\":{note.time.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"pad\":\"{note.pad}\"}}"));
        }
    }
}
