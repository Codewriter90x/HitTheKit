using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HitTheKit.Unity.Gameplay
{
    public enum HtkSongImportStatus
    {
        Imported,
        AlreadyInstalled
    }

    public sealed class HtkSongImportResult
    {
        internal HtkSongImportResult(string songId, string folderPath, HtkSongImportStatus status)
        {
            SongId = songId;
            FolderPath = folderPath;
            Status = status;
        }

        public string SongId { get; }
        public string FolderPath { get; }
        public HtkSongImportStatus Status { get; }
    }

    public sealed class HtkSongInboxResult
    {
        internal HtkSongInboxResult(IReadOnlyList<string> importedSongIds, IReadOnlyList<string> diagnostics)
        {
            ImportedSongIds = importedSongIds;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<string> ImportedSongIds { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    public sealed class HtkSongPackageException : Exception
    {
        public HtkSongPackageException(string message) : base(message) { }
        public HtkSongPackageException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class HtkSongPackageService
    {
        public const int PackageVersion = 1;
        public const string Extension = ".htksong";
        public const string VersionEntryName = "htksong-version";
        public const string ManifestEntryName = "song.json";
        public const string ChartEntryName = "notes.json";
        public const long MaximumPackageBytes = 5L * 1024 * 1024;
        public const long MaximumManifestBytes = 64L * 1024;
        public const long MaximumChartBytes = 4L * 1024 * 1024;

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);
        private static readonly DateTimeOffset StableEntryTimestamp =
            new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public string CreateChartOnlyPackage(string songFolder, string packagePath)
        {
            if (string.IsNullOrWhiteSpace(songFolder))
                throw new ArgumentException("Song folder is required.", nameof(songFolder));
            ValidatePackagePath(packagePath);

            string folder = Path.GetFullPath(songFolder);
            if (!Directory.Exists(folder)) throw new HtkSongPackageException("The song folder does not exist.");
            if (IsLink(folder)) throw new HtkSongPackageException("The song folder cannot be a symbolic link.");

            string manifestPath = Path.Combine(folder, ManifestEntryName);
            string chartPath = Path.Combine(folder, ChartEntryName);
            ValidateSourceFile(manifestPath, MaximumManifestBytes, ManifestEntryName);
            ValidateSourceFile(chartPath, MaximumChartBytes, ChartEntryName);
            SongLibraryEntry song = ValidateChartOnlySong(folder, File.ReadAllText(manifestPath));
            string expectedPackageName = song.Id + Extension;
            if (!string.Equals(Path.GetFileName(packagePath), expectedPackageName, StringComparison.Ordinal))
                throw new HtkSongPackageException($"Package filename must be '{expectedPackageName}'.");

            string destination = Path.GetFullPath(packagePath);
            string parent = Path.GetDirectoryName(destination);
            if (string.IsNullOrEmpty(parent)) throw new HtkSongPackageException("Package destination is invalid.");
            Directory.CreateDirectory(parent);
            if (File.Exists(destination)) throw new HtkSongPackageException("A package with this song ID already exists.");

            string temporary = Path.Combine(parent, $".hitthekit-package-{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Utf8WithoutBom))
                {
                    WriteEntry(archive, VersionEntryName, Encoding.ASCII.GetBytes(PackageVersion + "\n"));
                    WriteEntry(archive, ManifestEntryName, File.ReadAllBytes(manifestPath));
                    WriteEntry(archive, ChartEntryName, File.ReadAllBytes(chartPath));
                }

                ValidatePackageFile(temporary);
                File.Move(temporary, destination);
                return destination;
            }
            catch (HtkSongPackageException)
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                throw;
            }
            catch (Exception exception)
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                throw new HtkSongPackageException("The chart package could not be created.", exception);
            }
        }

        public HtkSongImportResult ImportChartOnlyPackage(string packagePath, string libraryRoot)
        {
            ValidatePackagePath(packagePath);
            if (string.IsNullOrWhiteSpace(libraryRoot))
                throw new ArgumentException("Song-library root is required.", nameof(libraryRoot));

            string package = Path.GetFullPath(packagePath);
            string root = Path.GetFullPath(libraryRoot);
            if (!File.Exists(package)) throw new HtkSongPackageException("The .htksong package does not exist.");
            if (IsLink(package)) throw new HtkSongPackageException("A .htksong package cannot be a symbolic link.");
            Directory.CreateDirectory(root);

            PackagePayload payload = ReadPackage(package);
            string temporary = Path.Combine(root, $".hitthekit-import-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporary);
            try
            {
                File.WriteAllBytes(Path.Combine(temporary, ManifestEntryName), payload.Manifest);
                File.WriteAllBytes(Path.Combine(temporary, ChartEntryName), payload.Chart);
                SongLibraryEntry song = ValidateChartOnlySong(
                    temporary,
                    Utf8WithoutBom.GetString(payload.Manifest));
                string destination = Path.Combine(root, song.Id);

                if (Directory.Exists(destination))
                {
                    Directory.Delete(temporary, true);
                    return new HtkSongImportResult(song.Id, destination, HtkSongImportStatus.AlreadyInstalled);
                }

                Directory.Move(temporary, destination);
                return new HtkSongImportResult(song.Id, destination, HtkSongImportStatus.Imported);
            }
            catch (HtkSongPackageException)
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                throw;
            }
            catch (SongLibraryLoadException exception)
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                throw new HtkSongPackageException("The package contains an invalid song manifest or chart.", exception);
            }
            catch (Exception exception)
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
                throw new HtkSongPackageException("The .htksong package could not be imported.", exception);
            }
        }

        public HtkSongInboxResult ImportInbox(string libraryRoot)
        {
            if (string.IsNullOrWhiteSpace(libraryRoot))
                throw new ArgumentException("Song-library root is required.", nameof(libraryRoot));
            string root = Path.GetFullPath(libraryRoot);
            Directory.CreateDirectory(root);

            string[] packages = Directory.GetFiles(root, "*" + Extension, SearchOption.TopDirectoryOnly);
            Array.Sort(packages, StringComparer.Ordinal);
            var imported = new List<string>();
            var diagnostics = new List<string>();
            foreach (string package in packages)
            {
                try
                {
                    HtkSongImportResult result = ImportChartOnlyPackage(package, root);
                    if (result.Status == HtkSongImportStatus.Imported) imported.Add(result.SongId);
                }
                catch (HtkSongPackageException exception)
                {
                    diagnostics.Add($"Package '{Path.GetFileName(package)}' was ignored: {exception.Message}");
                }
                catch (Exception exception)
                {
                    diagnostics.Add(
                        $"Package '{Path.GetFileName(package)}' could not be read ({exception.GetType().Name}).");
                }
            }

            return new HtkSongInboxResult(
                new ReadOnlyCollection<string>(imported),
                new ReadOnlyCollection<string>(diagnostics));
        }

        private static PackagePayload ReadPackage(string packagePath)
        {
            ValidatePackageFile(packagePath);
            try
            {
                using (var stream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Utf8WithoutBom))
                {
                    if (archive.Entries.Count != 3)
                        throw new HtkSongPackageException("A chart-only package must contain exactly three entries.");
                    var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
                    var caseInsensitiveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!caseInsensitiveNames.Add(entry.FullName))
                            throw new HtkSongPackageException("Package entry names must be unique case-insensitively.");
                        if (!IsAllowedEntry(entry.FullName))
                            throw new HtkSongPackageException("A chart-only package contains an unsupported entry.");
                        entries.Add(entry.FullName, entry);
                    }

                    byte[] version = ReadEntry(entries[VersionEntryName], 16, VersionEntryName);
                    if (!string.Equals(Encoding.ASCII.GetString(version), PackageVersion + "\n", StringComparison.Ordinal))
                        throw new HtkSongPackageException("Unsupported .htksong package version.");
                    byte[] manifest = ReadEntry(entries[ManifestEntryName], MaximumManifestBytes, ManifestEntryName);
                    byte[] chart = ReadEntry(entries[ChartEntryName], MaximumChartBytes, ChartEntryName);
                    return new PackagePayload(manifest, chart);
                }
            }
            catch (HtkSongPackageException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new HtkSongPackageException("The file is not a valid .htksong archive.", exception);
            }
        }

        private static SongLibraryEntry ValidateChartOnlySong(string folder, string manifestJson)
        {
            SongLibraryEntry song = new SongLibraryDiscovery().Parse(
                manifestJson,
                folder,
                SongLibraryOrigin.UserFolder);
            if (song.AudioAvailability != SongAudioAvailability.Missing || song.AudioPath != null)
                throw new HtkSongPackageException("Chart-only packages cannot contain or declare audio.");
            if (song.ChartAvailability != SongChartAvailability.Available ||
                !string.Equals(Path.GetFileName(song.ChartPath), ChartEntryName, StringComparison.Ordinal))
                throw new HtkSongPackageException("A chart-only package must declare notes.json as available.");
            return song;
        }

        private static byte[] ReadEntry(ZipArchiveEntry entry, long maximumBytes, string name)
        {
            if (entry.Length <= 0 || entry.Length > maximumBytes)
                throw new HtkSongPackageException($"{name} exceeds the allowed package size.");
            if (entry.CompressedLength < 0 || entry.CompressedLength > MaximumPackageBytes)
                throw new HtkSongPackageException($"{name} has an invalid compressed size.");

            using (Stream source = entry.Open())
            using (var destination = new MemoryStream((int)entry.Length))
            {
                var buffer = new byte[8192];
                long total = 0;
                while (true)
                {
                    int read = source.Read(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    total += read;
                    if (total > maximumBytes)
                        throw new HtkSongPackageException($"{name} exceeds the allowed package size.");
                    destination.Write(buffer, 0, read);
                }
                if (total != entry.Length)
                    throw new HtkSongPackageException($"{name} has an inconsistent uncompressed size.");
                return destination.ToArray();
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, byte[] content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = StableEntryTimestamp;
            using (Stream stream = entry.Open()) stream.Write(content, 0, content.Length);
        }

        private static bool IsAllowedEntry(string name) =>
            string.Equals(name, VersionEntryName, StringComparison.Ordinal) ||
            string.Equals(name, ManifestEntryName, StringComparison.Ordinal) ||
            string.Equals(name, ChartEntryName, StringComparison.Ordinal);

        private static void ValidatePackagePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Package path is required.", nameof(path));
            if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
                throw new HtkSongPackageException("Chart packages must use the .htksong extension.");
        }

        private static void ValidatePackageFile(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumPackageBytes)
                throw new HtkSongPackageException("A .htksong package must be between 1 byte and 5 MiB.");
        }

        private static void ValidateSourceFile(string path, long maximumBytes, string name)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > maximumBytes)
                throw new HtkSongPackageException($"{name} is missing or exceeds the allowed size.");
            if (IsLink(path)) throw new HtkSongPackageException($"{name} cannot be a symbolic link.");
        }

        private static bool IsLink(string path) =>
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

        private sealed class PackagePayload
        {
            public PackagePayload(byte[] manifest, byte[] chart)
            {
                Manifest = manifest;
                Chart = chart;
            }

            public byte[] Manifest { get; }
            public byte[] Chart { get; }
        }
    }
}
