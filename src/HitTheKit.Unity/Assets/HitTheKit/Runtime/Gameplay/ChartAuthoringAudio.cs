using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class ChartAuthoringAudioRequest
    {
        public ChartAuthoringAudioRequest(
            string sourceAudioPath,
            string title,
            string artist,
            double bpm,
            int bars,
            int beatsPerBar)
        {
            if (string.IsNullOrWhiteSpace(sourceAudioPath))
                throw new ArgumentException("An audio file is required.", nameof(sourceAudioPath));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(artist)) throw new ArgumentException("Artist is required.", nameof(artist));
            if (!IsFinite(bpm) || bpm <= 0 || bpm > 400) throw new ArgumentOutOfRangeException(nameof(bpm));
            if (bars <= 0 || bars > 10000) throw new ArgumentOutOfRangeException(nameof(bars));
            if (beatsPerBar <= 0 || beatsPerBar > 32) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));

            SourceAudioPath = Path.GetFullPath(sourceAudioPath);
            Title = title.Trim();
            Artist = artist.Trim();
            Bpm = bpm;
            Bars = bars;
            BeatsPerBar = beatsPerBar;
        }

        public string SourceAudioPath { get; }
        public string Title { get; }
        public string Artist { get; }
        public double Bpm { get; }
        public int Bars { get; }
        public int BeatsPerBar { get; }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class ChartAuthoringAudioImportResult
    {
        internal ChartAuthoringAudioImportResult(SongLibraryEntry song, string sourcePath)
        {
            Song = song;
            SourcePath = sourcePath;
        }

        public SongLibraryEntry Song { get; }
        public string SourcePath { get; }
    }

    public sealed class ChartAuthoringAudioImportException : Exception
    {
        public ChartAuthoringAudioImportException(string message) : base(message) { }
        public ChartAuthoringAudioImportException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class ChartAuthoringAudioImporter
    {
        public const long MaximumAudioBytes = 1024L * 1024 * 1024;
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public ChartAuthoringAudioImportResult Import(
            ChartAuthoringAudioRequest request,
            string libraryRoot,
            DateTimeOffset createdAtUtc)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(libraryRoot))
                throw new ArgumentException("Song-library root is required.", nameof(libraryRoot));
            if (createdAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("Creation time must be UTC.", nameof(createdAtUtc));

            string source = ValidateSource(request.SourceAudioPath);
            string root = Path.GetFullPath(libraryRoot);
            Directory.CreateDirectory(root);
            string baseId = BuildId(request.Artist, request.Title, createdAtUtc);
            string songId = UniqueSongId(root, baseId);
            string destination = Path.Combine(root, songId);
            string temporary = Path.Combine(root, $".hitthekit-audio-{Guid.NewGuid():N}");
            string audioFileName = "source-audio" + Path.GetExtension(source).ToLowerInvariant();
            Directory.CreateDirectory(temporary);

            try
            {
                string copiedAudio = Path.Combine(temporary, audioFileName);
                File.Copy(source, copiedAudio, false);
                string manifest = Manifest(songId, request, audioFileName);
                File.WriteAllText(Path.Combine(temporary, SongLibraryDiscovery.ManifestFileName), manifest, Utf8WithoutBom);
                SongLibraryEntry validated = new SongLibraryDiscovery().Parse(
                    manifest,
                    temporary,
                    SongLibraryOrigin.UserFolder);
                if (!validated.CanAuthorChart || validated.IsPlayable)
                    throw new ChartAuthoringAudioImportException("The imported audio did not produce a valid authoring source.");

                Directory.Move(temporary, destination);
                SongLibraryEntry published = new SongLibraryDiscovery().Parse(
                    File.ReadAllText(Path.Combine(destination, SongLibraryDiscovery.ManifestFileName)),
                    destination,
                    SongLibraryOrigin.UserFolder);
                return new ChartAuthoringAudioImportResult(published, source);
            }
            catch (ChartAuthoringAudioImportException)
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                throw;
            }
            catch (Exception exception)
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                throw new ChartAuthoringAudioImportException("The selected audio could not be imported.", exception);
            }
        }

        public static string ValidateSource(string sourceAudioPath)
        {
            if (string.IsNullOrWhiteSpace(sourceAudioPath))
                throw new ChartAuthoringAudioImportException("Select a WAV or OGG audio file.");
            string source = Path.GetFullPath(sourceAudioPath);
            var info = new FileInfo(source);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumAudioBytes)
                throw new ChartAuthoringAudioImportException("Audio must be between 1 byte and 1 GiB.");
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                throw new ChartAuthoringAudioImportException("The selected audio cannot be a symbolic link.");
            string extension = Path.GetExtension(source);
            if (!string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
                throw new ChartAuthoringAudioImportException("Chart Creator supports WAV and OGG audio.");
            return source;
        }

        private static string UniqueSongId(string root, string baseId)
        {
            string candidate = baseId;
            int suffix = 2;
            while (Directory.Exists(Path.Combine(root, candidate)) ||
                   File.Exists(Path.Combine(root, candidate + HtkSongPackageService.Extension)))
                candidate = $"{baseId}-{suffix++}";
            ChartCreatorMetadata.ValidateIdentifier(candidate, nameof(baseId));
            return candidate;
        }

        private static string BuildId(string artist, string title, DateTimeOffset createdAtUtc)
        {
            string text = (artist + "-" + title).Normalize(NormalizationForm.FormD);
            var id = new StringBuilder(64);
            bool separator = false;
            for (int index = 0; index < text.Length && id.Length < 48; index++)
            {
                char value = char.ToLowerInvariant(text[index]);
                if (value >= 'a' && value <= 'z' || value >= '0' && value <= '9')
                {
                    id.Append(value);
                    separator = false;
                }
                else if (id.Length > 0 && !separator)
                {
                    id.Append('-');
                    separator = true;
                }
            }
            string prefix = id.ToString().Trim('-');
            if (prefix.Length == 0) prefix = "local-song";
            return $"{prefix}-authoring-{createdAtUtc:yyyyMMdd-HHmmss}";
        }

        private static string Manifest(
            string songId,
            ChartAuthoringAudioRequest request,
            string audioFileName)
        {
            return "{\n" +
                   "  \"schemaVersion\": 1,\n" +
                   $"  \"id\": \"{Escape(songId)}\",\n" +
                   $"  \"title\": \"{Escape(request.Title)}\",\n" +
                   $"  \"artist\": \"{Escape(request.Artist)}\",\n" +
                   $"  \"bpm\": {request.Bpm.ToString("0.#########", CultureInfo.InvariantCulture)},\n" +
                   $"  \"bars\": {request.Bars},\n" +
                   $"  \"beatsPerBar\": {request.BeatsPerBar},\n" +
                   "  \"difficultyHint\": \"New local chart\",\n" +
                   "  \"sortOrder\": 0,\n" +
                   "  \"audioAvailability\": \"available\",\n" +
                   $"  \"audioFile\": \"{Escape(audioFileName)}\",\n" +
                   "  \"chartAvailability\": \"unavailable\"\n" +
                   "}\n";
        }

        private static string Escape(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    public interface IChartAuthoringAudioPicker
    {
        string PickAudioFile();
    }

    public sealed class MacOsChartAuthoringAudioPicker : IChartAuthoringAudioPicker
    {
        public string PickAudioFile()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            const string script =
                "const app = Application.currentApplication(); " +
                "app.includeStandardAdditions = true; " +
                "const chosen = app.chooseFile({withPrompt: 'Choose a WAV or OGG backing track'}); " +
                "Path(chosen).toString();";
            var start = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = "-l JavaScript -e \"" +
                            script.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("The macOS audio picker could not start.");
                string output = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 ? output.Trim() : null;
            }
#else
            throw new PlatformNotSupportedException("The native audio picker currently supports macOS.");
#endif
        }
    }
}
