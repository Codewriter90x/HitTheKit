using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Melanchall.DryWetMidi.Multimedia;

namespace HitTheKit.MidiCapture;

public static class Program
{
    public const int Success = 0;
    public const int UsageError = 2;
    public const int OperationalError = 3;
    public const int VerificationError = 4;

    public static async Task<int> Main(string[] args)
    {
        if (!CliParser.TryParse(args, out CliOptions? options, out string? error))
        {
            Console.Error.WriteLine(error);
            PrintHelp();
            return UsageError;
        }
        try
        {
            return options!.Command switch
            {
                CaptureCommand.Help => Help(),
                CaptureCommand.Doctor => Doctor(),
                CaptureCommand.List => ListDevices(),
                CaptureCommand.Listen => await ListenAsync(options),
                CaptureCommand.Capture => await CaptureAsync(options),
                CaptureCommand.GuidedCapture => await GuidedCaptureAsync(options),
                CaptureCommand.Summarize => await SummarizeAsync(options),
                CaptureCommand.Verify => await VerifyAsync(options),
                CaptureCommand.Pack => await PackAsync(options),
                CaptureCommand.Replay => await ReplayAsync(options),
                _ => UsageError
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or MidiDeviceException or JsonException)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return OperationalError;
        }
    }

    private static int Help() { PrintHelp(); return Success; }

    private static int Doctor()
    {
        Assembly tool = typeof(Program).Assembly;
        Assembly dryWetMidi = typeof(InputDevice).Assembly;
        Console.WriteLine("HitTheKit MIDI Capture doctor");
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"Tool: {tool.GetName().Version}");
        Console.WriteLine($"DryWetMIDI: {dryWetMidi.GetName().Version}");
        try
        {
            int count = InputDevice.GetDevicesCount();
            Console.WriteLine($"Multimedia API: available ({count} input device(s))");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Multimedia API: unavailable ({exception.GetType().Name}: {exception.Message})");
            return OperationalError;
        }
        string probe = Path.Combine(Path.GetTempPath(), $"hitthekit-write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probe, "ok");
            Console.WriteLine("Temporary output: writable");
        }
        finally
        {
            if (File.Exists(probe)) File.Delete(probe);
        }
        Console.WriteLine("Privacy: no computer identity or network metadata is collected.");
        return Success;
    }

    private static int ListDevices()
    {
        IReadOnlyList<DeviceSnapshot> devices = DeviceServices.List();
        if (devices.Count == 0) Console.WriteLine("No MIDI input devices found.");
        foreach (DeviceSnapshot value in devices)
            Console.WriteLine($"[{value.Index}] {value.Name}{(string.IsNullOrWhiteSpace(value.Manufacturer) ? string.Empty : $" — {value.Manufacturer}")}");
        return Success;
    }

    private static async Task<int> ListenAsync(CliOptions options)
    {
        using InputDevice device = DeviceServices.Open(options.DeviceIndex, options.DeviceName);
        Console.WriteLine($"Listening to '{device.Name}'. Press Ctrl+C to stop.");
        using var cancellation = new ConsoleCancellationScope();
        TimeSpan? duration = options.DurationSeconds > 0 ? TimeSpan.FromSeconds(options.DurationSeconds) : null;
        LiveCaptureResult result = await MidiCaptureRunner.RunAsync(
            device,
            duration,
            value => { if (options.ShowAll || !IsNoisy(value)) Console.WriteLine(MidiEventAdapter.FormatCompact(value)); return ValueTask.CompletedTask; },
            () => null,
            cancellation.Token);
        ReportDeviceErrors(result.Errors);
        return result.Errors.Count == 0 ? Success : OperationalError;
    }

    private static async Task<int> CaptureAsync(CliOptions options)
    {
        string output = ResolveBundleOutput(options.OutputPath, options.Label);
        CaptureBundle.PrepareNewOutputDirectory(output);
        string jsonl = Path.Combine(output, "events.jsonl");
        using InputDevice device = DeviceServices.Open(options.DeviceIndex, options.DeviceName);
        Console.WriteLine($"Capturing '{device.Name}' to '{Path.GetFileName(output)}'. Press Ctrl+C to finish and save.");
        using var cancellation = new ConsoleCancellationScope();
        LiveCaptureResult result;
        await using (var journal = new JsonlCaptureJournal(jsonl))
        {
            result = await MidiCaptureRunner.RunAsync(
                device,
                TimeSpan.FromSeconds(options.DurationSeconds),
                async value => { await journal.AppendAsync(value); if (options.ShowAll || !IsNoisy(value)) Console.WriteLine(MidiEventAdapter.FormatCompact(value)); },
                () => null,
                cancellation.Token);
        }
        var session = CreateSession(device.Name, options, "capture", Array.Empty<CaptureStepDefinition>(), Array.Empty<CaptureStepState>(), result.Events, result.StartSeconds, result.EndSeconds, false);
        await CaptureBundle.CreateAsync(output, session, result.Events, Log(result.Errors), finalizeExistingJournal: true);
        ReportDeviceErrors(result.Errors);
        Console.WriteLine($"Saved {result.Events.Count} events: {output}");
        return result.Errors.Count == 0 ? Success : OperationalError;
    }

    private static async Task<int> GuidedCaptureAsync(CliOptions options)
    {
        string output = ResolveBundleOutput(options.OutputPath, options.Label);
        CaptureBundle.PrepareNewOutputDirectory(output);
        using InputDevice device = DeviceServices.Open(options.DeviceIndex, options.DeviceName);
        IReadOnlyList<CaptureStepDefinition> steps = GuidedCaptureWorkflow.WithOverrides(options.Samples, options.DurationSeconds);
        var sequence = new SequenceGenerator();
        var errors = new List<string>();
        double totalElapsed = 0;
        long acceptedSequence = 0;
        using var cancellation = new ConsoleCancellationScope();
        var journal = new JsonlCaptureJournal(Path.Combine(output, "events.jsonl"));
        GuidedCaptureResult result;
        try
        {
            result = await GuidedCaptureWorkflow.RunAsync(
                steps,
                async (step, attempt, cancellationToken) =>
                {
                    Console.WriteLine();
                    Console.WriteLine($"Step {FindStepIndex(steps, step) + 1}/{steps.Count} — {step.DisplayName} (attempt {attempt})");
                    Console.WriteLine(step.SuggestedDurationSeconds.HasValue
                        ? $"Perform the action for about {step.SuggestedDurationSeconds:0.#} seconds."
                        : $"Perform about {step.TargetSamples} clean repetitions. Press ENTER when complete.");
                    using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token);
                    TimeSpan? duration = step.SuggestedDurationSeconds.HasValue ? TimeSpan.FromSeconds(step.SuggestedDurationSeconds.Value) : null;
                    if (!duration.HasValue)
                        _ = Task.Run(Console.ReadLine, cancellationToken).ContinueWith(_ => stop.Cancel(), CancellationToken.None);
                    double attemptOffset = totalElapsed;
                    LiveCaptureResult captured = await MidiCaptureRunner.RunAsync(
                        device,
                        duration,
                        value => { if (!IsNoisy(value)) Console.WriteLine(MidiEventAdapter.FormatCompact(value)); return ValueTask.CompletedTask; },
                        () => step.Id,
                        stop.Token,
                        sequence,
                        () => attempt);
                    totalElapsed += captured.EndSeconds;
                    errors.AddRange(captured.Errors);
                    return captured.Events.Select(value => value with { ElapsedSeconds = value.ElapsedSeconds + attemptOffset }).ToArray();
                },
                async (step, captured, cancellationToken) =>
                {
                    Console.WriteLine($"Captured {captured.Count} event(s). ENTER=accept, r=retry, s=skip, q=finish and save");
                    string decision = cancellation.Token.IsCancellationRequested
                        ? "q"
                        : (await Task.Run(Console.ReadLine, cancellationToken) ?? string.Empty).Trim().ToLowerInvariant();
                    GuidedDecision value = decision switch { "r" => GuidedDecision.Retry, "s" => GuidedDecision.Skip, "q" => GuidedDecision.Finish, _ => GuidedDecision.Accept };
                    foreach (CaptureEvent item in captured)
                        await journal.AppendAsync(item with { Sequence = ++acceptedSequence }, cancellationToken);
                    return value;
                },
                CancellationToken.None);
        }
        finally
        {
            await journal.DisposeAsync();
        }

        CaptureEvent[] normalizedEvents = result.Events.Select((value, index) => value with { Sequence = index + 1 }).ToArray();
        var session = CreateSession(device.Name, options, "guided-capture", steps, result.Steps, normalizedEvents, 0, totalElapsed, false);
        await CaptureBundle.CreateAsync(output, session, normalizedEvents, Log(errors), finalizeExistingJournal: true);
        Console.WriteLine($"Saved guided capture with {normalizedEvents.Length} events: {output}");
        return errors.Count == 0 ? Success : OperationalError;
    }

    private static async Task<int> SummarizeAsync(CliOptions options)
    {
        IReadOnlyList<CaptureEvent> events = await CaptureBundle.ReadEventsAsync(options.InputPath!);
        CaptureSummary summary = SummaryService.Create(events);
        string text = SummaryService.ToText(summary);
        Console.Write(text);
        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            await AtomicFile.WriteJsonAsync(options.OutputPath!, summary, CaptureJson.Indented);
            await AtomicFile.WriteTextAsync(Path.ChangeExtension(options.OutputPath, ".txt"), text);
        }
        return Success;
    }

    private static async Task<int> VerifyAsync(CliOptions options)
    {
        BundleVerification result = await CaptureBundle.VerifyAsync(options.InputPath!);
        if (result.IsValid) { Console.WriteLine($"Bundle verified: {result.EventCount} events."); return Success; }
        foreach (string error in result.Errors) Console.Error.WriteLine(error);
        return VerificationError;
    }

    private static async Task<int> PackAsync(CliOptions options)
    {
        string path = await CaptureBundle.PackAsync(options.InputPath!, options.OutputPath);
        Console.WriteLine($"Created: {path}");
        return Success;
    }

    private static async Task<int> ReplayAsync(CliOptions options)
    {
        ReplayFixture? fixture = JsonSerializer.Deserialize<ReplayFixture>(await File.ReadAllTextAsync(options.FixturePath!), CaptureJson.Compact);
        if (fixture is null || fixture.SchemaVersion != 1) throw new InvalidDataException("Unsupported or empty replay fixture.");
        CaptureEvent[] events = fixture.Events.Select((value, index) => value with
        {
            SchemaVersion = CaptureEvent.CurrentSchemaVersion,
            Sequence = index + 1,
            IsFoundationCompatible = value.RawKind is "noteOn" or "noteOff" or "controlChange" or "polyAftertouch" or "channelAftertouch"
        }).OrderBy(value => value.Sequence).ToArray();
        string output = ResolveBundleOutput(options.OutputPath, fixture.Label);
        CaptureBundle.PrepareNewOutputDirectory(output);
        var replayOptions = options with { Label = options.Label ?? fixture.Label };
        var session = CreateSession("synthetic-replay", replayOptions, "replay", GuidedCaptureWorkflow.DefaultSteps, StepStates(events), events, 0, events.LastOrDefault()?.ElapsedSeconds ?? 0, true);
        await CaptureBundle.CreateAsync(output, session, events, "Synthetic fixture replay; no MIDI hardware was used.\n");
        BundleVerification verification = await CaptureBundle.VerifyAsync(output);
        if (!verification.IsValid) throw new InvalidDataException(string.Join("; ", verification.Errors));
        Console.WriteLine($"Synthetic replay bundle created and verified: {output}");
        return Success;
    }

    private static CaptureSession CreateSession(
        string deviceName,
        CliOptions options,
        string mode,
        IReadOnlyList<CaptureStepDefinition> definitions,
        IReadOnlyList<CaptureStepState> states,
        IReadOnlyList<CaptureEvent> events,
        double start,
        double end,
        bool synthetic) => new(
            CaptureSession.CurrentSchemaVersion,
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            DateTimeOffset.UtcNow,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            deviceName,
            options.Label,
            mode,
            definitions,
            states,
            events.Count,
            start,
            end,
            options.Notes,
            synthetic);

    private static IReadOnlyList<CaptureStepState> StepStates(IReadOnlyList<CaptureEvent> events) =>
        events.Where(value => value.StepId is not null).GroupBy(value => value.StepId!, StringComparer.Ordinal)
            .Select(group => new CaptureStepState(group.Key, true, false, group.Count())).ToArray();

    private static int FindStepIndex(IReadOnlyList<CaptureStepDefinition> steps, CaptureStepDefinition step)
    {
        for (int index = 0; index < steps.Count; index++)
            if (ReferenceEquals(steps[index], step) || steps[index] == step) return index;
        return -1;
    }

    private static string ResolveBundleOutput(string? requested, string? label)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return Path.GetFullPath(requested);
        string safeLabel = string.IsNullOrWhiteSpace(label) ? "hampback-capture" : new string(label.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeLabel)) safeLabel = "hampback-capture";
        return Path.GetFullPath($"{safeLabel}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}");
    }

    private static bool IsNoisy(CaptureEvent value) => value.RawKind is "activeSensing" or "timingClock";
    private static string Log(IEnumerable<string> errors) => "HitTheKit MIDI capture log\n" + string.Join('\n', errors) + "\n";
    private static void ReportDeviceErrors(IReadOnlyList<string> errors) { foreach (string error in errors) Console.Error.WriteLine($"MIDI error: {error}"); }

    private static void PrintHelp() => Console.WriteLine("""
        HitTheKit MIDI Capture

        Commands:
          doctor
          list
          listen --device <index> [--all] [--duration <seconds>]
          capture --device <index> --output <directory> [--duration <seconds>]
          guided-capture --device <index> [--output <directory>] [--samples <count>] [--duration <seconds>]
          summarize --input <bundle-directory> [--output <summary.json>]
          verify --input <bundle-directory>  (or: verify <bundle-directory>)
          pack --input <bundle-directory> [--output <archive.zip>]
          replay --fixture <synthetic-fixture.json> [--output <directory>]
          help

        A device can also be selected with --device-name <exact name>.
        """);
}
