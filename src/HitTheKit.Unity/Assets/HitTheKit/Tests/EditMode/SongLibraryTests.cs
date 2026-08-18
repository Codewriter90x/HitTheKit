using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class SongLibraryTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "hitthekit-song-library-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Discovers_generated_and_incomplete_external_songs_in_stable_order()
        {
            WriteManifest("generated", Manifest("generated", "Neon Circuit", "HitTheKit", 20, "generated"));
            WriteManifest("local-example", Manifest(
                "local-song-example", "Local Song Example", "Your Library", 10, "audioFile",
                "notes.json", "song.ogg"));

            SongLibrarySnapshot snapshot = Discover(root, SongLibraryOrigin.Bundled);

            Assert.That(snapshot.Songs.Select(song => song.Id), Is.EqualTo(new[]
            {
                "local-song-example",
                "generated"
            }));
            Assert.That(snapshot.Songs[0].Availability, Is.EqualTo(SongLibraryAvailability.Unavailable));
            Assert.That(snapshot.Songs[0].MissingFiles, Is.EquivalentTo(new[] { "notes.json", "song.ogg" }));
            Assert.That(snapshot.Songs[1].IsPlayable, Is.True);
        }

        [Test]
        public void External_song_is_playable_only_when_audio_and_a_valid_chart_exist()
        {
            string folder = WriteManifest("external", Manifest(
                "external-song", "Folder Song", "Player", 1, "audioFile", "notes.json", "song.wav"));
            File.WriteAllText(Path.Combine(folder, "notes.json"), ValidChart);
            WriteWaveHeader(Path.Combine(folder, "song.wav"));

            SongLibraryEntry song = Discover(root, SongLibraryOrigin.UserFolder).Songs.Single();

            Assert.That(song.IsPlayable, Is.True);
            Assert.That(song.Origin, Is.EqualTo(SongLibraryOrigin.UserFolder));
            Assert.That(song.ChartPath, Is.EqualTo(Path.Combine(folder, "notes.json")));
            Assert.That(song.AudioPath, Is.EqualTo(Path.Combine(folder, "song.wav")));
            Assert.That(song.AvailableDifficulties, Is.EqualTo(new[] { "easy" }));
        }

        [Test]
        public void Invalid_chart_is_ignored_with_a_bounded_diagnostic_instead_of_crashing_gameplay()
        {
            string folder = WriteManifest("bad", Manifest(
                "bad-song", "Bad Song", "Player", 1, "audioFile", "notes.json", "song.ogg"));
            File.WriteAllText(Path.Combine(folder, "notes.json"), "{}");
            File.WriteAllBytes(Path.Combine(folder, "song.ogg"), new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' });

            SongLibrarySnapshot snapshot = Discover(root, SongLibraryOrigin.UserFolder);

            Assert.That(snapshot.Songs, Is.Empty);
            Assert.That(snapshot.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(snapshot.Diagnostics[0], Does.Contain("valid schema-v1 chart"));
            Assert.That(snapshot.Diagnostics[0], Does.Not.Contain(root));
        }

        [Test]
        public void Audio_extension_without_the_matching_file_signature_is_not_marked_ready()
        {
            string folder = WriteManifest("bad-audio", Manifest(
                "bad-audio", "Bad Audio", "Player", 1, "audioFile", "notes.json", "song.ogg"));
            File.WriteAllText(Path.Combine(folder, "notes.json"), ValidChart);
            File.WriteAllBytes(Path.Combine(folder, "song.ogg"), new byte[] { 1, 2, 3, 4 });

            SongLibrarySnapshot snapshot = Discover(root, SongLibraryOrigin.UserFolder);

            Assert.That(snapshot.Songs, Is.Empty);
            Assert.That(snapshot.Diagnostics.Single(), Does.Contain("declared OGG/WAVE format"));
        }

        [TestCase("../outside.json", "song.ogg")]
        [TestCase("notes.json", "../outside.ogg")]
        public void Song_files_cannot_escape_their_folder(string chart, string audio)
        {
            WriteManifest("escape", Manifest(
                "escape", "Escape", "Player", 1, "audioFile", chart, audio));

            SongLibrarySnapshot snapshot = Discover(root, SongLibraryOrigin.UserFolder);

            Assert.That(snapshot.Songs, Is.Empty);
            Assert.That(snapshot.Diagnostics.Single(), Does.Contain("escapes its song folder"));
        }

        [Test]
        public void Song_files_cannot_escape_through_an_intermediate_symbolic_link()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
                Assert.Ignore("The symbolic-link boundary regression is exercised on the supported macOS Editor target.");

            string outside = Path.Combine(root, "outside-assets");
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(outside, "notes.json"), ValidChart);
            WriteWaveHeader(Path.Combine(outside, "song.wav"));

            string songFolder = WriteManifest("linked-escape", Manifest(
                "linked-escape", "Linked Escape", "Player", 1, "audioFile",
                "linked/notes.json", "linked/song.wav"));
            string link = Path.Combine(songFolder, "linked");
            Assert.That(CreateSymbolicLink(outside, link), Is.EqualTo(0),
                $"Could not create the test symbolic link (errno {Marshal.GetLastWin32Error()}).");

            SongLibrarySnapshot snapshot = Discover(root, SongLibraryOrigin.UserFolder);

            Assert.That(snapshot.Songs, Is.Empty);
            Assert.That(snapshot.Diagnostics.Single(), Does.Contain("cannot traverse a symbolic link"));
            Assert.That(snapshot.Diagnostics.Single(), Does.Not.Contain(root));
        }

        [Test]
        public void User_song_wins_a_duplicate_id_without_depending_on_directory_order()
        {
            string bundled = Path.Combine(root, "bundled");
            string user = Path.Combine(root, "user");
            Directory.CreateDirectory(bundled);
            Directory.CreateDirectory(user);
            WriteManifest(Path.Combine(bundled, "same"), Manifest("same", "Bundled", "Band", 1, "generated"), true);
            WriteManifest(Path.Combine(user, "same"), Manifest("same", "User", "Band", 1, "generated"), true);

            SongLibrarySnapshot snapshot = new SongLibraryDiscovery().Discover(new[]
            {
                new SongLibraryRoot(user, SongLibraryOrigin.UserFolder),
                new SongLibraryRoot(bundled, SongLibraryOrigin.Bundled)
            });

            Assert.That(snapshot.Songs.Single().Title, Is.EqualTo("User"));
            Assert.That(snapshot.Songs.Single().Origin, Is.EqualTo(SongLibraryOrigin.UserFolder));
            Assert.That(snapshot.Diagnostics.Single(), Does.Contain("Duplicate song ID"));
        }

        [Test]
        public void Runtime_creates_the_documents_song_root_automatically()
        {
            string documents = Path.Combine(root, "Documents");
            string documentsSongs = SongLibraryRuntime.DocumentsRoot(documents);
            string legacy = Path.Combine(root, "legacy");
            string bundled = Path.Combine(root, "bundled");
            Directory.CreateDirectory(legacy);
            Directory.CreateDirectory(bundled);
            WriteManifest(Path.Combine(legacy, "legacy-song"),
                Manifest("legacy-song", "Legacy", "Player", 1, "generated"), true);

            SongLibrarySnapshot snapshot = SongLibraryRuntime.Discover(
                documentsSongs,
                legacy,
                bundled);

            Assert.That(documentsSongs, Is.EqualTo(Path.Combine(documents, "HTKSongs")));
            Assert.That(Directory.Exists(documentsSongs), Is.True);
            Assert.That(snapshot.Songs.Single().Id, Is.EqualTo("legacy-song"));
            Assert.That(snapshot.Diagnostics, Is.Empty);
        }

        [Test]
        public void User_profile_root_includes_the_documents_directory()
        {
            string userProfile = Path.Combine(root, "player-home");

            string songRoot = SongLibraryRuntime.UserProfileRoot(userProfile);

            Assert.That(songRoot, Is.EqualTo(Path.Combine(userProfile, "Documents", "HTKSongs")));
        }

        [Test]
        public void Runtime_user_root_does_not_treat_the_unix_home_as_documents()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            Assert.That(userProfile, Is.Not.Empty);
            Assert.That(SongLibraryRuntime.UserRoot,
                Is.EqualTo(Path.Combine(userProfile, "Documents", "HTKSongs")));
            Assert.That(SongLibraryRuntime.UserRoot,
                Is.Not.EqualTo(Path.Combine(userProfile, "HTKSongs")));
        }

        [Test]
        public void Documents_song_wins_legacy_and_bundled_duplicates()
        {
            string documents = Path.Combine(root, "documents-songs");
            string legacy = Path.Combine(root, "legacy");
            string bundled = Path.Combine(root, "bundled");
            Directory.CreateDirectory(documents);
            Directory.CreateDirectory(legacy);
            Directory.CreateDirectory(bundled);
            WriteManifest(Path.Combine(documents, "same"),
                Manifest("same", "Documents", "Band", 1, "generated"), true);
            WriteManifest(Path.Combine(legacy, "same"),
                Manifest("same", "Legacy", "Band", 1, "generated"), true);
            WriteManifest(Path.Combine(bundled, "same"),
                Manifest("same", "Bundled", "Band", 1, "generated"), true);

            SongLibrarySnapshot snapshot = SongLibraryRuntime.Discover(documents, legacy, bundled);

            Assert.That(snapshot.Songs.Single().Title, Is.EqualTo("Documents"));
            Assert.That(snapshot.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(snapshot.Diagnostics.All(message => message.Contains("Duplicate song ID")), Is.True);
        }

        [Test]
        public void Documents_creation_failure_preserves_legacy_and_bundled_discovery()
        {
            string documents = Path.Combine(root, "documents-is-a-file");
            string legacy = Path.Combine(root, "legacy");
            string bundled = Path.Combine(root, "bundled");
            File.WriteAllText(documents, "not a directory");
            Directory.CreateDirectory(legacy);
            Directory.CreateDirectory(bundled);
            WriteManifest(Path.Combine(legacy, "legacy-song"),
                Manifest("legacy-song", "Legacy", "Player", 1, "generated"), true);

            SongLibrarySnapshot snapshot = SongLibraryRuntime.Discover(documents, legacy, bundled);

            Assert.That(snapshot.Songs.Single().Id, Is.EqualTo("legacy-song"));
            Assert.That(snapshot.Diagnostics.Single(), Does.Contain("Could not create the Documents song folder"));
            Assert.That(snapshot.Diagnostics.Single(), Does.Not.Contain(root));
        }

        [Test]
        public void External_song_session_uses_detected_files_and_returns_to_the_library()
        {
            string folder = WriteManifest("external", Manifest(
                "external-song", "Folder Song", "Player", 1, "audioFile", "notes.json", "song.wav"));
            File.WriteAllText(Path.Combine(folder, "notes.json"), ValidChart);
            WriteWaveHeader(Path.Combine(folder, "song.wav"));
            SongLibraryEntry song = Discover(root, SongLibraryOrigin.UserFolder).Songs.Single();

            GameplaySessionDefinition session = GameplaySessionFactory.Song(
                song, GameplayPresentationTheme.ConcertStage);

            Assert.That(session.Chart, Is.EqualTo(GameplaySessionChart.ExternalFile));
            Assert.That(session.Difficulty, Is.EqualTo("easy"),
                "A catalog difficulty hint must not become a technical chart difficulty ID.");
            Assert.That(session.ChartFilePath, Is.EqualTo(song.ChartPath));
            Assert.That(session.AudioFilePath, Is.EqualTo(song.AudioPath));
            Assert.That(session.ReturnTarget, Is.EqualTo(GameplayReturnTarget.SongLibrary));
            Assert.That(session.SongId, Is.EqualTo(song.Id));
        }

        [Test]
        public void Song_session_uses_a_chart_difficulty_speed_and_six_second_minimum_count_in()
        {
            string folder = WriteManifest("external", Manifest(
                "external-song", "Folder Song", "Player", 1, "audioFile", "notes.json", "song.wav"));
            File.WriteAllText(Path.Combine(folder, "notes.json"), MultiDifficultyChart);
            WriteWaveHeader(Path.Combine(folder, "song.wav"));
            SongLibraryEntry song = Discover(root, SongLibraryOrigin.UserFolder).Songs.Single();

            GameplaySessionDefinition session = GameplaySessionFactory.Song(
                song,
                GameplayPresentationTheme.ConcertStage,
                0.5,
                "hard");

            Assert.That(song.AvailableDifficulties, Is.EqualTo(new[] { "easy", "hard" }));
            Assert.That(session.Difficulty, Is.EqualTo("hard"));
            Assert.That(session.SpeedMultiplier, Is.EqualTo(0.5));
            Assert.That(session.Bpm, Is.EqualTo(60));
            Assert.That(session.CountInBeats * 60.0 / session.Bpm,
                Is.GreaterThanOrEqualTo(GameplaySessionFactory.MinimumSongCountInSeconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameplaySessionFactory.Song(
                song,
                GameplayPresentationTheme.ConcertStage,
                0.5,
                "expert"));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameplaySessionFactory.Song(
                song,
                GameplayPresentationTheme.ConcertStage,
                0.2,
                "easy"));
        }

        [Test]
        public void Manifest_contract_is_explicit_and_fail_closed()
        {
            var discovery = new SongLibraryDiscovery();

            Assert.Throws<SongLibraryLoadException>(() => discovery.Parse(
                Manifest("bad id", "Song", "Band", 1, "generated"), root, SongLibraryOrigin.UserFolder));
            Assert.Throws<SongLibraryLoadException>(() => discovery.Parse(
                Manifest("song", "Song", "Band", 1, "audioFile", "notes.json", "song.ogg")
                    .Replace("\"audioAvailability\":\"available\"", "\"audioAvailability\":\"Available\""),
                root,
                SongLibraryOrigin.UserFolder));
            Assert.Throws<SongLibraryLoadException>(() => discovery.Parse(
                "{\"schemaVersion\":2,\"id\":\"song\"}", root, SongLibraryOrigin.UserFolder));
        }

        [Test]
        public void Metadata_only_manifest_preserves_unknowns_without_future_filename_assumptions()
        {
            const string manifest =
                "{\"schemaVersion\":1,\"id\":\"metadata-only\"," +
                "\"title\":\"Metadata Only\",\"artist\":\"Band\"," +
                "\"audioAvailability\":\"missing\"," +
                "\"chartAvailability\":\"unavailable\"}";

            SongLibraryEntry song = new SongLibraryDiscovery().Parse(
                manifest, root, SongLibraryOrigin.Bundled);

            Assert.That(song.Album, Is.Null);
            Assert.That(song.Genre, Is.Null);
            Assert.That(song.Year, Is.Null);
            Assert.That(song.Bpm, Is.Null);
            Assert.That(song.Bars, Is.Null);
            Assert.That(song.BeatsPerBar, Is.Null);
            Assert.That(song.DifficultyHint, Is.Null);
            Assert.That(song.AudioAvailability, Is.EqualTo(SongAudioAvailability.Missing));
            Assert.That(song.ChartAvailability, Is.EqualTo(SongChartAvailability.Unavailable));
            Assert.That(song.AudioPath, Is.Null);
            Assert.That(song.ChartPath, Is.Null);
            Assert.That(song.AvailableDifficulties, Is.Empty);
            Assert.That(song.MissingFiles, Is.Empty);
            Assert.That(song.IsPlayable, Is.False);
            Assert.Throws<InvalidOperationException>(() => GameplaySessionFactory.Song(
                song,
                GameplayPresentationTheme.ArcadeNeon));
        }

        [TestCase("{\"schemaVersion\":1,\"title\":\"Song\",\"artist\":\"Band\",\"audioAvailability\":\"missing\",\"chartAvailability\":\"unavailable\"}")]
        [TestCase("{\"schemaVersion\":1,\"id\":\"song\",\"artist\":\"Band\",\"audioAvailability\":\"missing\",\"chartAvailability\":\"unavailable\"}")]
        [TestCase("{\"schemaVersion\":1,\"id\":\"song\",\"title\":\"Song\",\"audioAvailability\":\"missing\",\"chartAvailability\":\"unavailable\"}")]
        [TestCase("{\"schemaVersion\":1,\"id\":\"song\",\"title\":\"Song\",\"artist\":\"Band\",\"chartAvailability\":\"unavailable\"}")]
        [TestCase("{\"schemaVersion\":1,\"id\":\"song\",\"title\":\"Song\",\"artist\":\"Band\",\"audioAvailability\":\"missing\"}")]
        [TestCase("{")]
        public void Missing_required_fields_and_malformed_json_are_rejected(string manifest)
        {
            Assert.Throws<SongLibraryLoadException>(() => new SongLibraryDiscovery().Parse(
                manifest,
                root,
                SongLibraryOrigin.Bundled));
        }

        [Test]
        public void Unknown_json_fields_do_not_invent_optional_metadata()
        {
            const string manifest =
                "{\"schemaVersion\":1,\"id\":\"metadata-only\"," +
                "\"title\":\"Metadata Only\",\"artist\":\"Band\"," +
                "\"audioAvailability\":\"missing\"," +
                "\"chartAvailability\":\"unavailable\"," +
                "\"futureMetadata\":\"not part of schema v1\"}";

            SongLibraryEntry song = new SongLibraryDiscovery().Parse(
                manifest,
                root,
                SongLibraryOrigin.Bundled);

            Assert.That(song.Album, Is.Null);
            Assert.That(song.Year, Is.Null);
            Assert.That(song.Bpm, Is.Null);
            Assert.That(song.DifficultyHint, Is.Null);
            Assert.That(song.IsPlayable, Is.False);
        }

        [Test]
        public void Availability_is_not_inferred_from_optional_filename_hints()
        {
            var discovery = new SongLibraryDiscovery();
            const string prefix =
                "{\"schemaVersion\":1,\"id\":\"metadata-only\"," +
                "\"title\":\"Metadata Only\",\"artist\":\"Band\",";

            Assert.Throws<SongLibraryLoadException>(() => discovery.Parse(
                prefix + "\"audioAvailability\":\"missing\",\"audioFile\":\"song.ogg\"," +
                "\"chartAvailability\":\"unavailable\"}", root, SongLibraryOrigin.Bundled));
            Assert.Throws<SongLibraryLoadException>(() => discovery.Parse(
                prefix + "\"audioAvailability\":\"missing\"," +
                "\"chartAvailability\":\"unavailable\",\"chartFile\":\"notes.json\"}",
                root,
                SongLibraryOrigin.Bundled));
        }

        [Test]
        public void Bundled_catalog_is_rights_clean_and_contains_only_project_owned_examples()
        {
            var expected = new Dictionary<string, string[]>
            {
                { "local-song-example", new[] { "Your Library", "Local Song Example", null } }
            };

            string songsRoot = Path.Combine(Application.streamingAssetsPath, "Songs");
            SongLibrarySnapshot snapshot = Discover(songsRoot, SongLibraryOrigin.Bundled);
            SongLibraryEntry[] requested = snapshot.Songs
                .Where(song => expected.ContainsKey(song.Id))
                .ToArray();

            Assert.That(snapshot.Diagnostics, Is.Empty);
            Assert.That(snapshot.Songs, Has.Count.EqualTo(2));
            Assert.That(requested, Has.Length.EqualTo(1));
            Assert.That(requested.Select(song => song.Id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1));
            foreach (SongLibraryEntry song in requested)
            {
                string[] metadata = expected[song.Id];
                Assert.That(song.Artist, Is.EqualTo(metadata[0]), song.Id);
                Assert.That(song.Title, Is.EqualTo(metadata[1]), song.Id);
                Assert.That(song.DifficultyHint, Is.EqualTo(metadata[2]), song.Id);
                Assert.That(song.Album, Is.Null, song.Id);
                Assert.That(song.Genre, Is.Null, song.Id);
                Assert.That(song.Year, Is.Null, song.Id);
                Assert.That(song.AudioAvailability, Is.EqualTo(SongAudioAvailability.Missing), song.Id);
                Assert.That(song.Availability, Is.EqualTo(SongLibraryAvailability.Unavailable), song.Id);
                Assert.That(song.AudioPath, Is.Null, song.Id);
                Assert.That(song.MissingFiles, Is.Empty, song.Id);

                Assert.That(song.Bpm, Is.Null, song.Id);
                Assert.That(song.Bars, Is.Null, song.Id);
                Assert.That(song.BeatsPerBar, Is.Null, song.Id);
                Assert.That(song.ChartAvailability, Is.EqualTo(SongChartAvailability.Unavailable), song.Id);
                Assert.That(song.ChartPath, Is.Null, song.Id);

                string manifest = File.ReadAllText(Path.Combine(song.FolderPath, SongLibraryDiscovery.ManifestFileName));
                Assert.That(manifest, Does.Not.Contain("\"album\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"genre\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"year\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"duration\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"audioVariant\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"audioHash\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"audioFile\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("://"), song.Id);
                Assert.That(manifest, Does.Not.Contain("/Users/"), song.Id);

                Assert.That(manifest, Does.Not.Contain("\"bpm\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"bars\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"beatsPerBar\""), song.Id);
                Assert.That(manifest, Does.Not.Contain("\"chartFile\""), song.Id);

                string[] commercialAssets = Directory.GetFiles(song.FolderPath)
                    .Where(path =>
                    {
                        string extension = Path.GetExtension(path);
                        string fileName = Path.GetFileName(path);
                        return new[] { ".mp3", ".ogg", ".wav", ".flac", ".aac", ".m4a", ".mid", ".midi" }
                                   .Contains(extension, StringComparer.OrdinalIgnoreCase) ||
                               fileName.IndexOf("artwork", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               fileName.IndexOf("cover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               fileName.IndexOf("lyrics", StringComparison.OrdinalIgnoreCase) >= 0;
                    })
                    .ToArray();
                Assert.That(commercialAssets, Is.Empty, song.Id);
            }

            Assert.That(snapshot.Songs.Select(song => song.Id), Does.Contain("neon-circuit"));
            Assert.That(snapshot.Songs.Single(song => song.Id == "neon-circuit").UsesGeneratedBacking, Is.True);
            Assert.That(snapshot.Songs.Select(song => song.Id), Does.Contain("local-song-example"));
            Assert.That(snapshot.Songs.Single(song => song.Id == "local-song-example").IsPlayable, Is.False);
        }

        private SongLibrarySnapshot Discover(string path, SongLibraryOrigin origin) =>
            new SongLibraryDiscovery().Discover(new[] { new SongLibraryRoot(path, origin) });

        private string WriteManifest(string folderName, string json) =>
            WriteManifest(Path.Combine(root, folderName), json, true);

        private static string WriteManifest(string folder, string json, bool createFolder)
        {
            if (createFolder) Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, SongLibraryDiscovery.ManifestFileName), json);
            return folder;
        }

        private static void WriteWaveHeader(string path) => File.WriteAllBytes(path, new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F', 4, 0, 0, 0,
            (byte)'W', (byte)'A', (byte)'V', (byte)'E'
        });

        private static string Manifest(
            string id,
            string title,
            string artist,
            int sortOrder,
            string playback,
            string chart = "",
            string audio = "") =>
            "{" +
            "\"schemaVersion\":1," +
            $"\"id\":\"{id}\"," +
            $"\"title\":\"{title}\"," +
            $"\"artist\":\"{artist}\"," +
            "\"album\":\"Test Album\"," +
            "\"genre\":\"Rock\"," +
            "\"year\":2026," +
            "\"bpm\":120," +
            "\"bars\":8," +
            "\"beatsPerBar\":4," +
            "\"difficultyHint\":\"Easy\"," +
            $"\"sortOrder\":{sortOrder}," +
            $"\"audioAvailability\":\"{(playback == "generated" ? "generated" : "available")}\"," +
            $"\"chartAvailability\":\"{(playback == "generated" ? "generated" : "available")}\"," +
            $"\"chartFile\":\"{chart}\"," +
            $"\"audioFile\":\"{audio}\"" +
            "}";

        private const string ValidChart =
            "{\"version\":1,\"offsetSeconds\":0.0,\"difficulties\":{" +
            "\"easy\":[{\"time\":0.0,\"pad\":\"kick\"}]}}";

        private const string MultiDifficultyChart =
            "{\"version\":1,\"offsetSeconds\":0.0,\"difficulties\":{" +
            "\"easy\":[{\"time\":0.0,\"pad\":\"kick\"}]," +
            "\"hard\":[{\"time\":0.0,\"pad\":\"snare\"}]}}";

        [DllImport("libc", EntryPoint = "symlink", SetLastError = true)]
        private static extern int CreateSymbolicLink(string target, string linkPath);
    }
}
