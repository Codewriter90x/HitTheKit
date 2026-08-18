using System.IO.Compression;
using System.Text.Json;
using HitTheKit.Unity.Devices;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Xunit;

namespace HitTheKit.MidiCapture.Tests;

public sealed class CliAndAdapterTests
{
    [Theory]
    [InlineData("doctor", CaptureCommand.Doctor)]
    [InlineData("list", CaptureCommand.List)]
    [InlineData("guided-capture", CaptureCommand.GuidedCapture)]
    [InlineData("summarize", CaptureCommand.Summarize)]
    [InlineData("verify", CaptureCommand.Verify)]
    [InlineData("pack", CaptureCommand.Pack)]
    [InlineData("replay", CaptureCommand.Replay)]
    public void Parses_commands(string command, CaptureCommand expected)
    {
        string[] args = expected switch
        {
            CaptureCommand.GuidedCapture => [command, "--device", "0"],
            CaptureCommand.Summarize or CaptureCommand.Verify or CaptureCommand.Pack => [command, "--input", "bundle"],
            CaptureCommand.Replay => [command, "--fixture", "fixture.json"],
            _ => [command]
        };
        Assert.True(CliParser.TryParse(args, out CliOptions? options, out _));
        Assert.Equal(expected, options!.Command);
    }

    [Fact]
    public void Parses_capture_options()
    {
        Assert.True(CliParser.TryParse(["capture", "--device", "2", "--duration", "12.5", "--output", "bundle", "--all"], out CliOptions? options, out _));
        Assert.Equal(2, options!.DeviceIndex);
        Assert.Equal(12.5, options.DurationSeconds);
        Assert.True(options.ShowAll);
    }

    [Theory]
    [InlineData("verify", CaptureCommand.Verify)]
    [InlineData("summarize", CaptureCommand.Summarize)]
    [InlineData("pack", CaptureCommand.Pack)]
    public void Accepts_bundle_as_positional_argument(string command, CaptureCommand expected)
    {
        Assert.True(CliParser.TryParse([command, "bundle"], out CliOptions? options, out _));
        Assert.Equal(expected, options!.Command);
        Assert.Equal("bundle", options.InputPath);
    }

    [Fact]
    public void Rejects_invalid_device_index() => Assert.False(CliParser.TryParse(["listen", "--device", "-1"], out _, out _));

    [Fact]
    public void Selects_device_and_rejects_invalid_or_ambiguous_selection()
    {
        DeviceSnapshot[] devices = [new(0, "A", null), new(1, "B", "Maker")];
        Assert.Equal(1, DeviceServices.SelectIndex(devices, 1, null));
        Assert.Equal(0, DeviceServices.SelectIndex(devices, null, "A"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DeviceServices.SelectIndex(devices, 3, null));
        Assert.Throws<ArgumentException>(() => DeviceServices.SelectIndex([new(0, "A", null), new(1, "A", null)], null, "A"));
    }

    [Fact]
    public void Converts_note_on_through_foundation_contract()
    {
        var midi = new NoteOnEvent((SevenBitNumber)38, (SevenBitNumber)112) { Channel = (FourBitNumber)9 };
        AdaptedMidiEvent result = MidiEventAdapter.Adapt(midi, 1, .25);
        Assert.Equal("noteOn", result.Capture.RawKind);
        Assert.Equal(38, result.Capture.Data1);
        Assert.Equal(112, result.Capture.Data2);
        Assert.Equal(RawMidiMessageKind.NoteOn, result.FoundationMessage!.Kind);
        Assert.Equal(.25, result.FoundationMessage.TimestampSeconds);
    }

    [Fact]
    public void Preserves_zero_velocity_note_on()
    {
        AdaptedMidiEvent result = MidiEventAdapter.Adapt(new NoteOnEvent((SevenBitNumber)42, (SevenBitNumber)0), 1, 0);
        Assert.Equal("noteOn", result.Capture.RawKind);
        Assert.True(result.Capture.IsNoteOffEquivalent);
        Assert.Equal(RawMidiMessageKind.NoteOff, result.FoundationMessage!.SemanticKind);
    }

    [Theory]
    [MemberData(nameof(FoundationCases))]
    public void Converts_foundation_supported_events(MidiEvent midi, RawMidiMessageKind kind)
    {
        AdaptedMidiEvent result = MidiEventAdapter.Adapt(midi, 1, 0);
        Assert.True(result.Capture.IsFoundationCompatible);
        Assert.Equal(kind, result.FoundationMessage!.Kind);
    }

    public static IEnumerable<object[]> FoundationCases()
    {
        yield return [new NoteOffEvent((SevenBitNumber)38, (SevenBitNumber)64), RawMidiMessageKind.NoteOff];
        yield return [new ControlChangeEvent((SevenBitNumber)4, (SevenBitNumber)90), RawMidiMessageKind.ControlChange];
        yield return [new NoteAftertouchEvent((SevenBitNumber)38, (SevenBitNumber)75), RawMidiMessageKind.PolyAftertouch];
        yield return [new ChannelAftertouchEvent((SevenBitNumber)75), RawMidiMessageKind.ChannelAftertouch];
    }

    [Fact]
    public void Captures_pitch_program_sysex_and_unknown_without_fabricating_foundation_messages()
    {
        MidiEvent[] values =
        [
            new PitchBendEvent((ushort)8000),
            new ProgramChangeEvent((SevenBitNumber)12),
            new NormalSysExEvent(Enumerable.Range(0, 80).Select(value => (byte)value).ToArray()),
            new TimingClockEvent()
        ];
        AdaptedMidiEvent[] results = values.Select((value, index) => MidiEventAdapter.Adapt(value, index + 1, index)).ToArray();
        Assert.All(results, value => Assert.Null(value.FoundationMessage));
        Assert.Equal(80, results[2].Capture.UnknownLength);
        Assert.True(results[2].Capture.IsTruncated);
        Assert.Equal(128, results[2].Capture.UnknownHexPrefix!.Length);
        Assert.Equal("timingClock", results[3].Capture.RawKind);
    }

    [Fact]
    public void Sequence_generation_is_thread_safe()
    {
        var generator = new SequenceGenerator();
        long[] values = new long[1000];
        Parallel.For(0, values.Length, index => values[index] = generator.Next());
        Assert.Equal(Enumerable.Range(1, 1000).Select(value => (long)value), values.Order());
    }
}

public sealed class BundleTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"hitthekit-midi-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Jsonl_journal_is_append_only_and_flushes()
    {
        string path = Path.Combine(root, "events.jsonl");
        Directory.CreateDirectory(root);
        await using (var journal = new JsonlCaptureJournal(path))
        {
            await journal.AppendAsync(Event(1, .1));
            Assert.Single(await File.ReadAllLinesAsync(path));
            await journal.AppendAsync(Event(2, .2));
        }
        Assert.Equal(2, (await File.ReadAllLinesAsync(path)).Length);
    }

    [Fact]
    public async Task Finalizes_a_progressive_journal_without_accepting_other_existing_files()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureBundle.PrepareNewOutputDirectory(bundle);
        CaptureEvent[] events = [Event(1, .1)];
        await using (var journal = new JsonlCaptureJournal(Path.Combine(bundle, "events.jsonl")))
            await journal.AppendAsync(events[0]);

        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n", finalizeExistingJournal: true);
        Assert.True((await CaptureBundle.VerifyAsync(bundle)).IsValid);

        string unsafeBundle = Path.Combine(root, "unsafe-bundle");
        Directory.CreateDirectory(unsafeBundle);
        await File.WriteAllTextAsync(Path.Combine(unsafeBundle, "existing.txt"), "keep");
        await Assert.ThrowsAsync<IOException>(() => CaptureBundle.CreateAsync(
            unsafeBundle,
            Session(events),
            events,
            "ok\n",
            finalizeExistingJournal: true));
    }

    [Fact]
    public async Task Creates_and_verifies_complete_bundle()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1), Event(2, .2, "controlChange", 4, 98)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.True(verification.IsValid, string.Join("; ", verification.Errors));
        Assert.Equal(2, verification.EventCount);
        Assert.All(CaptureBundle.RequiredFiles, relative => Assert.True(File.Exists(Path.Combine(bundle, relative))));
    }

    [Fact]
    public async Task Detects_corrupted_file()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        await File.AppendAllTextAsync(Path.Combine(bundle, "summary.txt"), "corrupt");
        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.False(verification.IsValid);
        Assert.Contains(verification.Errors, value => value.Contains("Hash mismatch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refuses_to_overwrite_an_existing_capture_or_zip()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        byte[] sessionBefore = await File.ReadAllBytesAsync(Path.Combine(bundle, "session.json"));

        await Assert.ThrowsAsync<IOException>(() => CaptureBundle.CreateAsync(bundle, Session(events), events, "replacement\n"));
        Assert.Equal(sessionBefore, await File.ReadAllBytesAsync(Path.Combine(bundle, "session.json")));

        string zip = await CaptureBundle.PackAsync(bundle, Path.Combine(root, "capture.zip"));
        await Assert.ThrowsAsync<IOException>(() => CaptureBundle.PackAsync(bundle, zip));
    }

    [Fact]
    public async Task Rejects_truncated_jsonl_even_when_manifest_matches()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        string jsonl = Path.Combine(bundle, "events.jsonl");
        byte[] contents = await File.ReadAllBytesAsync(jsonl);
        await File.WriteAllBytesAsync(jsonl, contents[..^1]);
        await CaptureBundle.WriteManifestAsync(bundle);

        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.False(verification.IsValid);
        Assert.Contains(verification.Errors, value => value.Contains("does not end with a newline", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_symbolic_links_in_manifest_and_packaging()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        string outside = Path.Combine(root, "outside.txt");
        await File.WriteAllTextAsync(outside, "external-private-data");
        File.CreateSymbolicLink(Path.Combine(bundle, "leak.txt"), outside);

        await Assert.ThrowsAsync<InvalidDataException>(() => CaptureBundle.WriteManifestAsync(bundle));
        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.False(verification.IsValid);
        Assert.Contains(verification.Errors, value => value.Contains("symbolic link", StringComparison.Ordinal));
        await Assert.ThrowsAsync<InvalidDataException>(() => CaptureBundle.PackAsync(bundle, Path.Combine(root, "unsafe.zip")));
    }

    [Fact]
    public async Task Rejects_unlisted_and_unsafe_manifest_entries()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        await File.WriteAllTextAsync(Path.Combine(bundle, "unexpected.txt"), "extra");
        await File.AppendAllTextAsync(Path.Combine(bundle, "manifest.sha256"), $"{new string('0', 64)}  ../outside\n");
        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.False(verification.IsValid);
        Assert.Contains(verification.Errors, value => value.Contains("not listed", StringComparison.Ordinal));
        Assert.Contains(verification.Errors, value => value.Contains("Unsafe manifest path", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Detects_sequence_and_timestamp_errors()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(2, .2), Event(3, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        BundleVerification verification = await CaptureBundle.VerifyAsync(bundle);
        Assert.False(verification.IsValid);
        Assert.Contains(verification.Errors, value => value.Contains("Expected sequence", StringComparison.Ordinal));
        Assert.Contains(verification.Errors, value => value.Contains("invalid timestamp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Deterministic_zip_has_sorted_entries_and_stable_metadata()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        string first = await CaptureBundle.PackAsync(bundle, Path.Combine(root, "first.zip"));
        string second = await CaptureBundle.PackAsync(bundle, Path.Combine(root, "second.zip"));
        Assert.Equal(await File.ReadAllBytesAsync(first), await File.ReadAllBytesAsync(second));
        using ZipArchive archive = ZipFile.OpenRead(first);
        Assert.Equal(archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal), archive.Entries.Select(entry => entry.FullName));
        Assert.All(archive.Entries, entry => Assert.Equal(1980, entry.LastWriteTime.Year));
    }

    [Fact]
    public async Task Pack_rejects_output_inside_bundle()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        await Assert.ThrowsAsync<ArgumentException>(() => CaptureBundle.PackAsync(bundle, Path.Combine(bundle, "bad.zip")));
    }

    [Fact]
    public async Task Session_metadata_excludes_computer_identity()
    {
        string bundle = Path.Combine(root, "bundle");
        CaptureEvent[] events = [Event(1, .1)];
        await CaptureBundle.CreateAsync(bundle, Session(events), events, "ok\n");
        string json = await File.ReadAllTextAsync(Path.Combine(bundle, "session.json"));
        Assert.DoesNotContain("username", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hostname", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ipAddress", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("serialNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("homePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Summary_excludes_velocity_zero_and_reports_controller_range()
    {
        CaptureSummary summary = SummaryService.Create([
            Event(1, .1, "noteOn", 42, 30),
            Event(2, .2, "noteOn", 42, 100),
            Event(3, .3, "noteOn", 42, 0) with { IsNoteOffEquivalent = true },
            Event(4, .4, "controlChange", 4, 12),
            Event(5, .5, "controlChange", 4, 98)]);
        Assert.Equal(2, summary.PositiveNoteFrequencies[42]);
        Assert.Equal(30, summary.VelocityMinimum);
        Assert.Equal(100, summary.VelocityMaximum);
        Assert.Equal(65, summary.VelocityMedian);
        Assert.Equal(new ValueRange(12, 98), summary.ControlChanges[4]);
        Assert.Equal(1, summary.NoteOffCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private static CaptureEvent Event(long sequence, double elapsed, string kind = "noteOn", int data1 = 36, int data2 = 100) =>
        new(1, sequence, elapsed, "kick", kind, 9, data1, data2, data2 == 0, kind is "noteOn" or "noteOff" or "controlChange");

    private static CaptureSession Session(IReadOnlyList<CaptureEvent> events) =>
        new(1, "1.0.0", DateTimeOffset.UnixEpoch, "macOS", "Arm64", "Synthetic device", "test", "replay", [], [], events.Count, 0, events.LastOrDefault()?.ElapsedSeconds ?? 0, null, true);
}

public sealed class CaptureIngressTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"hitthekit-midi-ingress-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Concurrent_producers_preserve_all_ten_thousand_events_in_sequence_order()
    {
        const int eventCount = 10_000;
        var ingress = new CaptureIngress();
        var consumed = new List<CaptureEvent>(eventCount);
        Task consumer = Task.Run(async () =>
        {
            await foreach (CaptureEvent value in ingress.Reader.ReadAllAsync()) consumed.Add(value);
        });

        Parallel.For(0, eventCount, _ =>
            Assert.True(ingress.TryAccept(sequence => Event(sequence))));
        ingress.Complete();
        await consumer;

        Assert.Equal(eventCount, consumed.Count);
        Assert.Equal(Enumerable.Range(1, eventCount).Select(value => (long)value), consumed.Select(value => value.Sequence));
        Assert.Equal(consumed, ingress.Snapshot());

        string bundle = Path.Combine(root, "stress-bundle");
        await CaptureBundle.CreateAsync(bundle, Session(consumed), consumed, "stress\n");
        Assert.True((await CaptureBundle.VerifyAsync(bundle)).IsValid);
    }

    [Fact]
    public async Task Completion_rejects_late_events_and_drains_every_accepted_event()
    {
        var ingress = new CaptureIngress();
        var consumed = new List<CaptureEvent>();
        Task consumer = Task.Run(async () =>
        {
            await foreach (CaptureEvent value in ingress.Reader.ReadAllAsync()) consumed.Add(value);
        });
        int accepted = 0;
        Task[] producers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (int index = 0; index < 20_000; index++)
            {
                if (!ingress.TryAccept(sequence => Event(sequence))) return;
                Interlocked.Increment(ref accepted);
            }
        })).ToArray();

        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref accepted) >= 1_000, TimeSpan.FromSeconds(5)));
        ingress.Complete();
        await Task.WhenAll(producers);
        await consumer;

        Assert.False(ingress.TryAccept(sequence => Event(sequence)));
        Assert.Equal(accepted, consumed.Count);
        Assert.Equal(accepted, ingress.Snapshot().Count);
        Assert.Equal(Enumerable.Range(1, accepted).Select(value => (long)value), consumed.Select(value => value.Sequence));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private static CaptureEvent Event(long sequence) =>
        new(1, sequence, sequence / 100_000d, "stress", "noteOn", 9, 36, 100, false, true);

    private static CaptureSession Session(IReadOnlyList<CaptureEvent> events) =>
        new(1, "1.0.0", DateTimeOffset.UnixEpoch, "macOS", "Arm64", "Synthetic device", "stress", "replay", [], [], events.Count, 0, events.LastOrDefault()?.ElapsedSeconds ?? 0, null, true);
}

public sealed class GuidedCaptureTests
{
    [Fact]
    public void Includes_all_required_steps_and_defaults()
    {
        string[] expected = ["kick", "snare-center", "snare-rim", "tom-1", "tom-2", "floor-tom", "crash-1", "crash-2-optional", "ride-bow", "ride-bell-optional", "hihat-closed", "hihat-open", "hihat-pedal", "hihat-continuous", "crash-choke", "ride-choke-optional", "free-play"];
        Assert.Equal(expected, GuidedCaptureWorkflow.DefaultSteps.Select(step => step.Id));
        Assert.Equal(3, GuidedCaptureWorkflow.DefaultSteps.Single(step => step.Id == "crash-choke").TargetSamples);
        Assert.Equal(20, GuidedCaptureWorkflow.DefaultSteps.Single(step => step.Id == "free-play").SuggestedDurationSeconds);
    }

    [Fact]
    public async Task Supports_retry_skip_and_early_finish()
    {
        CaptureStepDefinition[] steps = [new("a", "A", false, 1), new("b", "B", true, 1), new("c", "C", false, 1)];
        var attempts = new Dictionary<string, int>();
        GuidedCaptureResult result = await GuidedCaptureWorkflow.RunAsync(
            steps,
            (step, attempt, _) =>
            {
                attempts[step.Id] = attempt;
                IReadOnlyList<CaptureEvent> captured = [new(1, attempt, attempt, step.Id, "noteOn", 9, 36, 100)];
                return Task.FromResult(captured);
            },
            (step, _, _) => Task.FromResult(step.Id switch
            {
                "a" when attempts[step.Id] == 1 => GuidedDecision.Retry,
                "a" => GuidedDecision.Accept,
                "b" => GuidedDecision.Skip,
                _ => GuidedDecision.Finish
            }));
        Assert.True(result.FinishedEarly);
        Assert.Equal(2, attempts["a"]);
        Assert.Equal(4, result.Events.Count);
        Assert.True(result.Steps.Single(step => step.Id == "b").Skipped);
    }

    [Fact]
    public async Task Complete_workflow_preserves_events_by_step()
    {
        CaptureStepDefinition[] steps = [new("a", "A", false, 1), new("b", "B", false, 1)];
        GuidedCaptureResult result = await GuidedCaptureWorkflow.RunAsync(
            steps,
            (step, attempt, _) => Task.FromResult<IReadOnlyList<CaptureEvent>>([new(1, step.Id == "a" ? 1 : 2, step.Id == "a" ? .1 : .2, step.Id, "noteOn", 9, 36, 100)]),
            (_, _, _) => Task.FromResult(GuidedDecision.Accept));
        Assert.False(result.FinishedEarly);
        Assert.Equal(["a", "b"], result.Events.Select(value => value.StepId));
        Assert.All(result.Steps, step => Assert.True(step.Completed));
    }
}
