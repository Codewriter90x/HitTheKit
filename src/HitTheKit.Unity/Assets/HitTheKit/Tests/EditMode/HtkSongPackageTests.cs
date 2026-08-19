using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using HitTheKit.Core;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class HtkSongPackageTests
    {
        private string root;
        private string exportRoot;
        private string importRoot;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hitthekit-package-tests-" + Guid.NewGuid().ToString("N"));
            exportRoot = Path.Combine(root, "export");
            importRoot = Path.Combine(root, "import");
            Directory.CreateDirectory(exportRoot);
            Directory.CreateDirectory(importRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Chart_only_package_round_trip_imports_the_real_song_formats_without_audio()
        {
            ChartCreatorExportResult exported = ExportTake();
            string incoming = Path.Combine(importRoot, Path.GetFileName(exported.PackagePath));
            File.Copy(exported.PackagePath, incoming);

            HtkSongImportResult imported = new HtkSongPackageService().ImportChartOnlyPackage(incoming, importRoot);
            SongLibrarySnapshot library = new SongLibraryDiscovery().Discover(new[]
            {
                new SongLibraryRoot(importRoot, SongLibraryOrigin.UserFolder)
            });

            Assert.That(imported.Status, Is.EqualTo(HtkSongImportStatus.Imported));
            Assert.That(imported.SongId, Is.EqualTo(exported.SongId));
            Assert.That(Directory.GetFiles(imported.FolderPath).Select(Path.GetFileName),
                Is.EquivalentTo(new[] { "song.json", "notes.json" }));
            Assert.That(library.Diagnostics, Is.Empty);
            Assert.That(library.Songs.Single().AudioAvailability, Is.EqualTo(SongAudioAvailability.Missing));
            Assert.That(library.Songs.Single().ChartAvailability, Is.EqualTo(SongChartAvailability.Available));
            Assert.That(library.Songs.Single().IsPlayable, Is.False);
            Assert.That(File.ReadAllText(Path.Combine(imported.FolderPath, "notes.json")),
                Is.EqualTo(File.ReadAllText(exported.ChartPath)));
        }

        [Test]
        public void Imported_chart_can_bind_an_authorized_local_audio_copy_without_changing_the_package_or_source()
        {
            ChartCreatorExportResult exported = ExportTake();
            string incoming = Path.Combine(importRoot, Path.GetFileName(exported.PackagePath));
            File.Copy(exported.PackagePath, incoming);
            new HtkSongPackageService().ImportChartOnlyPackage(incoming, importRoot);
            SongLibraryEntry imported = DiscoverImportedSong();
            string source = Path.Combine(root, "authorized.wav");
            WriteWave(source);
            byte[] sourceBefore = File.ReadAllBytes(source);
            byte[] packageBefore = File.ReadAllBytes(incoming);

            SongAudioBindingResult result = new SongAudioBindingService().Bind(imported, source, importRoot);
            SongLibraryEntry rebound = DiscoverImportedSong();

            Assert.That(imported.CanBindAudio, Is.True);
            Assert.That(result.Song.IsPlayable, Is.True);
            Assert.That(rebound.IsPlayable, Is.True);
            Assert.That(rebound.CanBindAudio, Is.False);
            Assert.That(Path.GetFileName(rebound.AudioPath), Is.EqualTo("source-audio.wav"));
            Assert.That(Directory.GetFiles(rebound.FolderPath).Select(Path.GetFileName), Is.EquivalentTo(new[]
            {
                "song.json", "notes.json", "source-audio.wav"
            }));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBefore));
            Assert.That(File.ReadAllBytes(incoming), Is.EqualTo(packageBefore),
                "Local binding must never rewrite the portable chart-only package.");
            Assert.That(File.ReadAllText(Path.Combine(rebound.FolderPath, "song.json")),
                Does.Not.Contain(Path.GetFullPath(source)));
            Assert.That(Directory.GetFiles(rebound.FolderPath, ".hitthekit-bind-*"), Is.Empty);
        }

        [Test]
        public void Audio_binding_rejects_a_song_outside_the_declared_user_root_without_partial_files()
        {
            ChartCreatorExportResult exported = ExportTake();
            string incoming = Path.Combine(importRoot, Path.GetFileName(exported.PackagePath));
            File.Copy(exported.PackagePath, incoming);
            new HtkSongPackageService().ImportChartOnlyPackage(incoming, importRoot);
            SongLibraryEntry imported = DiscoverImportedSong();
            string source = Path.Combine(root, "authorized.wav");
            WriteWave(source);
            string differentRoot = Path.Combine(root, "different-root");
            Directory.CreateDirectory(differentRoot);

            SongAudioBindingException error = Assert.Throws<SongAudioBindingException>(() =>
                new SongAudioBindingService().Bind(imported, source, differentRoot));

            Assert.That(error.Message, Does.Contain("outside"));
            Assert.That(Directory.GetFiles(imported.FolderPath).Select(Path.GetFileName),
                Is.EquivalentTo(new[] { "song.json", "notes.json" }));
        }

        [Test]
        public void Inbox_import_is_deterministic_idempotent_and_preserves_the_portable_file()
        {
            ChartCreatorExportResult exported = ExportTake();
            string incoming = Path.Combine(importRoot, Path.GetFileName(exported.PackagePath));
            File.Copy(exported.PackagePath, incoming);
            var service = new HtkSongPackageService();

            HtkSongInboxResult first = service.ImportInbox(importRoot);
            HtkSongInboxResult second = service.ImportInbox(importRoot);

            Assert.That(first.ImportedSongIds, Is.EqualTo(new[] { exported.SongId }));
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(second.ImportedSongIds, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(File.Exists(incoming), Is.True, "The transferable package must remain available.");
            Assert.That(Directory.GetDirectories(importRoot, ".hitthekit-import-*"), Is.Empty);
        }

        [Test]
        public void Existing_song_folder_is_never_overwritten_by_an_import()
        {
            ChartCreatorExportResult exported = ExportTake();
            string incoming = Path.Combine(importRoot, Path.GetFileName(exported.PackagePath));
            File.Copy(exported.PackagePath, incoming);
            var service = new HtkSongPackageService();
            HtkSongImportResult first = service.ImportChartOnlyPackage(incoming, importRoot);
            string sentinel = Path.Combine(first.FolderPath, "local-change.txt");
            File.WriteAllText(sentinel, "keep me");

            HtkSongImportResult second = service.ImportChartOnlyPackage(incoming, importRoot);

            Assert.That(second.Status, Is.EqualTo(HtkSongImportStatus.AlreadyInstalled));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("keep me"));
        }

        [Test]
        public void Package_with_path_traversal_or_unknown_entry_is_rejected_before_extraction()
        {
            string package = Path.Combine(importRoot, "unsafe.htksong");
            WriteArchive(package,
                (HtkSongPackageService.VersionEntryName, "1\n"),
                (HtkSongPackageService.ManifestEntryName, ValidManifest("unsafe")),
                ("../notes.json", ValidChart()));

            HtkSongPackageException error = Assert.Throws<HtkSongPackageException>(() =>
                new HtkSongPackageService().ImportChartOnlyPackage(package, importRoot));

            Assert.That(error.Message, Does.Contain("unsupported entry"));
            Assert.That(File.Exists(Path.Combine(root, "notes.json")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(importRoot, "unsafe")), Is.False);
        }

        [Test]
        public void Package_with_case_insensitive_duplicate_entries_is_rejected()
        {
            string package = Path.Combine(importRoot, "duplicate.htksong");
            WriteArchive(package,
                (HtkSongPackageService.VersionEntryName, "1\n"),
                (HtkSongPackageService.ManifestEntryName, ValidManifest("duplicate")),
                ("SONG.JSON", ValidManifest("duplicate")));

            HtkSongPackageException error = Assert.Throws<HtkSongPackageException>(() =>
                new HtkSongPackageService().ImportChartOnlyPackage(package, importRoot));

            Assert.That(error.Message, Does.Contain("unique case-insensitively"));
            Assert.That(Directory.Exists(Path.Combine(importRoot, "duplicate")), Is.False);
        }

        [Test]
        public void Chart_only_package_cannot_smuggle_or_declare_audio()
        {
            string package = Path.Combine(importRoot, "audio.htksong");
            WriteArchive(package,
                (HtkSongPackageService.VersionEntryName, "1\n"),
                (HtkSongPackageService.ManifestEntryName, ValidManifest("audio")
                    .Replace("\"audioAvailability\":\"missing\"",
                        "\"audioAvailability\":\"available\",\"audioFile\":\"song.wav\"")),
                (HtkSongPackageService.ChartEntryName, ValidChart()));

            HtkSongPackageException error = Assert.Throws<HtkSongPackageException>(() =>
                new HtkSongPackageService().ImportChartOnlyPackage(package, importRoot));

            Assert.That(error.Message, Does.Contain("cannot contain or declare audio"));
            Assert.That(Directory.Exists(Path.Combine(importRoot, "audio")), Is.False);
        }

        [Test]
        public void Wrong_version_and_malformed_archives_are_fail_closed_and_reported_by_the_inbox()
        {
            string wrongVersion = Path.Combine(importRoot, "wrong-version.htksong");
            WriteArchive(wrongVersion,
                (HtkSongPackageService.VersionEntryName, "2\n"),
                (HtkSongPackageService.ManifestEntryName, ValidManifest("wrong-version")),
                (HtkSongPackageService.ChartEntryName, ValidChart()));
            File.WriteAllText(Path.Combine(importRoot, "malformed.htksong"), "not a zip");

            HtkSongInboxResult result = new HtkSongPackageService().ImportInbox(importRoot);

            Assert.That(result.ImportedSongIds, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(Directory.GetDirectories(importRoot), Is.Empty);
        }

        private ChartCreatorExportResult ExportTake()
        {
            var session = new ChartRecordingSession(8);
            session.Record(new DrumInputEvent(DrumPad.Kick, 100, 1, DrumInputSource.Test));
            var metadata = new ChartCreatorMetadata("portable-song", "Portable Song", "Local Player", "easy", 120, 4, 4);
            return new ChartCreatorExporter().ExportChartOnly(
                session.Finish(),
                metadata,
                ChartQuantization.None,
                exportRoot,
                new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero));
        }

        private SongLibraryEntry DiscoverImportedSong() => new SongLibraryDiscovery().Discover(new[]
        {
            new SongLibraryRoot(importRoot, SongLibraryOrigin.UserFolder)
        }).Songs.Single();

        private static void WriteWave(string path)
        {
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

        private static string ValidManifest(string id) =>
            "{\"schemaVersion\":1," +
            $"\"id\":\"{id}\",\"title\":\"Song\",\"artist\":\"Local Player\"," +
            "\"bpm\":120,\"bars\":4,\"beatsPerBar\":4," +
            "\"audioAvailability\":\"missing\"," +
            "\"chartAvailability\":\"available\",\"chartFile\":\"notes.json\"}";

        private static string ValidChart() =>
            "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{" +
            "\"easy\":[{\"time\":1,\"pad\":\"kick\"}]}}";

        private static void WriteArchive(string path, params (string Name, string Content)[] entries)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach ((string name, string content) in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(name);
                    using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
                }
            }
        }
    }
}
