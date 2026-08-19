using System;
using System.IO;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class ChartCreatorTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hitthekit-chart-creator-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Records_only_the_song_window_and_converts_slow_practice_time_to_the_original_timeline()
        {
            var session = new ChartRecordingSession(20, 0.5);

            Assert.That(session.Record(Input(DrumPad.Kick, -0.01)), Is.False);
            Assert.That(session.Record(Input(DrumPad.Kick, 4.0, 100, DrumInputSource.Midi)), Is.True);
            Assert.That(session.Record(Input(DrumPad.Snare, 20.01)), Is.False);
            ChartRecordingDraft draft = session.Finish();

            Assert.That(draft.DurationSeconds, Is.EqualTo(10));
            Assert.That(draft.Hits, Has.Count.EqualTo(1));
            Assert.That(draft.Hits[0].TimeSeconds, Is.EqualTo(2));
            Assert.That(draft.Hits[0].Velocity, Is.EqualTo(100));
            Assert.That(draft.Hits[0].Source, Is.EqualTo(DrumInputSource.Midi));
            Assert.That(session.IgnoredCount, Is.EqualTo(2));
            Assert.That(session.Record(Input(DrumPad.Ride, 5)), Is.False);
        }

        [Test]
        public void Restart_discards_the_previous_take_without_leaking_state()
        {
            var session = new ChartRecordingSession(10);
            session.Record(Input(DrumPad.Kick, 1));
            session.Finish();

            session.Restart();
            session.Record(Input(DrumPad.Snare, 2));
            ChartRecordingDraft second = session.Finish();

            Assert.That(second.Hits.Select(hit => hit.Pad), Is.EqualTo(new[] { DrumPad.Snare }));
            Assert.That(second.Hits[0].TimeSeconds, Is.EqualTo(2));
        }

        [Test]
        public void Raw_chart_is_deterministic_sorted_and_loadable_by_the_real_schema_loader()
        {
            var session = new ChartRecordingSession(10);
            session.Record(Input(DrumPad.Snare, 2));
            session.Record(Input(DrumPad.Crash, 1));
            session.Record(Input(DrumPad.Kick, 1));
            ChartRecordingDraft draft = session.Finish();

            string first = ChartCreatorJson.Serialize(draft, "hard", 120, ChartQuantization.None);
            string second = ChartCreatorJson.Serialize(draft, "hard", 120, ChartQuantization.None);
            LoadedChart loaded = new ChartLoader().Load(first, "hard");

            Assert.That(second, Is.EqualTo(first));
            Assert.That(loaded.Notes.Select(note => note.TimeSeconds), Is.EqualTo(new[] { 1d, 1d, 2d }));
            Assert.That(loaded.Notes.Select(note => note.Pad),
                Is.EqualTo(new[] { DrumPad.Crash, DrumPad.Kick, DrumPad.Snare }));
        }

        [Test]
        public void Quantization_is_non_destructive_and_uses_the_selected_musical_grid()
        {
            var session = new ChartRecordingSession(10);
            session.Record(Input(DrumPad.HiHat, 0.13));
            ChartRecordingDraft draft = session.Finish();

            LoadedChart raw = new ChartLoader().Load(
                ChartCreatorJson.Serialize(draft, "easy", 120, ChartQuantization.None), "easy");
            LoadedChart eighth = new ChartLoader().Load(
                ChartCreatorJson.Serialize(draft, "easy", 120, ChartQuantization.EighthNote), "easy");
            LoadedChart sixteenth = new ChartLoader().Load(
                ChartCreatorJson.Serialize(draft, "easy", 120, ChartQuantization.SixteenthNote), "easy");

            Assert.That(raw.Notes.Single().TimeSeconds, Is.EqualTo(0.13));
            Assert.That(eighth.Notes.Single().TimeSeconds, Is.EqualTo(0.25));
            Assert.That(sixteenth.Notes.Single().TimeSeconds, Is.EqualTo(0.125));
            Assert.That(draft.Hits.Single().TimeSeconds, Is.EqualTo(0.13), "Quantization must not mutate the raw take.");
        }

        [Test]
        public void Export_is_atomic_chart_only_and_discovered_as_unavailable_until_audio_is_bound()
        {
            var session = new ChartRecordingSession(10);
            session.Record(Input(DrumPad.Kick, 1));
            var metadata = new ChartCreatorMetadata(
                "my-local-song", "My Local Song", "Local Player", "easy", 120, 5, 4);
            DateTimeOffset created = new DateTimeOffset(2026, 8, 19, 10, 30, 0, TimeSpan.Zero);

            ChartCreatorExportResult result = new ChartCreatorExporter().ExportChartOnly(
                session.Finish(), metadata, ChartQuantization.SixteenthNote, root, created);
            SongLibrarySnapshot snapshot = new SongLibraryDiscovery().Discover(new[]
            {
                new SongLibraryRoot(root, SongLibraryOrigin.UserFolder)
            });

            Assert.That(result.SongId, Is.EqualTo("my-local-song-take-20260819-103000"));
            Assert.That(File.Exists(result.ChartPath), Is.True);
            Assert.That(File.Exists(result.ManifestPath), Is.True);
            Assert.That(Directory.GetFiles(result.FolderPath).Select(Path.GetFileName),
                Is.EquivalentTo(new[] { "notes.json", "song.json" }));
            Assert.That(Directory.GetDirectories(root, ".hitthekit-chart-*"), Is.Empty);
            Assert.That(snapshot.Diagnostics, Is.Empty);
            Assert.That(snapshot.Songs.Single().IsPlayable, Is.False);
            Assert.That(snapshot.Songs.Single().AudioAvailability, Is.EqualTo(SongAudioAvailability.Missing));
            Assert.That(snapshot.Songs.Single().ChartAvailability, Is.EqualTo(SongChartAvailability.Available));
            Assert.That(snapshot.Songs.Single().AvailableDifficulties, Is.EqualTo(new[] { "easy" }));
        }

        [Test]
        public void Repeated_export_never_overwrites_an_existing_take()
        {
            var session = new ChartRecordingSession(10);
            session.Record(Input(DrumPad.Kick, 1));
            ChartRecordingDraft draft = session.Finish();
            var metadata = new ChartCreatorMetadata("song", "Song", "Player", "full", 90, 4, 4);
            var exporter = new ChartCreatorExporter();
            DateTimeOffset created = new DateTimeOffset(2026, 8, 19, 10, 30, 0, TimeSpan.Zero);

            ChartCreatorExportResult first = exporter.ExportChartOnly(draft, metadata, ChartQuantization.None, root, created);
            ChartCreatorExportResult second = exporter.ExportChartOnly(draft, metadata, ChartQuantization.None, root, created);

            Assert.That(first.SongId, Is.EqualTo("song-take-20260819-103000"));
            Assert.That(second.SongId, Is.EqualTo("song-take-20260819-103000-2"));
            Assert.That(Directory.GetDirectories(root), Has.Length.EqualTo(2));
        }

        [Test]
        public void Empty_take_is_rejected_without_publishing_a_partial_song_folder()
        {
            var metadata = new ChartCreatorMetadata("song", "Song", "Player", "easy", 120, 4, 4);
            ChartRecordingDraft draft = new ChartRecordingSession(8).Finish();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                new ChartCreatorExporter().ExportChartOnly(
                    draft,
                    metadata,
                    ChartQuantization.None,
                    root,
                    new DateTimeOffset(2026, 8, 19, 10, 30, 0, TimeSpan.Zero)));

            Assert.That(error.Message, Does.Contain("at least one hit"));
            Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
        }

        private static DrumInputEvent Input(
            DrumPad pad,
            double time,
            int velocity = 96,
            DrumInputSource source = DrumInputSource.Keyboard) =>
            new DrumInputEvent(pad, velocity, time, source);
    }
}
