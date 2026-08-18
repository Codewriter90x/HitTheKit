using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using HitTheKit.Unity.Charts;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public enum SongLibraryOrigin
    {
        Bundled,
        UserFolder
    }

    public enum SongLibraryAvailability
    {
        Ready,
        Unavailable
    }

    public enum SongAudioAvailability
    {
        Generated,
        Missing,
        Available
    }

    public enum SongChartAvailability
    {
        Generated,
        Unavailable,
        Available
    }

    public sealed class SongLibraryEntry
    {
        internal SongLibraryEntry(
            string id,
            string title,
            string artist,
            string album,
            string genre,
            int? year,
            double? bpm,
            int? bars,
            int? beatsPerBar,
            string difficultyHint,
            int sortOrder,
            bool usesGeneratedBacking,
            string folderPath,
            string chartPath,
            string audioPath,
            SongLibraryOrigin origin,
            SongLibraryAvailability availability,
            SongAudioAvailability audioAvailability,
            SongChartAvailability chartAvailability,
            IReadOnlyList<string> availableDifficulties,
            IReadOnlyList<string> missingFiles)
        {
            Id = id;
            Title = title;
            Artist = artist;
            Album = album;
            Genre = genre;
            Year = year;
            Bpm = bpm;
            Bars = bars;
            BeatsPerBar = beatsPerBar;
            DifficultyHint = difficultyHint;
            SortOrder = sortOrder;
            UsesGeneratedBacking = usesGeneratedBacking;
            FolderPath = folderPath;
            ChartPath = chartPath;
            AudioPath = audioPath;
            Origin = origin;
            Availability = availability;
            AudioAvailability = audioAvailability;
            ChartAvailability = chartAvailability;
            AvailableDifficulties = availableDifficulties;
            MissingFiles = missingFiles;
        }

        public string Id { get; }
        public string Title { get; }
        public string Artist { get; }
        public string Album { get; }
        public string Genre { get; }
        public int? Year { get; }
        public double? Bpm { get; }
        public int? Bars { get; }
        public int? BeatsPerBar { get; }
        public string DifficultyHint { get; }
        public int SortOrder { get; }
        public bool UsesGeneratedBacking { get; }
        public string FolderPath { get; }
        public string ChartPath { get; }
        public string AudioPath { get; }
        public SongLibraryOrigin Origin { get; }
        public SongLibraryAvailability Availability { get; }
        public SongAudioAvailability AudioAvailability { get; }
        public SongChartAvailability ChartAvailability { get; }
        public IReadOnlyList<string> AvailableDifficulties { get; }
        public IReadOnlyList<string> MissingFiles { get; }
        public bool IsPlayable => Availability == SongLibraryAvailability.Ready;
    }

    public sealed class SongLibraryRoot
    {
        public SongLibraryRoot(string path, SongLibraryOrigin origin)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Song-library root is required.", nameof(path));
            if (!Enum.IsDefined(typeof(SongLibraryOrigin), origin)) throw new ArgumentOutOfRangeException(nameof(origin));
            Path = System.IO.Path.GetFullPath(path);
            Origin = origin;
        }

        public string Path { get; }
        public SongLibraryOrigin Origin { get; }
    }

    public sealed class SongLibrarySnapshot
    {
        internal SongLibrarySnapshot(IReadOnlyList<SongLibraryEntry> songs, IReadOnlyList<string> diagnostics)
        {
            Songs = songs;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<SongLibraryEntry> Songs { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    public sealed class SongLibraryDiscovery
    {
        public const int SupportedVersion = 1;
        public const string ManifestFileName = "song.json";
        private const long MaximumManifestBytes = 64 * 1024;
        private const long MaximumChartBytes = 4 * 1024 * 1024;

        public SongLibrarySnapshot Discover(IEnumerable<SongLibraryRoot> roots)
        {
            if (roots == null) throw new ArgumentNullException(nameof(roots));
            var songs = new List<SongLibraryEntry>();
            var diagnostics = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (SongLibraryRoot root in roots)
            {
                if (root == null) throw new ArgumentException("Song-library roots cannot contain null entries.", nameof(roots));
                if (!Directory.Exists(root.Path)) continue;

                string[] directories;
                try
                {
                    directories = Directory.GetDirectories(root.Path);
                    Array.Sort(directories, StringComparer.Ordinal);
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Could not enumerate song root: {exception.GetType().Name}.");
                    continue;
                }

                foreach (string directory in directories)
                {
                    string folderName = System.IO.Path.GetFileName(directory);
                    try
                    {
                        if (IsLink(directory))
                        {
                            diagnostics.Add($"Song folder '{folderName}' is a link and was ignored.");
                            continue;
                        }

                        string manifestPath = System.IO.Path.Combine(directory, ManifestFileName);
                        if (!File.Exists(manifestPath)) continue;
                        if (IsLink(manifestPath))
                            throw new SongLibraryLoadException("song.json cannot be a symbolic link.");
                        var info = new FileInfo(manifestPath);
                        if (info.Length <= 0 || info.Length > MaximumManifestBytes)
                            throw new SongLibraryLoadException("song.json must be between 1 byte and 64 KiB.");

                        SongLibraryEntry song = Parse(File.ReadAllText(manifestPath), directory, root.Origin);
                        if (!ids.Add(song.Id))
                        {
                            diagnostics.Add($"Duplicate song ID '{song.Id}' in folder '{folderName}' was ignored.");
                            continue;
                        }
                        songs.Add(song);
                    }
                    catch (SongLibraryLoadException exception)
                    {
                        diagnostics.Add($"Song folder '{folderName}' was ignored: {exception.Message}");
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Add(
                            $"Song folder '{folderName}' could not be read ({exception.GetType().Name}).");
                    }
                }
            }

            songs.Sort((left, right) =>
            {
                int byOrder = left.SortOrder.CompareTo(right.SortOrder);
                if (byOrder != 0) return byOrder;
                int byArtist = string.Compare(left.Artist, right.Artist, StringComparison.OrdinalIgnoreCase);
                return byArtist != 0 ? byArtist : string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
            });
            return new SongLibrarySnapshot(
                new ReadOnlyCollection<SongLibraryEntry>(songs),
                new ReadOnlyCollection<string>(diagnostics));
        }

        public SongLibraryEntry Parse(string json, string songFolder, SongLibraryOrigin origin)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new SongLibraryLoadException("song.json is empty.");
            if (string.IsNullOrWhiteSpace(songFolder)) throw new ArgumentException("Song folder is required.", nameof(songFolder));
            if (!Enum.IsDefined(typeof(SongLibraryOrigin), origin)) throw new ArgumentOutOfRangeException(nameof(origin));

            SongManifestDto dto;
            try
            {
                dto = JsonUtility.FromJson<SongManifestDto>(json);
            }
            catch (ArgumentException exception)
            {
                throw new SongLibraryLoadException("song.json is malformed.", exception);
            }

            if (dto == null) throw new SongLibraryLoadException("song.json did not contain a document.");
            if (dto.schemaVersion != SupportedVersion)
                throw new SongLibraryLoadException($"Unsupported schemaVersion {dto.schemaVersion}; expected {SupportedVersion}.");
            ValidateIdentifier(dto.id);
            ValidateText(dto.title, "title");
            ValidateText(dto.artist, "artist");
            string album = NormalizeOptionalText(dto.album, "album");
            string genre = NormalizeOptionalText(dto.genre, "genre");
            string difficultyHint = NormalizeOptionalText(dto.difficultyHint, "difficultyHint");
            int? year = OptionalPositive(dto.year, "year", 9999);
            double? bpm = OptionalPositive(dto.bpm, "bpm");
            int? bars = OptionalPositive(dto.bars, "bars", int.MaxValue);
            int? beatsPerBar = OptionalPositive(dto.beatsPerBar, "beatsPerBar", int.MaxValue);

            SongAudioAvailability declaredAudio = ParseAudioAvailability(dto.audioAvailability);
            SongChartAvailability declaredChart = ParseChartAvailability(dto.chartAvailability);
            bool generated = declaredAudio == SongAudioAvailability.Generated &&
                             declaredChart == SongChartAvailability.Generated;
            if ((declaredAudio == SongAudioAvailability.Generated) !=
                (declaredChart == SongChartAvailability.Generated))
                throw new SongLibraryLoadException("Generated audio and chart availability must be declared together.");

            string folder = System.IO.Path.GetFullPath(songFolder);
            string chartPath = null;
            string audioPath = null;
            var missing = new List<string>();
            SongAudioAvailability audioAvailability = declaredAudio;
            SongChartAvailability chartAvailability = declaredChart;
            IReadOnlyList<string> availableDifficulties = generated
                ? Array.AsReadOnly(new[] { "easy" })
                : Array.Empty<string>();

            if (generated)
            {
                if (!string.IsNullOrWhiteSpace(dto.chartFile) || !string.IsNullOrWhiteSpace(dto.audioFile))
                    throw new SongLibraryLoadException("Generated songs cannot reference chartFile or audioFile.");
            }
            else
            {
                if (declaredChart == SongChartAvailability.Unavailable && !string.IsNullOrWhiteSpace(dto.chartFile))
                    throw new SongLibraryLoadException("An unavailable chart cannot declare chartFile.");
                if (declaredAudio == SongAudioAvailability.Missing && !string.IsNullOrWhiteSpace(dto.audioFile))
                    throw new SongLibraryLoadException("Missing audio cannot declare audioFile.");
            }

            if (declaredChart == SongChartAvailability.Available)
            {
                chartPath = ResolveFile(folder, dto.chartFile, ".json", "chartFile");
                if (!File.Exists(chartPath))
                {
                    missing.Add(dto.chartFile);
                    chartAvailability = SongChartAvailability.Unavailable;
                }
                else
                {
                    if (IsLink(chartPath)) throw new SongLibraryLoadException("chartFile cannot be a symbolic link.");
                    availableDifficulties = ValidateChart(chartPath);
                }
            }

            if (declaredAudio == SongAudioAvailability.Available)
            {
                audioPath = ResolveAudioFile(folder, dto.audioFile);
                if (!File.Exists(audioPath))
                {
                    missing.Add(dto.audioFile);
                    audioAvailability = SongAudioAvailability.Missing;
                }
                else
                {
                    if (IsLink(audioPath)) throw new SongLibraryLoadException("audioFile cannot be a symbolic link.");
                    ValidateAudioHeader(audioPath);
                }
            }

            bool mediaReady = generated ||
                              audioAvailability == SongAudioAvailability.Available &&
                              chartAvailability == SongChartAvailability.Available;
            if (mediaReady && (!bpm.HasValue || !bars.HasValue || !beatsPerBar.HasValue))
                throw new SongLibraryLoadException(
                    "Playable songs require verified bpm, bars, and beatsPerBar for their exact audio/chart variant.");

            return new SongLibraryEntry(
                dto.id,
                dto.title.Trim(),
                dto.artist.Trim(),
                album,
                genre,
                year,
                bpm,
                bars,
                beatsPerBar,
                difficultyHint,
                dto.sortOrder,
                generated,
                folder,
                chartPath,
                audioPath,
                origin,
                mediaReady ? SongLibraryAvailability.Ready : SongLibraryAvailability.Unavailable,
                audioAvailability,
                chartAvailability,
                availableDifficulties,
                new ReadOnlyCollection<string>(missing));
        }

        private static SongAudioAvailability ParseAudioAvailability(string value)
        {
            if (string.Equals(value, "generated", StringComparison.Ordinal)) return SongAudioAvailability.Generated;
            if (string.Equals(value, "missing", StringComparison.Ordinal)) return SongAudioAvailability.Missing;
            if (string.Equals(value, "available", StringComparison.Ordinal)) return SongAudioAvailability.Available;
            throw new SongLibraryLoadException("audioAvailability must be 'generated', 'missing', or 'available'.");
        }

        private static SongChartAvailability ParseChartAvailability(string value)
        {
            if (string.Equals(value, "generated", StringComparison.Ordinal)) return SongChartAvailability.Generated;
            if (string.Equals(value, "unavailable", StringComparison.Ordinal)) return SongChartAvailability.Unavailable;
            if (string.Equals(value, "available", StringComparison.Ordinal)) return SongChartAvailability.Available;
            throw new SongLibraryLoadException("chartAvailability must be 'generated', 'unavailable', or 'available'.");
        }

        private static string ResolveAudioFile(string folder, string relativePath)
        {
            string extension = System.IO.Path.GetExtension(relativePath ?? string.Empty);
            if (!string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
                throw new SongLibraryLoadException("audioFile must use .ogg or .wav.");
            return ResolveFile(folder, relativePath, extension, "audioFile");
        }

        private static IReadOnlyList<string> ValidateChart(string chartPath)
        {
            var info = new FileInfo(chartPath);
            if (info.Length <= 0 || info.Length > MaximumChartBytes)
                throw new SongLibraryLoadException("chartFile must be between 1 byte and 4 MiB.");
            try
            {
                string json = File.ReadAllText(chartPath);
                var loader = new ChartLoader();
                IReadOnlyList<string> difficulties = loader.GetAvailableDifficulties(json);
                for (int index = 0; index < difficulties.Count; index++)
                    loader.Load(json, difficulties[index]);
                return difficulties;
            }
            catch (Exception exception) when (!(exception is SongLibraryLoadException))
            {
                throw new SongLibraryLoadException("chartFile is not a valid schema-v1 chart.", exception);
            }
        }

        private static void ValidateAudioHeader(string audioPath)
        {
            string extension = System.IO.Path.GetExtension(audioPath);
            byte[] header = new byte[12];
            int read;
            using (FileStream stream = File.OpenRead(audioPath))
            {
                read = stream.Read(header, 0, header.Length);
            }

            bool validOgg = string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase) &&
                            read >= 4 && header[0] == (byte)'O' && header[1] == (byte)'g' &&
                            header[2] == (byte)'g' && header[3] == (byte)'S';
            bool validWave = string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) &&
                             read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' &&
                             header[2] == (byte)'F' && header[3] == (byte)'F' &&
                             header[8] == (byte)'W' && header[9] == (byte)'A' &&
                             header[10] == (byte)'V' && header[11] == (byte)'E';
            if (!validOgg && !validWave)
                throw new SongLibraryLoadException("audioFile does not match its declared OGG/WAVE format.");
        }

        private static string ResolveFile(string folder, string relativePath, string extension, string field)
        {
            ValidateText(relativePath, field);
            if (System.IO.Path.IsPathRooted(relativePath))
                throw new SongLibraryLoadException($"{field} must be relative to its song folder.");
            if (!string.Equals(System.IO.Path.GetExtension(relativePath), extension, StringComparison.OrdinalIgnoreCase))
                throw new SongLibraryLoadException($"{field} has an unsupported extension.");

            string candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, relativePath));
            string boundary = folder.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? folder
                : folder + System.IO.Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(boundary, StringComparison.Ordinal))
                throw new SongLibraryLoadException($"{field} escapes its song folder.");
            RejectLinkedPathComponents(folder, candidate, field);
            return candidate;
        }

        private static void RejectLinkedPathComponents(string folder, string candidate, string field)
        {
            string relativePath = candidate.Substring(folder.Length)
                .TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string[] components = relativePath.Split(
                new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            string current = folder;
            foreach (string component in components)
            {
                current = System.IO.Path.Combine(current, component);
                if ((File.Exists(current) || Directory.Exists(current)) && IsLink(current))
                    throw new SongLibraryLoadException($"{field} cannot traverse a symbolic link.");
            }
        }

        private static bool IsLink(string path) =>
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

        private static void ValidateIdentifier(string value)
        {
            ValidateText(value, "id");
            if (value.Length > 80) throw new SongLibraryLoadException("id is too long.");
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool valid = character >= 'a' && character <= 'z' ||
                             character >= '0' && character <= '9' ||
                             character == '-';
                if (!valid) throw new SongLibraryLoadException("id must use lowercase letters, numbers, and hyphens.");
            }
        }

        private static void ValidateText(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new SongLibraryLoadException($"{field} is required.");
            if (value.Length > 200) throw new SongLibraryLoadException($"{field} is too long.");
        }

        private static string NormalizeOptionalText(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (value.Length > 200) throw new SongLibraryLoadException($"{field} is too long.");
            return value.Trim();
        }

        private static int? OptionalPositive(int value, string field, int maximum)
        {
            if (value == 0) return null;
            if (value < 0 || value > maximum)
                throw new SongLibraryLoadException($"{field} must be positive when supplied.");
            return value;
        }

        private static double? OptionalPositive(double value, string field)
        {
            if (!IsFinite(value)) throw new SongLibraryLoadException($"{field} must be finite when supplied.");
            if (value == 0) return null;
            if (value < 0) throw new SongLibraryLoadException($"{field} must be positive when supplied.");
            return value;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        [Serializable]
        private sealed class SongManifestDto
        {
            public int schemaVersion;
            public string id;
            public string title;
            public string artist;
            public string album;
            public string genre;
            public int year;
            public double bpm;
            public int bars;
            public int beatsPerBar;
            public string difficultyHint;
            public int sortOrder;
            public string audioAvailability;
            public string chartAvailability;
            public string chartFile;
            public string audioFile;
        }
    }

    public sealed class SongLibraryLoadException : Exception
    {
        public SongLibraryLoadException(string message) : base(message) { }
        public SongLibraryLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public static class SongLibraryRuntime
    {
        public const string DocumentsFolderName = "HTKSongs";

        public static string BundledRoot => System.IO.Path.Combine(Application.streamingAssetsPath, "Songs");
        public static string UserRoot => UserProfileRoot(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        public static string LegacyUserRoot => System.IO.Path.Combine(Application.persistentDataPath, "Songs");

        public static string UserProfileRoot(string userProfileDirectory)
        {
            if (string.IsNullOrWhiteSpace(userProfileDirectory))
                throw new ArgumentException("User profile directory is required.", nameof(userProfileDirectory));
            return DocumentsRoot(System.IO.Path.Combine(userProfileDirectory, "Documents"));
        }

        public static string DocumentsRoot(string documentsDirectory)
        {
            if (string.IsNullOrWhiteSpace(documentsDirectory))
                throw new ArgumentException("Documents directory is required.", nameof(documentsDirectory));
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(documentsDirectory, DocumentsFolderName));
        }

        public static SongLibrarySnapshot Discover() => Discover(UserRoot, LegacyUserRoot, BundledRoot);

        public static SongLibrarySnapshot Discover(
            string userRoot,
            string legacyUserRoot,
            string bundledRoot)
        {
            if (string.IsNullOrWhiteSpace(userRoot))
                throw new ArgumentException("Documents song root is required.", nameof(userRoot));
            if (string.IsNullOrWhiteSpace(legacyUserRoot))
                throw new ArgumentException("Legacy song root is required.", nameof(legacyUserRoot));
            if (string.IsNullOrWhiteSpace(bundledRoot))
                throw new ArgumentException("Bundled song root is required.", nameof(bundledRoot));

            string documentsRoot = System.IO.Path.GetFullPath(userRoot);
            var runtimeDiagnostics = new List<string>();
            try
            {
                Directory.CreateDirectory(documentsRoot);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is NotSupportedException)
            {
                runtimeDiagnostics.Add(
                    $"Could not create the Documents song folder ({exception.GetType().Name}); " +
                    "the legacy and bundled libraries remain available.");
            }

            var roots = new List<SongLibraryRoot>();
            var uniqueRoots = new HashSet<string>(StringComparer.Ordinal);
            AddRoot(roots, uniqueRoots, documentsRoot, SongLibraryOrigin.UserFolder);
            AddRoot(roots, uniqueRoots, legacyUserRoot, SongLibraryOrigin.UserFolder);
            AddRoot(roots, uniqueRoots, bundledRoot, SongLibraryOrigin.Bundled);

            SongLibrarySnapshot discovered = new SongLibraryDiscovery().Discover(roots);
            if (runtimeDiagnostics.Count == 0) return discovered;

            runtimeDiagnostics.AddRange(discovered.Diagnostics);
            return new SongLibrarySnapshot(
                discovered.Songs,
                new ReadOnlyCollection<string>(runtimeDiagnostics));
        }

        private static void AddRoot(
            ICollection<SongLibraryRoot> roots,
            ISet<string> uniqueRoots,
            string path,
            SongLibraryOrigin origin)
        {
            var root = new SongLibraryRoot(path, origin);
            if (uniqueRoots.Add(root.Path)) roots.Add(root);
        }
    }
}
