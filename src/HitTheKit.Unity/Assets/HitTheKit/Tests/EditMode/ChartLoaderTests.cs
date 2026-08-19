using System;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class ChartLoaderTests
    {
        private readonly ChartLoader loader = new ChartLoader();

        [Test]
        public void Loads_valid_chart_and_maps_all_supported_pads()
        {
            LoadedChart chart = loader.Load(
                Chart("" +
                    "{\"time\":1.0,\"pad\":\"kick\"}," +
                    "{\"time\":2.0,\"pad\":\"snare\"}," +
                    "{\"time\":3.0,\"pad\":\"hiHat\"}," +
                    "{\"time\":4.0,\"pad\":\"tom1\"}," +
                    "{\"time\":5.0,\"pad\":\"tom2\"}," +
                    "{\"time\":6.0,\"pad\":\"floorTom\"}," +
                    "{\"time\":7.0,\"pad\":\"crash\"}," +
                    "{\"time\":8.0,\"pad\":\"ride\"}"),
                "easy");

            Assert.That(chart.Version, Is.EqualTo(1));
            Assert.That(chart.Difficulty, Is.EqualTo("easy"));
            Assert.That(chart.Notes.Select(note => note.Pad), Is.EqualTo(new[]
            {
                DrumPad.Kick,
                DrumPad.Snare,
                DrumPad.HiHat,
                DrumPad.Tom1,
                DrumPad.Tom2,
                DrumPad.FloorTom,
                DrumPad.Crash,
                DrumPad.Ride
            }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Rejects_null_or_empty_json(string json)
        {
            Assert.Throws<ArgumentException>(() => loader.Load(json, "easy"));
        }

        [Test]
        public void Rejects_malformed_json()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load("{not-json", "easy"));
        }

        [Test]
        public void Rejects_missing_version()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                "{\"offsetSeconds\":0,\"difficulties\":{\"easy\":[]}}",
                "easy"));
        }

        [Test]
        public void Rejects_missing_offset()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                "{\"version\":1,\"difficulties\":{\"easy\":[]}}",
                "easy"));
        }

        [Test]
        public void Rejects_unsupported_version()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(Chart(string.Empty, version: 2), "easy"));
        }

        [TestCase(0.125)]
        [TestCase(-0.125)]
        public void Preserves_finite_offset(double offset)
        {
            LoadedChart chart = loader.Load(Chart(string.Empty, offset), "easy");
            Assert.That(chart.OffsetSeconds, Is.EqualTo(offset));
        }

        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("1e999")]
        public void Rejects_non_finite_offset(string value)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                $"{{\"version\":1,\"offsetSeconds\":{value},\"difficulties\":{{\"easy\":[]}}}}",
                "easy"));
        }

        [Test]
        public void Rejects_missing_difficulties()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                "{\"version\":1,\"offsetSeconds\":0}",
                "easy"));
        }

        [Test]
        public void Rejects_null_difficulties()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":null}",
                "easy"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Rejects_missing_difficulty(string difficulty)
        {
            Assert.Throws<ArgumentException>(() => loader.Load(Chart(string.Empty), difficulty));
        }

        [TestCase("hard")]
        [TestCase("Easy")]
        public void Rejects_unsupported_or_incorrectly_cased_difficulty(string difficulty)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(Chart(string.Empty), difficulty));
        }

        [Test]
        public void Discovers_and_loads_only_the_supported_difficulties_present_in_the_chart()
        {
            const string json =
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{" +
                "\"easy\":[{\"time\":1,\"pad\":\"kick\"}]," +
                "\"hard\":[{\"time\":2,\"pad\":\"snare\"}]}}";

            Assert.That(loader.GetAvailableDifficulties(json), Is.EqualTo(new[] { "easy", "hard" }));
            Assert.That(loader.Load(json, "hard").Notes.Single().Pad, Is.EqualTo(DrumPad.Snare));
            Assert.Throws<ChartLoadException>(() => loader.Load(json, "medium"));
        }

        [Test]
        public void Discovers_progressive_learning_track_difficulties_in_teaching_order()
        {
            const string json =
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{" +
                "\"easy\":[],\"medium\":[],\"hard\":[],\"advanced\":[],\"full\":[]}}";

            Assert.That(loader.GetAvailableDifficulties(json), Is.EqualTo(new[]
            {
                "easy",
                "medium",
                "hard",
                "advanced",
                "full"
            }));
        }

        [Test]
        public void Rejects_a_chart_without_any_supported_difficulty()
        {
            const string json =
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"custom\":[]}}";

            Assert.Throws<ChartLoadException>(() => loader.GetAvailableDifficulties(json));
        }

        [Test]
        public void Rejects_null_note_list()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"easy\":null}}",
                "easy"));
        }

        [Test]
        public void Accepts_empty_note_list()
        {
            Assert.That(loader.Load(Chart(string.Empty), "easy").Notes, Is.Empty);
        }

        [Test]
        public void Rejects_null_note()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(Chart("null"), "easy"));
        }

        [Test]
        public void Rejects_missing_note_time()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart("{\"pad\":\"kick\"}"),
                "easy"));
        }

        [TestCase("-0.1")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("1e999")]
        public void Rejects_invalid_note_time(string time)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart($"{{\"time\":{time},\"pad\":\"kick\"}}"),
                "easy"));
        }

        [TestCase("")]
        [TestCase("tom")]
        [TestCase("Kick")]
        [TestCase("SNARE")]
        [TestCase("hihat")]
        [TestCase("hi-hat")]
        [TestCase(" kick")]
        [TestCase("kick ")]
        public void Rejects_unknown_or_incorrectly_cased_pad(string pad)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart($"{{\"time\":1,\"pad\":\"{pad}\"}}"),
                "easy"));
        }

        [Test]
        public void Ignores_unknown_properties_without_changing_known_fields()
        {
            LoadedChart chart = loader.Load(
                "{\"version\":1,\"offsetSeconds\":0,\"futureField\":42," +
                "\"difficulties\":{\"easy\":[]}}",
                "easy");

            Assert.That(chart.Version, Is.EqualTo(1));
            Assert.That(chart.OffsetSeconds, Is.Zero);
            Assert.That(chart.Notes, Is.Empty);
        }

        [Test]
        public void Sorts_by_time_and_preserves_json_order_on_ties()
        {
            LoadedChart chart = loader.Load(
                Chart("" +
                    "{\"time\":2,\"pad\":\"kick\"}," +
                    "{\"time\":1,\"pad\":\"snare\"}," +
                    "{\"time\":1,\"pad\":\"hiHat\"}"),
                "easy");

            Assert.That(chart.Notes.Select(note => note.Pad), Is.EqualTo(new[]
            {
                DrumPad.Snare,
                DrumPad.HiHat,
                DrumPad.Kick
            }));
        }

        [Test]
        public void Preserves_duplicate_events_as_distinct_instances()
        {
            LoadedChart chart = loader.Load(
                Chart("{\"time\":1,\"pad\":\"kick\"},{\"time\":1,\"pad\":\"kick\"}"),
                "easy");

            Assert.That(chart.Notes, Has.Count.EqualTo(2));
            Assert.That(ReferenceEquals(chart.Notes[0], chart.Notes[1]), Is.False);
        }

        [Test]
        public void Loads_optional_velocity_and_articulation_without_changing_legacy_notes()
        {
            LoadedChart chart = loader.Load(Chart(
                "{\"time\":1,\"pad\":\"ride\",\"velocity\":112,\"articulation\":\"bell\"}," +
                "{\"time\":2,\"pad\":\"snare\"}"), "easy");

            Assert.That(chart.Notes[0].Velocity, Is.EqualTo(112));
            Assert.That(chart.Notes[0].Articulation, Is.EqualTo(DrumArticulation.Bell));
            Assert.That(chart.Notes[1].Velocity, Is.Null);
            Assert.That(chart.Notes[1].Articulation, Is.EqualTo(DrumArticulation.Default));
        }

        [TestCase("0")]
        [TestCase("128")]
        [TestCase("null")]
        public void Rejects_invalid_explicit_velocity(string value)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart($"{{\"time\":1,\"pad\":\"snare\",\"velocity\":{value}}}"), "easy"));
        }

        [TestCase("Bell")]
        [TestCase("unknown")]
        public void Rejects_unknown_or_incorrectly_cased_articulation(string articulation)
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart($"{{\"time\":1,\"pad\":\"ride\",\"articulation\":\"{articulation}\"}}"), "easy"));
        }

        [Test]
        public void Rejects_articulation_that_is_not_valid_for_the_selected_pad()
        {
            Assert.Throws<ChartLoadException>(() => loader.Load(
                Chart("{\"time\":1,\"pad\":\"kick\",\"articulation\":\"bell\"}"), "easy"));
        }

        private static string Chart(string notes, double offset = 0, int version = 1)
        {
            return $"{{\"version\":{version},\"offsetSeconds\":{offset.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"\"difficulties\":{{\"easy\":[{notes}]}}}}";
        }
    }
}
