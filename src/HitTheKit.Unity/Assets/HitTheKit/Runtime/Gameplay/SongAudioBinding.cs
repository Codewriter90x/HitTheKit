using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class SongAudioBindingException : Exception
    {
        public SongAudioBindingException(string message) : base(message) { }
        public SongAudioBindingException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class SongAudioBindingResult
    {
        internal SongAudioBindingResult(SongLibraryEntry song, string sourcePath)
        {
            Song = song;
            SourcePath = sourcePath;
        }

        public SongLibraryEntry Song { get; }
        public string SourcePath { get; }
    }

    public sealed class SongAudioBindingService
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public SongAudioBindingResult Bind(
            SongLibraryEntry song,
            string sourceAudioPath,
            string libraryRoot)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            if (!song.CanBindAudio)
                throw new SongAudioBindingException("The selected song is not an imported chart awaiting local audio.");
            if (string.IsNullOrWhiteSpace(libraryRoot))
                throw new ArgumentException("Song-library root is required.", nameof(libraryRoot));

            string source = ChartAuthoringAudioImporter.ValidateSource(sourceAudioPath);
            string root = Path.GetFullPath(libraryRoot);
            string folder = Path.GetFullPath(song.FolderPath);
            ValidateSongBoundary(song, root, folder);

            string manifestPath = Path.Combine(folder, SongLibraryDiscovery.ManifestFileName);
            if (!File.Exists(manifestPath) || IsLink(manifestPath))
                throw new SongAudioBindingException("The imported song manifest is missing or linked.");
            if (!File.Exists(song.ChartPath) || IsLink(song.ChartPath))
                throw new SongAudioBindingException("The imported chart is missing or linked.");

            string extension = Path.GetExtension(source).ToLowerInvariant();
            string audioName = "source-audio" + extension;
            string destinationAudio = Path.Combine(folder, audioName);
            if (File.Exists(destinationAudio))
                throw new SongAudioBindingException("This song folder already contains a local audio binding.");

            string token = Guid.NewGuid().ToString("N");
            string temporaryAudioName = $".hitthekit-bind-{token}{extension}";
            string temporaryAudio = Path.Combine(folder, temporaryAudioName);
            string temporaryManifest = Path.Combine(folder, $".hitthekit-bind-{token}.json");
            string backupManifest = Path.Combine(folder, $".hitthekit-bind-{token}.backup");
            bool audioPublished = false;
            try
            {
                File.Copy(source, temporaryAudio, false);
                string validationManifest = Manifest(song, temporaryAudioName);
                SongLibraryEntry validation = new SongLibraryDiscovery().Parse(
                    validationManifest,
                    folder,
                    SongLibraryOrigin.UserFolder);
                if (!validation.IsPlayable)
                    throw new SongAudioBindingException("The selected audio does not complete this chart.");

                File.Move(temporaryAudio, destinationAudio);
                audioPublished = true;
                string finalManifest = Manifest(song, audioName);
                SongLibraryEntry published = new SongLibraryDiscovery().Parse(
                    finalManifest,
                    folder,
                    SongLibraryOrigin.UserFolder);
                if (!published.IsPlayable)
                    throw new SongAudioBindingException("The local audio binding did not produce a playable song.");

                File.WriteAllText(temporaryManifest, finalManifest, Utf8WithoutBom);
                File.Replace(temporaryManifest, manifestPath, backupManifest);
                TryDelete(backupManifest);
                return new SongAudioBindingResult(published, source);
            }
            catch (SongAudioBindingException)
            {
                Cleanup(temporaryAudio, temporaryManifest, backupManifest, destinationAudio, audioPublished);
                throw;
            }
            catch (Exception exception)
            {
                Cleanup(temporaryAudio, temporaryManifest, backupManifest, destinationAudio, audioPublished);
                throw new SongAudioBindingException("The local audio binding could not be completed.", exception);
            }
        }

        private static void ValidateSongBoundary(SongLibraryEntry song, string root, string folder)
        {
            if (!Directory.Exists(root) || !Directory.Exists(folder) || IsLink(folder))
                throw new SongAudioBindingException("The imported song folder is unavailable or linked.");
            DirectoryInfo parent = Directory.GetParent(folder);
            if (parent == null || !string.Equals(Path.GetFullPath(parent.FullName), root, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFileName(folder), song.Id, StringComparison.Ordinal))
                throw new SongAudioBindingException("The song folder is outside the configured user library.");
        }

        private static string Manifest(SongLibraryEntry song, string audioFileName)
        {
            var fields = new List<string>
            {
                "  \"schemaVersion\": 1",
                $"  \"id\": \"{Escape(song.Id)}\"",
                $"  \"title\": \"{Escape(song.Title)}\"",
                $"  \"artist\": \"{Escape(song.Artist)}\""
            };
            AddOptional(fields, "album", song.Album);
            AddOptional(fields, "genre", song.Genre);
            if (song.Year.HasValue) fields.Add($"  \"year\": {song.Year.Value}");
            if (song.Bpm.HasValue)
                fields.Add($"  \"bpm\": {song.Bpm.Value.ToString("0.#########", CultureInfo.InvariantCulture)}");
            if (song.Bars.HasValue) fields.Add($"  \"bars\": {song.Bars.Value}");
            if (song.BeatsPerBar.HasValue) fields.Add($"  \"beatsPerBar\": {song.BeatsPerBar.Value}");
            AddOptional(fields, "difficultyHint", song.DifficultyHint);
            fields.Add($"  \"sortOrder\": {song.SortOrder}");
            fields.Add("  \"audioAvailability\": \"available\"");
            fields.Add($"  \"audioFile\": \"{Escape(audioFileName)}\"");
            fields.Add("  \"chartAvailability\": \"available\"");
            fields.Add($"  \"chartFile\": \"{Escape(Path.GetFileName(song.ChartPath))}\"");
            return "{\n" + string.Join(",\n", fields) + "\n}\n";
        }

        private static void AddOptional(ICollection<string> fields, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) fields.Add($"  \"{name}\": \"{Escape(value)}\"");
        }

        private static string Escape(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        private static bool IsLink(string path) =>
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

        private static void Cleanup(
            string temporaryAudio,
            string temporaryManifest,
            string backupManifest,
            string destinationAudio,
            bool audioPublished)
        {
            TryDelete(temporaryAudio);
            TryDelete(temporaryManifest);
            TryDelete(backupManifest);
            if (audioPublished) TryDelete(destinationAudio);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* Cleanup must not hide the structured binding failure. */ }
        }
    }
}
