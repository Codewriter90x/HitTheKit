using System.Text;

namespace HitTheKit.MidiCapture;

public sealed record StepSummary(
    string StepId,
    int EventCount,
    int PositiveNoteOnCount,
    IReadOnlyDictionary<int, int> NoteFrequencies,
    IReadOnlyList<int> Channels,
    int? VelocityMinimum,
    int? VelocityMaximum,
    double? VelocityMedian,
    IReadOnlyDictionary<int, ValueRange> ControlChanges,
    int AftertouchCount,
    int NoteOffCount,
    int UniqueEventCount,
    bool PossibleNoiseOrCrosstalk);

public sealed record ValueRange(int Minimum, int Maximum);

public sealed record CaptureSummary(
    int SchemaVersion,
    int EventCount,
    IReadOnlyDictionary<int, int> PositiveNoteFrequencies,
    IReadOnlyList<int> Channels,
    int? VelocityMinimum,
    int? VelocityMaximum,
    double? VelocityMedian,
    IReadOnlyDictionary<int, ValueRange> ControlChanges,
    int AftertouchCount,
    int NoteOffCount,
    int UnknownEventCount,
    IReadOnlyList<StepSummary> Steps)
{
    public const int CurrentSchemaVersion = 1;
}

public static class SummaryService
{
    public static CaptureSummary Create(IReadOnlyList<CaptureEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var positiveNotes = events.Where(IsPositiveNoteOn).ToArray();
        return new(
            CaptureSummary.CurrentSchemaVersion,
            events.Count,
            Frequencies(positiveNotes.Select(value => value.Data1!.Value)),
            events.Where(value => value.Channel.HasValue).Select(value => value.Channel!.Value).Distinct().Order().ToArray(),
            Minimum(positiveNotes.Select(value => value.Data2!.Value)),
            Maximum(positiveNotes.Select(value => value.Data2!.Value)),
            Median(positiveNotes.Select(value => value.Data2!.Value)),
            ControlRanges(events),
            events.Count(IsAftertouch),
            events.Count(IsNoteOff),
            events.Count(value => !KnownKinds.Contains(value.RawKind)),
            events.Where(value => value.StepId is not null)
                .GroupBy(value => value.StepId!, StringComparer.Ordinal)
                .OrderBy(group => group.Min(value => value.Sequence))
                .Select(group => CreateStep(group.Key, group.ToArray()))
                .ToArray());
    }

    public static string ToText(CaptureSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var text = new StringBuilder();
        text.AppendLine("HitTheKit MIDI capture summary");
        text.AppendLine($"Events: {summary.EventCount}");
        text.AppendLine($"Channels (1-based): {Join(summary.Channels.Select(value => value + 1))}");
        text.AppendLine($"Positive NoteOn notes: {Join(summary.PositiveNoteFrequencies.Select(pair => $"{pair.Key} ({pair.Value})"))}");
        text.AppendLine($"Velocity range/median: {Format(summary.VelocityMinimum)}–{Format(summary.VelocityMaximum)} / {Format(summary.VelocityMedian)}");
        text.AppendLine($"Control changes: {Join(summary.ControlChanges.Select(pair => $"CC{pair.Key}={pair.Value.Minimum}–{pair.Value.Maximum}"))}");
        text.AppendLine($"Aftertouch: {summary.AftertouchCount}; NoteOff equivalents: {summary.NoteOffCount}; unknown/system: {summary.UnknownEventCount}");
        foreach (StepSummary step in summary.Steps)
        {
            text.AppendLine();
            text.AppendLine($"Step: {step.StepId}");
            text.AppendLine($"  events={step.EventCount}; positiveNoteOn={step.PositiveNoteOnCount}; channels={Join(step.Channels.Select(value => value + 1))}");
            text.AppendLine($"  observed notes={Join(step.NoteFrequencies.Select(pair => $"{pair.Key} ({pair.Value})"))}");
            text.AppendLine($"  velocity={Format(step.VelocityMinimum)}–{Format(step.VelocityMaximum)}; median={Format(step.VelocityMedian)}");
            text.AppendLine($"  controllers={Join(step.ControlChanges.Select(pair => $"CC{pair.Key}={pair.Value.Minimum}–{pair.Value.Maximum}"))}");
            text.AppendLine($"  aftertouch={step.AftertouchCount}; noteOff={step.NoteOffCount}; unique={step.UniqueEventCount}");
            if (step.PossibleNoiseOrCrosstalk)
                text.AppendLine("  observation: multiple positive notes were observed; review for intended articulation or possible crosstalk.");
        }
        text.AppendLine();
        text.AppendLine("Observations are capture evidence, not a verified device mapping.");
        return text.ToString();
    }

    private static StepSummary CreateStep(string stepId, IReadOnlyList<CaptureEvent> events)
    {
        var positiveNotes = events.Where(IsPositiveNoteOn).ToArray();
        var noteFrequencies = Frequencies(positiveNotes.Select(value => value.Data1!.Value));
        return new(
            stepId,
            events.Count,
            positiveNotes.Length,
            noteFrequencies,
            events.Where(value => value.Channel.HasValue).Select(value => value.Channel!.Value).Distinct().Order().ToArray(),
            Minimum(positiveNotes.Select(value => value.Data2!.Value)),
            Maximum(positiveNotes.Select(value => value.Data2!.Value)),
            Median(positiveNotes.Select(value => value.Data2!.Value)),
            ControlRanges(events),
            events.Count(IsAftertouch),
            events.Count(IsNoteOff),
            events.Select(value => $"{value.RawKind}|{value.Channel}|{value.Data1}|{value.Data2}").Distinct(StringComparer.Ordinal).Count(),
            noteFrequencies.Count > 1);
    }

    private static IReadOnlyDictionary<int, int> Frequencies(IEnumerable<int> values) =>
        values.GroupBy(value => value).OrderBy(group => group.Key).ToDictionary(group => group.Key, group => group.Count());

    private static IReadOnlyDictionary<int, ValueRange> ControlRanges(IEnumerable<CaptureEvent> values) =>
        values.Where(value => value.RawKind == "controlChange" && value.Data1.HasValue && value.Data2.HasValue)
            .GroupBy(value => value.Data1!.Value)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => new ValueRange(group.Min(value => value.Data2!.Value), group.Max(value => value.Data2!.Value)));

    private static bool IsPositiveNoteOn(CaptureEvent value) => value.RawKind == "noteOn" && value.Data2 is > 0;
    private static bool IsNoteOff(CaptureEvent value) => value.RawKind == "noteOff" || value.IsNoteOffEquivalent;
    private static bool IsAftertouch(CaptureEvent value) => value.RawKind is "polyAftertouch" or "channelAftertouch";
    private static int? Minimum(IEnumerable<int> values) => values.Cast<int?>().Min();
    private static int? Maximum(IEnumerable<int> values) => values.Cast<int?>().Max();
    private static double? Median(IEnumerable<int> values)
    {
        int[] ordered = values.Order().ToArray();
        if (ordered.Length == 0) return null;
        int middle = ordered.Length / 2;
        return ordered.Length % 2 == 1 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2.0;
    }

    private static string Join<T>(IEnumerable<T> values)
    {
        string joined = string.Join(", ", values);
        return joined.Length == 0 ? "none" : joined;
    }

    private static string Format(object? value) => value?.ToString() ?? "n/a";

    private static readonly HashSet<string> KnownKinds = new(StringComparer.Ordinal)
    {
        "noteOn", "noteOff", "controlChange", "polyAftertouch", "channelAftertouch", "pitchBend", "programChange", "normalSysEx", "escapeSysEx"
    };
}
