using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using NUnit.Framework;
using UnityEngine;

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
        public void Visual_editor_updates_adds_and_deletes_individual_notes_without_mutating_the_raw_take()
        {
            var session = new ChartRecordingSession(8);
            session.Record(Input(DrumPad.Kick, 1));
            session.Record(Input(DrumPad.Snare, 2));
            ChartRecordingDraft raw = session.Finish();
            var editor = new ChartDraftEditor(raw);

            int moved = editor.Update(0, 2.5, DrumPad.Ride);
            int added = editor.Add(1.5, DrumPad.HiHat, 88);
            editor.Delete(0);
            ChartRecordingDraft edited = editor.BuildDraft();

            Assert.That(moved, Is.EqualTo(1), "Moving the first note after the snare must re-sort the visual timeline.");
            Assert.That(added, Is.EqualTo(0));
            Assert.That(raw.Hits.Select(hit => (hit.TimeSeconds, hit.Pad)), Is.EqualTo(new[]
            {
                (1d, DrumPad.Kick),
                (2d, DrumPad.Snare)
            }), "The captured raw take is immutable.");
            Assert.That(edited.Hits.Select(hit => (hit.TimeSeconds, hit.Pad)), Is.EqualTo(new[]
            {
                (2d, DrumPad.Snare),
                (2.5d, DrumPad.Ride)
            }));
            Assert.That(new ChartLoader().Load(
                ChartCreatorJson.Serialize(edited, "easy", 120, ChartQuantization.None), "easy").Notes,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void Visual_editor_and_json_round_trip_preserve_velocity_and_articulation()
        {
            var session = new ChartRecordingSession(8);
            session.Record(Input(
                DrumPad.Ride,
                1,
                73,
                DrumInputSource.Midi,
                DrumArticulation.Bow));
            var editor = new ChartDraftEditor(session.Finish());

            editor.Update(0, 1.25, DrumPad.Ride, 119, DrumArticulation.Bell);
            editor.Add(2, DrumPad.Snare, 86, DrumArticulation.Rim);
            LoadedChart loaded = new ChartLoader().Load(
                ChartCreatorJson.Serialize(editor.BuildDraft(), "easy", 120, ChartQuantization.None),
                "easy");

            Assert.That(loaded.Notes.Select(note => (note.Velocity, note.Articulation)), Is.EqualTo(new[]
            {
                ((int?)119, DrumArticulation.Bell),
                ((int?)86, DrumArticulation.Rim)
            }));
        }

        [Test]
        public void Waveform_envelope_scrubbing_and_zoom_are_deterministic_and_bounded()
        {
            float[] samples =
            {
                0.1f, -0.2f,
                0.8f, 0.4f,
                -0.5f, 0.3f,
                1.0f, -0.9f
            };
            ChartWaveformModel first = ChartWaveformModel.FromInterleaved(samples, 2, 4, 4);
            ChartWaveformModel second = ChartWaveformModel.FromInterleaved(samples, 2, 4, 4);

            Assert.That(Enumerable.Range(0, first.PeakCount).Select(first.PeakAt),
                Is.EqualTo(Enumerable.Range(0, second.PeakCount).Select(second.PeakAt)));
            Assert.That(first.Scrub(0.5), Is.EqualTo(2).Within(0.000001));
            first.Zoom(2);
            Assert.That(first.ViewEndSeconds - first.ViewStartSeconds, Is.EqualTo(2).Within(0.000001));
            Assert.That(first.SelectedTimeSeconds, Is.InRange(first.ViewStartSeconds, first.ViewEndSeconds));
            Assert.That(first.Scrub(-1), Is.EqualTo(first.ViewStartSeconds));
            Assert.That(first.Scrub(2), Is.EqualTo(first.ViewEndSeconds));
            first.ResetZoom();
            Assert.That((first.ViewStartSeconds, first.ViewEndSeconds), Is.EqualTo((0d, 4d)));
        }

        [Test]
        public void Waveform_reads_the_complete_audio_clip_in_bounded_chunks()
        {
            AudioClip clip = AudioClip.Create("waveform-test", 1024, 1, 1024, false);
            var samples = new float[1024];
            samples[10] = 0.75f;
            samples[900] = -1f;
            Assert.That(clip.SetData(samples, 0), Is.True);

            try
            {
                ChartWaveformModel model = ChartWaveformModel.FromAudioClip(clip, 8, 16);

                Assert.That(model.PeakAt(0), Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(model.PeakAt(7), Is.EqualTo(1f).Within(0.0001f));
                Assert.That(model.DurationSeconds, Is.EqualTo(1).Within(0.0001));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Visual_editor_rejects_invalid_indices_times_pads_and_velocity()
        {
            var session = new ChartRecordingSession(4);
            session.Record(Input(DrumPad.Kick, 1));
            var editor = new ChartDraftEditor(session.Finish());

            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Update(-1, 1, DrumPad.Kick));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Update(0, -0.01, DrumPad.Kick));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Update(0, 4.01, DrumPad.Kick));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Update(0, 1, (DrumPad)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Add(1, DrumPad.Snare, 128));
            Assert.Throws<ArgumentException>(() =>
                editor.Add(1, DrumPad.Kick, 100, DrumArticulation.Bell));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.Delete(2));
            Assert.That(editor.Notes, Has.Count.EqualTo(1));
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
            Assert.That(File.Exists(result.PackagePath), Is.True);
            Assert.That(Directory.GetFiles(result.FolderPath).Select(Path.GetFileName),
                Is.EquivalentTo(new[] { "notes.json", "song.json" }));
            using (var packageStream = File.OpenRead(result.PackagePath))
            using (var package = new ZipArchive(packageStream, ZipArchiveMode.Read))
            {
                Assert.That(package.Entries.Select(entry => entry.FullName), Is.EqualTo(new[]
                {
                    HtkSongPackageService.VersionEntryName,
                    HtkSongPackageService.ManifestEntryName,
                    HtkSongPackageService.ChartEntryName
                }));
            }
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
            Assert.That(Directory.GetFiles(root, "*.htksong"), Has.Length.EqualTo(2));
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
            DrumInputSource source = DrumInputSource.Keyboard,
            DrumArticulation articulation = DrumArticulation.Default) =>
            new DrumInputEvent(pad, velocity, time, source, articulation);
    }
}
