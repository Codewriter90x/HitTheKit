using System;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayHighwayTests
    {
        private static readonly string[] EnvironmentBackgroundPaths =
        {
            "Assets/HitTheKit/UI/Gameplay/Backgrounds/arcade-neon-environment-v2.png",
            "Assets/HitTheKit/UI/Gameplay/Backgrounds/concert-stage-environment-v2.png",
            "Assets/HitTheKit/UI/Gameplay/Backgrounds/precision-grid-environment-v2.png"
        };

        [Test]
        public void Defines_complete_unique_drum_lane_set_with_distinct_kick_track()
        {
            Assert.That(GameplayHighwayLanes.All, Has.Count.EqualTo(8));
            Assert.That(GameplayHighwayLanes.All.Select(lane => lane.Pad), Is.Unique);
            Assert.That(GameplayHighwayLanes.All.Select(lane => lane.Id), Is.Unique);
            Assert.That(GameplayHighwayLanes.HighwayLaneCount, Is.EqualTo(7));
            Assert.That(GameplayHighwayLanes.HighwayIndex(DrumPad.Kick), Is.EqualTo(-1));
            Assert.That(GameplayHighwayLanes.Find(DrumPad.Kick).Subtitle, Is.EqualTo("GRANCASSA"));
        }

        [Test]
        public void Exposes_three_visually_distinct_theme_palettes()
        {
            GameplayThemePalette arcade = GameplayThemePalette.For(GameplayPresentationTheme.ArcadeNeon);
            GameplayThemePalette concert = GameplayThemePalette.For(GameplayPresentationTheme.ConcertStage);
            GameplayThemePalette precision = GameplayThemePalette.For(GameplayPresentationTheme.PrecisionGrid);

            Assert.That(arcade.Kick, Is.Not.EqualTo(concert.Kick));
            Assert.That(concert.Grid, Is.Not.EqualTo(precision.Grid));
            Assert.That(precision.SurfaceOpacity, Is.GreaterThan(concert.SurfaceOpacity));
        }

        [Test]
        public void Exposes_three_distinct_environment_compositions_and_note_shapes()
        {
            GameplayEnvironmentProfile arcade = GameplayEnvironmentProfile.For(GameplayPresentationTheme.ArcadeNeon);
            GameplayEnvironmentProfile concert = GameplayEnvironmentProfile.For(GameplayPresentationTheme.ConcertStage);
            GameplayEnvironmentProfile precision = GameplayEnvironmentProfile.For(GameplayPresentationTheme.PrecisionGrid);

            Assert.That(new[] { arcade.Title, concert.Title, precision.Title }, Is.Unique);
            Assert.That(new[] { arcade.NoteShape, concert.NoteShape, precision.NoteShape }, Is.Unique);
            Assert.That(arcade.ShowsInstructionalKit, Is.False);
            Assert.That(concert.ShowsInstructionalKit, Is.False);
            Assert.That(precision.ShowsInstructionalKit, Is.True,
                "The colored perspective kit is the opt-in Precision Grid presentation.");
            Assert.That(concert.StrikeRatio, Is.LessThan(arcade.StrikeRatio),
                "The concert layout must leave the foreground drum kit visible.");
            Assert.That(precision.HorizonRatio, Is.GreaterThan(arcade.HorizonRatio),
                "The technical chamber uses a deliberately flatter training perspective.");
            Assert.That(arcade.BottomRightRatio - arcade.BottomLeftRatio,
                Is.GreaterThan(concert.BottomRightRatio - concert.BottomLeftRatio));
        }

        [Test]
        public void Environment_backgrounds_are_three_distinct_widescreen_assets()
        {
            var textures = EnvironmentBackgroundPaths
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .ToArray();

            Assert.That(textures, Has.All.Not.Null);
            Assert.That(textures.Select(AssetDatabase.GetAssetPath), Is.Unique);
            Assert.That(textures.All(texture => texture.width >= 1600), Is.True);
            Assert.That(textures.All(texture => texture.height >= 900), Is.True);
        }

        [Test]
        public void Surface_accepts_canonical_chart_notes_for_every_lane()
        {
            LoadedChart chart = new ChartLoader().Load(
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"easy\":[" +
                "{\"time\":1,\"pad\":\"kick\"},{\"time\":1,\"pad\":\"snare\"}," +
                "{\"time\":1,\"pad\":\"hiHat\"},{\"time\":1,\"pad\":\"tom1\"}," +
                "{\"time\":1,\"pad\":\"tom2\"},{\"time\":1,\"pad\":\"floorTom\"}," +
                "{\"time\":1,\"pad\":\"crash\"},{\"time\":1,\"pad\":\"ride\"}]}}",
                "easy");
            var timeline = new ChartTimeline(chart);
            var surface = new GameplayHighwaySurface();

            surface.SetTheme(GameplayPresentationTheme.PrecisionGrid);
            surface.SetFrame(timeline.Notes, 0, 4, DrumPad.Kick, 1);

            Assert.That(surface.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(surface.Notes.Select(note => note.Note.Pad),
                Is.EquivalentTo(Enum.GetValues(typeof(DrumPad)).Cast<DrumPad>()));
        }

        [Test]
        public void Kit_surface_builds_a_colored_target_for_every_lane()
        {
            var surface = new GameplayKitSurface();

            Assert.That(surface.PieceCount, Is.EqualTo(8));
            foreach (DrumPad pad in Enum.GetValues(typeof(DrumPad)))
            {
                Assert.That(surface.TryGetTargetState(pad, out GameplayKitTargetState state), Is.True);
                Assert.That(state.Pad, Is.EqualTo(pad));
                Assert.That(state.Zone, Is.EqualTo(GameplayKitZoneResolver.DefaultFor(pad)));
                Assert.That(state.Intensity, Is.Zero);
            }
        }

        [Test]
        public void Kit_target_projection_is_absolute_and_aligns_simultaneous_hits()
        {
            LoadedChart chart = new ChartLoader().Load(
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"easy\":[" +
                "{\"time\":1,\"pad\":\"kick\"},{\"time\":1,\"pad\":\"crash\"}," +
                "{\"time\":1.1,\"pad\":\"snare\"}]}}",
                "easy");
            var timeline = new ChartTimeline(chart);
            var calculator = new GameplayKitTargetStateCalculator();

            GameplayKitTargetState kick = calculator.Calculate(
                DrumPad.Kick, timeline.Notes, 0.5, 1.0, null, 0);
            GameplayKitTargetState crash = calculator.Calculate(
                DrumPad.Crash, timeline.Notes, 0.5, 1.0, null, 0);
            GameplayKitTargetState direct = calculator.Calculate(
                DrumPad.Kick, timeline.Notes, 0.5, 1.0, null, 0);

            Assert.That(kick.Intensity, Is.EqualTo(crash.Intensity).Within(0.0001f));
            Assert.That(direct.Intensity, Is.EqualTo(kick.Intensity).Within(0.0001f),
                "The visual state must derive from absolute song position, not accumulated frames.");
            Assert.That(kick.Zone, Is.EqualTo(GameplayKitZone.Pedal));
            Assert.That(crash.Zone, Is.EqualTo(GameplayKitZone.Edge));
        }

        [Test]
        public void Kit_surface_explains_simultaneous_targets_in_beginner_language()
        {
            LoadedChart chart = new ChartLoader().Load(
                "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"easy\":[" +
                "{\"time\":1,\"pad\":\"kick\"},{\"time\":1,\"pad\":\"crash\"}]}}",
                "easy");
            var surface = new GameplayKitSurface();

            surface.SetFrame(new ChartTimeline(chart).Notes, 0.5, 1.0, null, 0);

            Assert.That(surface.GuidanceText, Does.StartWith("INSIEME"));
            Assert.That(surface.GuidanceText, Does.Contain("CASSA"));
            Assert.That(surface.GuidanceText, Does.Contain("CRASH"));
            Assert.That(surface.GuidanceText, Does.Contain("PEDALE"));
            Assert.That(surface.GuidanceText, Does.Contain("BORDO"));
        }

        [TestCase(DrumPad.Kick, GameplayKitZone.Pedal, "CASSA")]
        [TestCase(DrumPad.Snare, GameplayKitZone.Head, "RULLANTE")]
        [TestCase(DrumPad.HiHat, GameplayKitZone.Bow, "CHARLESTON")]
        [TestCase(DrumPad.Tom1, GameplayKitZone.Head, "TOM 1")]
        [TestCase(DrumPad.Tom2, GameplayKitZone.Head, "TOM 2")]
        [TestCase(DrumPad.FloorTom, GameplayKitZone.Head, "TIMPANO")]
        [TestCase(DrumPad.Crash, GameplayKitZone.Edge, "CRASH")]
        [TestCase(DrumPad.Ride, GameplayKitZone.Bow, "RIDE")]
        public void Beginner_target_copy_identifies_piece_and_physical_zone(
            DrumPad pad,
            GameplayKitZone expectedZone,
            string expectedName)
        {
            Assert.That(GameplayKitZoneResolver.DefaultFor(pad), Is.EqualTo(expectedZone));
            Assert.That(GameplayKitZoneResolver.BeginnerPieceName(pad), Does.Contain(expectedName));
            Assert.That(GameplayKitZoneResolver.BeginnerInstruction(pad), Does.Contain(expectedName));
            Assert.That(
                GameplayKitZoneResolver.BeginnerInstruction(pad),
                Does.Contain(GameplayKitZoneResolver.ZoneLabel(expectedZone)));
        }

        [Test]
        public void Rejects_unknown_theme_and_pad_values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplayThemePalette.For((GameplayPresentationTheme)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplayEnvironmentProfile.For((GameplayPresentationTheme)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplayHighwayLanes.Find((DrumPad)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplayKitZoneResolver.DefaultFor((DrumPad)99));
        }

        [Test]
        public void Highway_uses_a_broad_impact_zone_and_bounded_multi_pass_note_glow()
        {
            Assert.That(GameplayHighwaySurface.ImpactZoneHalfHeight, Is.GreaterThanOrEqualTo(8f));
            Assert.That(GameplayHighwaySurface.NoteGlowPasses, Is.InRange(2, 4));
        }
    }
}
