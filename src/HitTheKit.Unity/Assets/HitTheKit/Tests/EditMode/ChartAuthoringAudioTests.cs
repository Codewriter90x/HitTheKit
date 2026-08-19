using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class ChartAuthoringAudioTests
    {
        private string root;
        private string library;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hitthekit-authoring-audio-" + Guid.NewGuid().ToString("N"));
            library = Path.Combine(root, "library");
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(library);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Audio_import_creates_an_atomic_local_authoring_source_without_inventing_a_chart()
        {
            string source = Path.Combine(root, "My Backing.wav");
            WriteWave(source);
            byte[] original = File.ReadAllBytes(source);
            var request = new ChartAuthoringAudioRequest(source, "My Song", "Local Artist", 118, 96, 4);

            ChartAuthoringAudioImportResult result = new ChartAuthoringAudioImporter().Import(
                request,
                library,
                new DateTimeOffset(2026, 8, 19, 14, 0, 0, TimeSpan.Zero));

            Assert.That(result.Song.Id, Is.EqualTo("local-artist-my-song-authoring-20260819-140000"));
            Assert.That(result.Song.IsPlayable, Is.False);
            Assert.That(result.Song.CanAuthorChart, Is.True);
            Assert.That(result.Song.AudioAvailability, Is.EqualTo(SongAudioAvailability.Available));
            Assert.That(result.Song.ChartAvailability, Is.EqualTo(SongChartAvailability.Unavailable));
            Assert.That(result.Song.ChartPath, Is.Null);
            Assert.That(File.Exists(result.Song.AudioPath), Is.True);
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(original), "Import must not mutate the selected source file.");
            Assert.That(Directory.GetDirectories(library, ".hitthekit-audio-*"), Is.Empty);
        }

        [Test]
        public void Audio_import_rejects_unsupported_or_falsely_named_content_without_publishing_a_folder()
        {
            string mp3 = Path.Combine(root, "source.mp3");
            File.WriteAllBytes(mp3, new byte[] { 1, 2, 3 });
            string fakeWave = Path.Combine(root, "fake.wav");
            File.WriteAllBytes(fakeWave, new byte[] { 1, 2, 3, 4, 5 });

            Assert.Throws<ChartAuthoringAudioImportException>(() =>
                ChartAuthoringAudioImporter.ValidateSource(mp3));
            Assert.Throws<ChartAuthoringAudioImportException>(() =>
                new ChartAuthoringAudioImporter().Import(
                    new ChartAuthoringAudioRequest(fakeWave, "Song", "Artist", 120, 8, 4),
                    library,
                    DateTimeOffset.UtcNow));
            Assert.That(Directory.GetDirectories(library), Is.Empty);
        }

        [Test]
        public void Audio_only_song_uses_the_shared_gameplay_session_with_an_empty_authoring_timeline()
        {
            SongLibraryEntry song = ImportSong();

            GameplaySessionDefinition session = GameplaySessionFactory.ChartCreator(
                song,
                GameplayPresentationTheme.ArcadeNeon,
                0.6,
                "easy");

            Assert.That(session.IsChartCreator, Is.True);
            Assert.That(session.Chart, Is.EqualTo(GameplaySessionChart.AuthoringEmpty));
            Assert.That(session.ChartFilePath, Is.Null);
            Assert.That(session.AudioFilePath, Is.EqualTo(song.AudioPath));
            Assert.That(session.SpeedMultiplier, Is.EqualTo(0.6));
            Assert.That(session.Bpm, Is.EqualTo(song.Bpm.Value * 0.6));
        }

        [Test]
        public void Recorded_take_keeps_audio_local_while_the_portable_package_remains_chart_only()
        {
            string source = Path.Combine(root, "source.wav");
            WriteWave(source);
            var recording = new ChartRecordingSession(8);
            recording.Record(new DrumInputEvent(DrumPad.Kick, 100, 1, DrumInputSource.Test));
            var metadata = new ChartCreatorMetadata("source-song", "Source Song", "Local Artist", "easy", 120, 4, 4);

            ChartCreatorExportResult result = new ChartCreatorExporter().ExportChartOnly(
                recording.Finish(),
                metadata,
                ChartQuantization.None,
                library,
                new DateTimeOffset(2026, 8, 19, 14, 30, 0, TimeSpan.Zero),
                source);
            SongLibraryEntry local = new SongLibraryDiscovery().Discover(new[]
            {
                new SongLibraryRoot(library, SongLibraryOrigin.UserFolder)
            }).Songs.Single();

            Assert.That(result.IsLocallyPlayable, Is.True);
            Assert.That(File.Exists(result.LocalAudioPath), Is.True);
            Assert.That(local.IsPlayable, Is.True);
            using (var stream = File.OpenRead(result.PackagePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            using (var reader = new StreamReader(archive.GetEntry(HtkSongPackageService.ManifestEntryName).Open()))
            {
                string packageManifest = reader.ReadToEnd();
                Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EquivalentTo(new[]
                {
                    HtkSongPackageService.VersionEntryName,
                    HtkSongPackageService.ManifestEntryName,
                    HtkSongPackageService.ChartEntryName
                }));
                Assert.That(packageManifest, Does.Contain("\"audioAvailability\": \"missing\""));
                Assert.That(packageManifest, Does.Not.Contain("audioFile"));
            }
        }

        private SongLibraryEntry ImportSong()
        {
            string source = Path.Combine(root, "source.wav");
            WriteWave(source);
            return new ChartAuthoringAudioImporter().Import(
                new ChartAuthoringAudioRequest(source, "Song", "Artist", 120, 8, 4),
                library,
                new DateTimeOffset(2026, 8, 19, 15, 0, 0, TimeSpan.Zero)).Song;
        }

        private static void WriteWave(string path)
        {
            // Minimal PCM WAVE with one silent mono sample. The production loader verifies
            // the container signature; Unity decodes normal user WAV files at runtime.
            byte[] wave =
            {
                0x52, 0x49, 0x46, 0x46, 0x26, 0x00, 0x00, 0x00,
                0x57, 0x41, 0x56, 0x45, 0x66, 0x6d, 0x74, 0x20,
                0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                0x40, 0x1f, 0x00, 0x00, 0x80, 0x3e, 0x00, 0x00,
                0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            File.WriteAllBytes(path, wave);
        }
    }
}
