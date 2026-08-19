using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Gameplay
{
    public enum ChartQuantization
    {
        None,
        EighthNote,
        SixteenthNote
    }

    public sealed class RecordedChartHit
    {
        internal RecordedChartHit(DrumInputEvent input, int recordingIndex)
        {
            Pad = input.Pad;
            Velocity = input.Velocity;
            TimeSeconds = input.SongTimeSeconds;
            Source = input.Source;
            RecordingIndex = recordingIndex;
        }

        public DrumPad Pad { get; }
        public int Velocity { get; }
        public double TimeSeconds { get; }
        public DrumInputSource Source { get; }
        internal int RecordingIndex { get; }
    }

    public sealed class ChartRecordingDraft
    {
        internal ChartRecordingDraft(double durationSeconds, IReadOnlyList<RecordedChartHit> hits)
        {
            DurationSeconds = durationSeconds;
            var copy = new RecordedChartHit[hits.Count];
            for (int index = 0; index < hits.Count; index++) copy[index] = hits[index];
            Hits = Array.AsReadOnly(copy);
        }

        public double DurationSeconds { get; }
        public IReadOnlyList<RecordedChartHit> Hits { get; }
    }

    public sealed class ChartRecordingSession
    {
        public const int MaximumHits = 100000;
        private readonly double durationSeconds;
        private readonly double outputTimeScale;
        private readonly List<RecordedChartHit> hits = new List<RecordedChartHit>();

        public ChartRecordingSession(double durationSeconds, double outputTimeScale = 1.0)
        {
            if (!IsFinite(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (!IsFinite(outputTimeScale) || outputTimeScale <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputTimeScale));
            this.durationSeconds = durationSeconds;
            this.outputTimeScale = outputTimeScale;
        }

        public bool IsRecording { get; private set; } = true;
        public int HitCount => hits.Count;
        public int IgnoredCount { get; private set; }

        public bool Record(DrumInputEvent input)
        {
            if (!IsRecording) return false;
            if (input.SongTimeSeconds < 0 || input.SongTimeSeconds > durationSeconds)
            {
                IgnoredCount++;
                return false;
            }
            if (hits.Count >= MaximumHits)
                throw new InvalidOperationException($"A chart recording cannot exceed {MaximumHits} hits.");

            hits.Add(new RecordedChartHit(input.WithSongTime(input.SongTimeSeconds * outputTimeScale), hits.Count));
            return true;
        }

        public ChartRecordingDraft Finish()
        {
            IsRecording = false;
            return new ChartRecordingDraft(durationSeconds * outputTimeScale, hits);
        }

        public void Restart()
        {
            hits.Clear();
            IgnoredCount = 0;
            IsRecording = true;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class ChartCreatorMetadata
    {
        public ChartCreatorMetadata(
            string sourceSongId,
            string title,
            string artist,
            string difficulty,
            double bpm,
            int bars,
            int beatsPerBar)
        {
            ValidateIdentifier(sourceSongId, nameof(sourceSongId));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(artist)) throw new ArgumentException("Artist is required.", nameof(artist));
            if (!Contains(ChartLoader.SupportedDifficulties, difficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            if (!IsFinite(bpm) || bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));
            if (bars <= 0) throw new ArgumentOutOfRangeException(nameof(bars));
            if (beatsPerBar <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));

            SourceSongId = sourceSongId;
            Title = title.Trim();
            Artist = artist.Trim();
            Difficulty = difficulty;
            Bpm = bpm;
            Bars = bars;
            BeatsPerBar = beatsPerBar;
        }

        public string SourceSongId { get; }
        public string Title { get; }
        public string Artist { get; }
        public string Difficulty { get; }
        public double Bpm { get; }
        public int Bars { get; }
        public int BeatsPerBar { get; }

        internal static void ValidateIdentifier(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
                throw new ArgumentException("Song ID is required and must not exceed 80 characters.", parameter);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') && character != '-')
                    throw new ArgumentException("Song ID must use lowercase letters, numbers, and hyphens.", parameter);
            }
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int index = 0; index < values.Count; index++)
                if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public static class ChartCreatorJson
    {
        public static string Serialize(
            ChartRecordingDraft draft,
            string difficulty,
            double bpm,
            ChartQuantization quantization)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (!Enum.IsDefined(typeof(ChartQuantization), quantization))
                throw new ArgumentOutOfRangeException(nameof(quantization));
            if (!IsFinite(bpm) || bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));
            bool supported = false;
            for (int index = 0; index < ChartLoader.SupportedDifficulties.Count; index++)
                if (string.Equals(ChartLoader.SupportedDifficulties[index], difficulty, StringComparison.Ordinal))
                    supported = true;
            if (!supported) throw new ArgumentOutOfRangeException(nameof(difficulty));

            var projected = new List<ProjectedHit>(draft.Hits.Count);
            for (int index = 0; index < draft.Hits.Count; index++)
            {
                RecordedChartHit hit = draft.Hits[index];
                double time = Quantize(hit.TimeSeconds, bpm, quantization);
                time = Math.Max(0, Math.Min(draft.DurationSeconds, time));
                projected.Add(new ProjectedHit(time, hit.Pad, hit.RecordingIndex));
            }
            projected.Sort((left, right) =>
            {
                int byTime = left.TimeSeconds.CompareTo(right.TimeSeconds);
                return byTime != 0 ? byTime : left.RecordingIndex.CompareTo(right.RecordingIndex);
            });

            var json = new StringBuilder(128 + projected.Count * 48);
            json.Append("{\n  \"version\": 1,\n  \"offsetSeconds\": 0,\n  \"difficulties\": {\n    \"")
                .Append(difficulty)
                .Append("\": [");
            for (int index = 0; index < projected.Count; index++)
            {
                ProjectedHit hit = projected[index];
                json.Append(index == 0 ? "\n      " : ",\n      ")
                    .Append("{ \"time\": ")
                    .Append(hit.TimeSeconds.ToString("0.#########", CultureInfo.InvariantCulture))
                    .Append(", \"pad\": \"")
                    .Append(PadId(hit.Pad))
                    .Append("\" }");
            }
            if (projected.Count > 0) json.Append('\n').Append("    ");
            json.Append("]\n  }\n}\n");

            string result = json.ToString();
            new ChartLoader().Load(result, difficulty);
            return result;
        }

        private static double Quantize(double value, double bpm, ChartQuantization quantization)
        {
            int subdivisions;
            switch (quantization)
            {
                case ChartQuantization.None: return value;
                case ChartQuantization.EighthNote: subdivisions = 2; break;
                case ChartQuantization.SixteenthNote: subdivisions = 4; break;
                default: throw new ArgumentOutOfRangeException(nameof(quantization));
            }
            double step = 60.0 / bpm / subdivisions;
            return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
        }

        private static string PadId(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return "kick";
                case DrumPad.Snare: return "snare";
                case DrumPad.HiHat: return "hiHat";
                case DrumPad.Tom1: return "tom1";
                case DrumPad.Tom2: return "tom2";
                case DrumPad.FloorTom: return "floorTom";
                case DrumPad.Crash: return "crash";
                case DrumPad.Ride: return "ride";
                default: throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private sealed class ProjectedHit
        {
            public ProjectedHit(double timeSeconds, DrumPad pad, int recordingIndex)
            {
                TimeSeconds = timeSeconds;
                Pad = pad;
                RecordingIndex = recordingIndex;
            }
            public double TimeSeconds { get; }
            public DrumPad Pad { get; }
            public int RecordingIndex { get; }
        }
    }

    public sealed class ChartCreatorExportResult
    {
        internal ChartCreatorExportResult(
            string songId,
            string folderPath,
            string chartPath,
            string manifestPath,
            string packagePath)
        {
            SongId = songId;
            FolderPath = folderPath;
            ChartPath = chartPath;
            ManifestPath = manifestPath;
            PackagePath = packagePath;
        }
        public string SongId { get; }
        public string FolderPath { get; }
        public string ChartPath { get; }
        public string ManifestPath { get; }
        public string PackagePath { get; }
    }

    public sealed class ChartCreatorExporter
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public ChartCreatorExportResult ExportChartOnly(
            ChartRecordingDraft draft,
            ChartCreatorMetadata metadata,
            ChartQuantization quantization,
            string libraryRoot,
            DateTimeOffset createdAtUtc)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (draft.Hits.Count == 0)
                throw new InvalidOperationException("A recorded chart must contain at least one hit.");
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (string.IsNullOrWhiteSpace(libraryRoot)) throw new ArgumentException("Library root is required.", nameof(libraryRoot));
            if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Creation time must be UTC.", nameof(createdAtUtc));

            string root = Path.GetFullPath(libraryRoot);
            Directory.CreateDirectory(root);
            string prefix = metadata.SourceSongId.Length <= 48 ? metadata.SourceSongId : metadata.SourceSongId.Substring(0, 48).TrimEnd('-');
            string baseId = $"{prefix}-take-{createdAtUtc:yyyyMMdd-HHmmss}";
            string songId = UniqueSongId(root, baseId);
            string destination = Path.Combine(root, songId);
            string packagePath = Path.Combine(root, songId + HtkSongPackageService.Extension);
            string temporary = Path.Combine(root, $".hitthekit-chart-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporary);
            bool destinationPublished = false;
            try
            {
                string chartJson = ChartCreatorJson.Serialize(draft, metadata.Difficulty, metadata.Bpm, quantization);
                string chartPath = Path.Combine(temporary, "notes.json");
                string manifestPath = Path.Combine(temporary, "song.json");
                File.WriteAllText(chartPath, chartJson, Utf8WithoutBom);
                File.WriteAllText(manifestPath, Manifest(songId, metadata), Utf8WithoutBom);

                // Validate both public formats before the atomic publish into HTKSongs.
                new ChartLoader().Load(chartJson, metadata.Difficulty);
                new SongLibraryDiscovery().Parse(File.ReadAllText(manifestPath), temporary, SongLibraryOrigin.UserFolder);
                Directory.Move(temporary, destination);
                destinationPublished = true;
                new HtkSongPackageService().CreateChartOnlyPackage(destination, packagePath);
                return new ChartCreatorExportResult(
                    songId,
                    destination,
                    Path.Combine(destination, "notes.json"),
                    Path.Combine(destination, "song.json"),
                    packagePath);
            }
            catch
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                if (destinationPublished && Directory.Exists(destination)) Directory.Delete(destination, true);
                throw;
            }
        }

        private static string UniqueSongId(string root, string baseId)
        {
            string value = baseId;
            int suffix = 2;
            while (Directory.Exists(Path.Combine(root, value)) ||
                   File.Exists(Path.Combine(root, value + HtkSongPackageService.Extension)))
                value = $"{baseId}-{suffix++}";
            ChartCreatorMetadata.ValidateIdentifier(value, nameof(baseId));
            return value;
        }

        private static string Manifest(string songId, ChartCreatorMetadata metadata)
        {
            return "{\n" +
                   "  \"schemaVersion\": 1,\n" +
                   $"  \"id\": \"{Escape(songId)}\",\n" +
                   $"  \"title\": \"{Escape(metadata.Title + " · Recorded Take")}\",\n" +
                   $"  \"artist\": \"{Escape(metadata.Artist)}\",\n" +
                   $"  \"bpm\": {metadata.Bpm.ToString("0.#########", CultureInfo.InvariantCulture)},\n" +
                   $"  \"bars\": {metadata.Bars},\n" +
                   $"  \"beatsPerBar\": {metadata.BeatsPerBar},\n" +
                   "  \"difficultyHint\": \"Recorded performance · review before sharing\",\n" +
                   "  \"sortOrder\": 0,\n" +
                   "  \"audioAvailability\": \"missing\",\n" +
                   "  \"chartAvailability\": \"available\",\n" +
                   "  \"chartFile\": \"notes.json\"\n" +
                   "}\n";
        }

        private static string Escape(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
