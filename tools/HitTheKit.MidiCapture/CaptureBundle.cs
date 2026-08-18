using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HitTheKit.MidiCapture;

public sealed record BundleVerification(bool IsValid, IReadOnlyList<string> Errors, int EventCount);

public static class CaptureBundle
{
    public static readonly string[] RequiredFiles =
    {
        "session.json", "events.jsonl", "events.json", "summary.json", "summary.txt", "README.txt", "logs/capture.log", "manifest.sha256"
    };

    public static async Task<string> CreateAsync(
        string outputDirectory,
        CaptureSession session,
        IReadOnlyList<CaptureEvent> events,
        string captureLog,
        CancellationToken cancellationToken = default,
        bool finalizeExistingJournal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(events);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (finalizeExistingJournal) ValidateFinalizationDirectory(outputDirectory);
        else PrepareNewOutputDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "logs"));

        await AtomicFile.WriteJsonAsync(Path.Combine(outputDirectory, "session.json"), session, CaptureJson.Indented, cancellationToken);
        string jsonl = string.Join('\n', events.Select(value => JsonSerializer.Serialize(value, CaptureJson.Compact))) + (events.Count > 0 ? "\n" : string.Empty);
        await AtomicFile.WriteTextAsync(Path.Combine(outputDirectory, "events.jsonl"), jsonl, cancellationToken);
        await AtomicFile.WriteJsonAsync(Path.Combine(outputDirectory, "events.json"), events, CaptureJson.Indented, cancellationToken);

        CaptureSummary summary = SummaryService.Create(events);
        await AtomicFile.WriteJsonAsync(Path.Combine(outputDirectory, "summary.json"), summary, CaptureJson.Indented, cancellationToken);
        await AtomicFile.WriteTextAsync(Path.Combine(outputDirectory, "summary.txt"), SummaryService.ToText(summary), cancellationToken);
        await AtomicFile.WriteTextAsync(Path.Combine(outputDirectory, "README.txt"), BundleReadme(session), cancellationToken);
        await AtomicFile.WriteTextAsync(Path.Combine(outputDirectory, "logs", "capture.log"), captureLog, cancellationToken);
        await WriteManifestAsync(outputDirectory, cancellationToken);
        return outputDirectory;
    }

    public static async Task<IReadOnlyList<CaptureEvent>> ReadEventsAsync(string bundleDirectory, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(bundleDirectory, "events.jsonl");
        var events = new List<CaptureEvent>();
        if (!File.Exists(path)) throw new FileNotFoundException("events.jsonl is missing.", path);
        await EnsureFinalNewlineAsync(path, cancellationToken);
        using var reader = new StreamReader(path, new UTF8Encoding(false, true), true);
        int lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) throw new InvalidDataException($"events.jsonl line {lineNumber} is empty.");
            try
            {
                CaptureEvent? value = JsonSerializer.Deserialize<CaptureEvent>(line, CaptureJson.Compact);
                events.Add(value ?? throw new InvalidDataException($"events.jsonl line {lineNumber} contains a null event."));
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"events.jsonl line {lineNumber} is not complete valid JSON.", exception);
            }
        }
        return events;
    }

    public static async Task<BundleVerification> VerifyAsync(string bundleDirectory, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        IReadOnlyList<CaptureEvent> events = Array.Empty<CaptureEvent>();
        try
        {
            ValidateBundleLayout(bundleDirectory);
            foreach (string required in RequiredFiles)
                if (!File.Exists(Path.Combine(bundleDirectory, required))) errors.Add($"Missing file: {required}");
            if (File.Exists(Path.Combine(bundleDirectory, "events.jsonl")))
                events = await ReadEventsAsync(bundleDirectory, cancellationToken);
            ValidateEvents(events, errors);
            string sessionPath = Path.Combine(bundleDirectory, "session.json");
            if (File.Exists(sessionPath))
            {
                CaptureSession? session = JsonSerializer.Deserialize<CaptureSession>(await File.ReadAllTextAsync(sessionPath, cancellationToken), CaptureJson.Compact);
                if (session is null) errors.Add("session.json is null.");
                else
                {
                    if (session.SchemaVersion != CaptureSession.CurrentSchemaVersion) errors.Add($"Unsupported session schema: {session.SchemaVersion}");
                    if (session.EventCount != events.Count) errors.Add($"Session event count {session.EventCount} does not match {events.Count} JSONL events.");
                    if (!double.IsFinite(session.StartMonotonicSeconds) || !double.IsFinite(session.EndMonotonicSeconds) || session.EndMonotonicSeconds < session.StartMonotonicSeconds)
                        errors.Add("Session monotonic timestamps are invalid.");
                }
            }
            string eventsArrayPath = Path.Combine(bundleDirectory, "events.json");
            if (File.Exists(eventsArrayPath))
            {
                CaptureEvent[]? array = JsonSerializer.Deserialize<CaptureEvent[]>(await File.ReadAllTextAsync(eventsArrayPath, cancellationToken), CaptureJson.Compact);
                if (array is null || !array.SequenceEqual(events)) errors.Add("events.json does not match events.jsonl.");
            }
            string summaryPath = Path.Combine(bundleDirectory, "summary.json");
            if (File.Exists(summaryPath) && JsonSerializer.Deserialize<CaptureSummary>(await File.ReadAllTextAsync(summaryPath, cancellationToken), CaptureJson.Compact) is null)
                errors.Add("summary.json is null.");
            await VerifyManifestAsync(bundleDirectory, errors, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or IOException or DecoderFallbackException)
        {
            errors.Add($"Bundle parse error: {exception.Message}");
        }
        return new(errors.Count == 0, errors, events.Count);
    }

    public static async Task<string> PackAsync(string bundleDirectory, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        BundleVerification verification = await VerifyAsync(bundleDirectory, cancellationToken);
        if (!verification.IsValid) throw new InvalidDataException("Bundle verification failed: " + string.Join("; ", verification.Errors));

        string fullBundle = Path.GetFullPath(bundleDirectory).TrimEnd(Path.DirectorySeparatorChar);
        outputPath ??= fullBundle + ".zip";
        outputPath = Path.GetFullPath(outputPath);
        if (outputPath.StartsWith(fullBundle + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("The ZIP output must be outside the bundle directory.", nameof(outputPath));
        if (File.Exists(outputPath)) throw new IOException($"The ZIP output already exists: {outputPath}");
        string temporary = outputPath + $".tmp-{Guid.NewGuid():N}";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        try
        {
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                string rootName = Path.GetFileName(fullBundle);
                foreach (string file in EnumerateFiles(fullBundle))
                {
                    string relative = Path.GetRelativePath(fullBundle, file).Replace(Path.DirectorySeparatorChar, '/');
                    ZipArchiveEntry entry = archive.CreateEntry($"{rootName}/{relative}", CompressionLevel.Optimal);
                    entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    await using Stream destination = entry.Open();
                    await using FileStream source = File.OpenRead(file);
                    await source.CopyToAsync(destination, cancellationToken);
                }
            }
            File.Move(temporary, outputPath, false);
            return outputPath;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static async Task WriteManifestAsync(string bundleDirectory, CancellationToken cancellationToken = default)
    {
        ValidateBundleLayout(bundleDirectory);
        string manifestPath = Path.Combine(bundleDirectory, "manifest.sha256");
        string content = string.Join('\n', EnumerateFiles(bundleDirectory)
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(manifestPath), StringComparison.Ordinal))
            .Select(path => $"{ComputeSha256(path)}  {Path.GetRelativePath(bundleDirectory, path).Replace(Path.DirectorySeparatorChar, '/')}")
            .Order(StringComparer.Ordinal)) + "\n";
        await AtomicFile.WriteTextAsync(manifestPath, content, cancellationToken);
    }

    private static async Task VerifyManifestAsync(string directory, ICollection<string> errors, CancellationToken cancellationToken)
    {
        string manifest = Path.Combine(directory, "manifest.sha256");
        if (!File.Exists(manifest)) return;
        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in await File.ReadAllLinesAsync(manifest, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            int separator = line.IndexOf("  ", StringComparison.Ordinal);
            if (separator != 64) { errors.Add($"Malformed manifest line: {line}"); continue; }
            string hash = line[..separator];
            string relative = line[(separator + 2)..];
            if (!listed.Add(relative)) errors.Add($"Duplicate manifest entry: {relative}");
            if (Path.IsPathRooted(relative) || relative.Split('/').Any(part => part is "" or "." or ".."))
            { errors.Add($"Unsafe manifest path: {relative}"); continue; }
            if (hash.Length != 64 || hash.Any(character => !Uri.IsHexDigit(character)))
            { errors.Add($"Invalid SHA-256 value for: {relative}"); continue; }
            string file = Path.GetFullPath(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar)));
            string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!file.StartsWith(root, StringComparison.Ordinal)) { errors.Add($"Unsafe manifest path: {relative}"); continue; }
            if (!File.Exists(file)) errors.Add($"Manifest file missing: {relative}");
            else if (!string.Equals(hash, ComputeSha256(file), StringComparison.OrdinalIgnoreCase)) errors.Add($"Hash mismatch: {relative}");
        }
        string[] actual = EnumerateFiles(directory)
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(manifest), StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
        foreach (string relative in actual)
            if (!listed.Contains(relative)) errors.Add($"File is not listed in manifest: {relative}");
        foreach (string relative in listed)
            if (!actual.Contains(relative, StringComparer.Ordinal)) errors.Add($"Manifest entry has no file: {relative}");
    }

    private static void ValidateEvents(IReadOnlyList<CaptureEvent> events, ICollection<string> errors)
    {
        long expected = 1;
        double previous = 0;
        foreach (CaptureEvent value in events)
        {
            if (value.SchemaVersion != CaptureEvent.CurrentSchemaVersion) errors.Add($"Event {value.Sequence} has unsupported schema {value.SchemaVersion}.");
            if (value.Sequence != expected) errors.Add($"Expected sequence {expected}, got {value.Sequence}.");
            if (!double.IsFinite(value.ElapsedSeconds) || value.ElapsedSeconds < 0 || value.ElapsedSeconds < previous) errors.Add($"Event {value.Sequence} has an invalid timestamp.");
            if (string.IsNullOrWhiteSpace(value.RawKind)) errors.Add($"Event {value.Sequence} has no raw kind.");
            if (value.Channel is < 0 or > 15) errors.Add($"Event {value.Sequence} has an invalid channel.");
            if (value.Data1 is < 0 or > 127) errors.Add($"Event {value.Sequence} has invalid data1.");
            int maximumData2 = value.RawKind == "pitchBend" ? 16383 : 127;
            if (value.Data2 < 0 || value.Data2 > maximumData2) errors.Add($"Event {value.Sequence} has invalid data2/value.");
            previous = value.ElapsedSeconds;
            expected++;
        }
    }

    public static string PrepareNewOutputDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        string fullPath = Path.GetFullPath(outputDirectory);
        if (File.Exists(fullPath)) throw new IOException($"The output path is an existing file: {fullPath}");
        if (Directory.Exists(fullPath))
        {
            if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("The output directory cannot be a symbolic link.");
            if (Directory.EnumerateFileSystemEntries(fullPath).Any())
                throw new IOException($"The output directory is not empty: {fullPath}");
        }
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private static void ValidateFinalizationDirectory(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException(outputDirectory);
        if ((File.GetAttributes(outputDirectory) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The output directory cannot be a symbolic link.");
        foreach (string entry in Directory.EnumerateFileSystemEntries(outputDirectory))
        {
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Capture output contains a symbolic link.");
            if (!string.Equals(Path.GetFileName(entry), "events.jsonl", StringComparison.Ordinal) || Directory.Exists(entry))
                throw new IOException($"Capture output contains an unexpected pre-existing entry: {Path.GetFileName(entry)}");
        }
    }

    private static void ValidateBundleLayout(string directory)
    {
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The bundle directory cannot be a symbolic link.");
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            FileAttributes attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Bundle contains a symbolic link: {Path.GetFileName(entry)}");
            if ((attributes & FileAttributes.Directory) != 0 && !string.Equals(Path.GetFileName(entry), "logs", StringComparison.Ordinal))
                throw new InvalidDataException($"Bundle contains an unexpected directory: {Path.GetFileName(entry)}");
        }
        string logs = Path.Combine(directory, "logs");
        if (Directory.Exists(logs))
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(logs))
            {
                FileAttributes attributes = File.GetAttributes(entry);
                if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                    throw new InvalidDataException($"Bundle logs contain an unsafe entry: {Path.GetFileName(entry)}");
            }
        }
    }

    private static IReadOnlyList<string> EnumerateFiles(string directory)
    {
        ValidateBundleLayout(directory);
        string[] files = Directory.EnumerateFiles(directory)
            .Concat(Directory.Exists(Path.Combine(directory, "logs"))
                ? Directory.EnumerateFiles(Path.Combine(directory, "logs"))
                : Array.Empty<string>())
            .OrderBy(path => Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/'), StringComparer.Ordinal)
            .ToArray();
        var caseInsensitivePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Bundle contains a symbolic link: {Path.GetFileName(file)}");
            string relative = Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/');
            if (!caseInsensitivePaths.Add(relative))
                throw new InvalidDataException($"Bundle contains a case-insensitive path collision: {relative}");
        }
        return files;
    }

    private static async Task EnsureFinalNewlineAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.Asynchronous);
        if (stream.Length == 0) return;
        stream.Seek(-1, SeekOrigin.End);
        var buffer = new byte[1];
        if (await stream.ReadAsync(buffer, cancellationToken) != 1 || buffer[0] != (byte)'\n')
            throw new InvalidDataException("events.jsonl does not end with a newline and may be truncated.");
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string BundleReadme(CaptureSession session) => $"""
        HitTheKit portable MIDI capture bundle

        Capture mode: {session.CaptureMode}
        Device label: {session.DeviceDisplayName}
        Created (UTC): {session.CreatedUtc:O}

        This bundle contains MIDI event evidence and minimal technical metadata only.
        It contains no audio, video, desktop recording, username, hostname, network address,
        computer serial number, or Apple ID.

        Verify after transfer:
          hitthekit-midi-capture verify --input <this-directory>

        Transfer the directory or its ZIP to the Mac Studio using AirDrop, iCloud Drive,
        removable storage, or a trusted local network. Observed messages and suggested
        articulations are capture evidence, not a verified device profile.
        """;
}

public static class AtomicFile
{
    public static Task WriteJsonAsync<T>(string path, T value, JsonSerializerOptions options, CancellationToken cancellationToken = default) =>
        WriteTextAsync(path, JsonSerializer.Serialize(value, options) + "\n", cancellationToken);

    public static async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = fullPath + $".tmp-{Guid.NewGuid():N}";
        try
        {
            await File.WriteAllTextAsync(temporary, content, new UTF8Encoding(false), cancellationToken);
            File.Move(temporary, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

public sealed class JsonlCaptureJournal : IAsyncDisposable
{
    private readonly FileStream stream;
    private readonly StreamWriter writer;

    public JsonlCaptureJournal(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        writer = new(stream, new UTF8Encoding(false));
    }

    public async Task AppendAsync(CaptureEvent value, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(value, CaptureJson.Compact).AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await writer.FlushAsync();
        await writer.DisposeAsync();
        await stream.DisposeAsync();
    }
}
