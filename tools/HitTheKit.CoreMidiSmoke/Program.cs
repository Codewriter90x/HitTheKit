using System.Globalization;
using System.Runtime.InteropServices;

namespace HitTheKit.CoreMidiSmoke;

internal static class Program
{
    private const int PollCapacity = 128;

    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsMacOS())
        {
            Console.Error.WriteLine("This tool requires macOS.");
            return 2;
        }
        if (args.Length == 0) return Usage();

        try
        {
            return args[0] switch
            {
                "doctor" when args.Length == 1 => Doctor(),
                "list" when args.Length == 1 => ListDevices(),
                "listen" => await ListenAsync(args[1..]),
                "guided-smoke" when args.Length == 1 => await GuidedSmokeAsync(),
                _ => Usage()
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Stopped.");
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static int Doctor()
    {
        var api = new CoreMidiNativeApi();
        int queueCapacity = 0;
        bool coreMidiAvailable = false;

        if (api.IsLoaded &&
            api.ApiVersion == CoreMidiNativeApi.ExpectedApiVersion &&
            api.GetMonotonicSeconds() > 0.0 &&
            CoreMidiNativeApi.AbiLayoutMatches)
        {
            // Exercise the native lifecycle repeatedly without opening any hardware endpoint.
            coreMidiAvailable = true;
            for (int iteration = 0; iteration < 5; iteration++)
            {
                using var session = new CoreMidiSession(api);
                if (!session.Create(out _))
                {
                    coreMidiAvailable = false;
                    break;
                }
                if (api.GetDiagnostics(out CoreMidiDiagnostics diagnostics) == 0)
                    queueCapacity = diagnostics.QueueCapacity;
            }
        }

        Console.WriteLine($"architecture: {RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}");
        Console.WriteLine($"pluginApi: {CoreMidiNativeApi.PluginName}");
        Console.WriteLine($"coreMidiAvailable: {coreMidiAvailable.ToString().ToLowerInvariant()}");
        Console.WriteLine($"pluginLoad: {api.IsLoaded.ToString().ToLowerInvariant()}");
        Console.WriteLine($"queueCapacity: {queueCapacity}");
        Console.WriteLine($"abiVersion: {api.ApiVersion}");
        return coreMidiAvailable && queueCapacity > 0 ? 0 : 2;
    }

    private static int ListDevices()
    {
        var api = new CoreMidiNativeApi();
        using var session = new CoreMidiSession(api);
        if (!session.Create(out string? error)) return Fail(error);
        if (api.RefreshDevices() < 0) return Fail(api.GetLastError());

        int count = api.GetDeviceCount();
        for (int index = 0; index < count; index++)
        {
            if (api.GetDeviceInfo(index, out CoreMidiDeviceInfo info) != 0)
                return Fail(api.GetLastError());
            PrintDevice(info);
        }
        if (count == 0) Console.WriteLine("No CoreMIDI input endpoints.");
        return 0;
    }

    private static async Task<int> ListenAsync(string[] args)
    {
        if (!TryOption(args, "--device", out int deviceIndex) ||
            !TryOption(args, "--seconds", out int seconds) || seconds is < 1 or > 3600)
            return Usage();

        var api = new CoreMidiNativeApi();
        using var session = new CoreMidiSession(api);
        if (!session.Create(out string? error)) return Fail(error);
        if (api.RefreshDevices() < 0) return Fail(api.GetLastError());
        if (api.GetDeviceCount() == 0)
        {
            Console.WriteLine("No CoreMIDI input endpoints.");
            return 0;
        }
        if (!TrySelectDevice(api, deviceIndex, out CoreMidiDeviceInfo device, out error)) return Fail(error);
        if (api.OpenInput(device.EndpointId) != 0) return Fail(api.GetLastError());

        Console.WriteLine("sequence\ttimestamp\tkind\tchannel\tdata1\tdata2\tisNoteOffEquivalent");
        using var cancellation = ConsoleCancellation();
        await PollForAsync(api, TimeSpan.FromSeconds(seconds), null, cancellation.Token);
        return 0;
    }

    private static async Task<int> GuidedSmokeAsync()
    {
        var api = new CoreMidiNativeApi();
        using var session = new CoreMidiSession(api);
        if (!session.Create(out string? error)) return Fail(error);
        if (api.RefreshDevices() < 0) return Fail(api.GetLastError());

        int count = api.GetDeviceCount();
        if (count == 0)
        {
            Console.WriteLine("No CoreMIDI input endpoints.");
            return 0;
        }
        for (int index = 0; index < count; index++)
        {
            if (api.GetDeviceInfo(index, out CoreMidiDeviceInfo info) == 0) PrintDevice(info);
        }

        Console.Write("Seleziona esplicitamente il device per index: ");
        if (!int.TryParse(Console.ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int selectedIndex) ||
            !TrySelectDevice(api, selectedIndex, out CoreMidiDeviceInfo selected, out error))
            return Fail(error ?? "Device index non valido.");
        if (api.OpenInput(selected.EndpointId) != 0) return Fail(api.GetLastError());

        using var cancellation = ConsoleCancellation();
        Console.WriteLine("Premi la grancassa 5 volte");
        IReadOnlyList<CoreMidiMessage> kick = await CollectFiveStrikesAsync(api, cancellation.Token);
        PrintSummary(kick);

        Console.WriteLine("Colpisci il rullante al centro 5 volte");
        IReadOnlyList<CoreMidiMessage> snare = await CollectFiveStrikesAsync(api, cancellation.Token);
        PrintSummary(snare);
        return 0;
    }

    private static async Task<IReadOnlyList<CoreMidiMessage>> CollectFiveStrikesAsync(
        CoreMidiNativeApi api,
        CancellationToken cancellationToken)
    {
        var messages = new List<CoreMidiMessage>();
        await PollForAsync(
            api,
            Timeout.InfiniteTimeSpan,
            message =>
            {
                messages.Add(message);
                return messages.Count(value =>
                    value.MessageKind == (int)CoreMidiMessageKind.NoteOn && value.Data2 > 0) >= 5;
            },
            cancellationToken);
        return messages;
    }

    private static async Task PollForAsync(
        CoreMidiNativeApi api,
        TimeSpan duration,
        Func<CoreMidiMessage, bool>? stop,
        CancellationToken cancellationToken)
    {
        var buffer = new CoreMidiMessage[PollCapacity];
        DateTime deadline = duration == Timeout.InfiniteTimeSpan ? DateTime.MaxValue : DateTime.UtcNow + duration;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (api.GetConnectionState() != 1) throw new InvalidOperationException("CoreMIDI endpoint disconnected.");
            int count = api.PollMessages(buffer);
            if (count < 0) throw new InvalidOperationException(api.GetLastError());
            for (int index = 0; index < count; index++)
            {
                PrintMessage(buffer[index]);
                if (stop?.Invoke(buffer[index]) == true) return;
            }
            await Task.Delay(5, cancellationToken);
        }
    }

    private static bool TrySelectDevice(
        CoreMidiNativeApi api,
        int index,
        out CoreMidiDeviceInfo device,
        out string? error)
    {
        device = default;
        error = null;
        int count = api.GetDeviceCount();
        if (index < 0 || index >= count)
        {
            error = count == 0 ? "No CoreMIDI input endpoints." : $"Device index must be between 0 and {count - 1}.";
            return false;
        }
        if (api.GetDeviceInfo(index, out device) != 0)
        {
            error = api.GetLastError();
            return false;
        }
        return true;
    }

    private static void PrintDevice(CoreMidiDeviceInfo info)
    {
        Console.WriteLine(
            $"index={info.Index}\tendpointId={info.EndpointId}\tname={Text(info.Name)}\t" +
            $"manufacturer={Text(info.Manufacturer)}\tmodel={Text(info.Model)}\tonline={(info.IsOnline != 0).ToString().ToLowerInvariant()}");
    }

    private static void PrintMessage(CoreMidiMessage message)
    {
        string kind = Enum.IsDefined(typeof(CoreMidiMessageKind), message.MessageKind)
            ? ((CoreMidiMessageKind)message.MessageKind).ToString()
            : $"Unknown({message.MessageKind})";
        Console.WriteLine(string.Join('\t',
            message.Sequence.ToString(CultureInfo.InvariantCulture),
            message.MonotonicSeconds.ToString("F6", CultureInfo.InvariantCulture),
            kind,
            message.Channel.ToString(CultureInfo.InvariantCulture),
            message.Data1.ToString(CultureInfo.InvariantCulture),
            message.Data2.ToString(CultureInfo.InvariantCulture),
            message.IsNoteOffEquivalent.ToString().ToLowerInvariant()));
    }

    private static void PrintSummary(IReadOnlyList<CoreMidiMessage> messages)
    {
        int strikes = messages.Count(value => value.MessageKind == (int)CoreMidiMessageKind.NoteOn && value.Data2 > 0);
        int noteOffs = messages.Count(value => value.IsNoteOffEquivalent);
        Console.WriteLine($"summary: events={messages.Count} strikes={strikes} noteOffEquivalent={noteOffs}");
        foreach (var group in messages
            .Where(value => value.MessageKind == (int)CoreMidiMessageKind.NoteOn && value.Data2 > 0)
            .GroupBy(value => new { value.Channel, value.Data1 })
            .OrderBy(value => value.Key.Channel).ThenBy(value => value.Key.Data1))
        {
            Console.WriteLine($"observed: channel={group.Key.Channel} data1={group.Key.Data1} count={group.Count()} " +
                $"velocityMin={group.Min(value => value.Data2)} velocityMax={group.Max(value => value.Data2)}");
        }
    }

    private static bool TryOption(string[] args, string name, out int value)
    {
        value = 0;
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length &&
            int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static CancellationTokenSource ConsoleCancellation()
    {
        var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += handler;
        cancellation.Token.Register(() => Console.CancelKeyPress -= handler);
        return cancellation;
    }

    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static int Fail(string? message)
    {
        Console.Error.WriteLine(string.IsNullOrWhiteSpace(message) ? "CoreMIDI operation failed." : message);
        return 2;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  HitTheKit.CoreMidiSmoke doctor");
        Console.Error.WriteLine("  HitTheKit.CoreMidiSmoke list");
        Console.Error.WriteLine("  HitTheKit.CoreMidiSmoke listen --device <index> --seconds <seconds>");
        Console.Error.WriteLine("  HitTheKit.CoreMidiSmoke guided-smoke");
        return 64;
    }
}
