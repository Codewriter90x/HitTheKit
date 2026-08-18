using System.Text.Json;

namespace HitTheKit.MidiCapture;

public sealed record DeviceSnapshot(int Index, string Name, string? Manufacturer);

public sealed record CaptureStepDefinition(
    string Id,
    string DisplayName,
    bool Optional,
    int TargetSamples,
    double? SuggestedDurationSeconds = null);

public sealed record CaptureStepState(string Id, bool Completed, bool Skipped, int EventCount);

public sealed record CaptureEvent(
    int SchemaVersion,
    long Sequence,
    double ElapsedSeconds,
    string? StepId,
    string RawKind,
    int? Channel,
    int? Data1,
    int? Data2,
    bool IsNoteOffEquivalent = false,
    bool IsFoundationCompatible = false,
    string? UnknownHexPrefix = null,
    int? UnknownLength = null,
    bool IsTruncated = false,
    string? Description = null,
    int? StepAttempt = null)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record CaptureSession(
    int SchemaVersion,
    string CaptureToolVersion,
    DateTimeOffset CreatedUtc,
    string OperatingSystem,
    string Architecture,
    string DeviceDisplayName,
    string? SessionLabel,
    string CaptureMode,
    IReadOnlyList<CaptureStepDefinition> StepDefinitions,
    IReadOnlyList<CaptureStepState> Steps,
    long EventCount,
    double StartMonotonicSeconds,
    double EndMonotonicSeconds,
    string? UserNotes,
    bool IsSyntheticReplay = false)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ReplayFixture(int SchemaVersion, string Label, IReadOnlyList<CaptureEvent> Events);

public static class CaptureJson
{
    public static readonly JsonSerializerOptions Compact = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static readonly JsonSerializerOptions Indented = new(Compact) { WriteIndented = true };
}
