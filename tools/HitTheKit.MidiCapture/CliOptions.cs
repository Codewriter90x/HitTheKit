using System.Globalization;

namespace HitTheKit.MidiCapture;

public enum CaptureCommand
{
    Help,
    Doctor,
    List,
    Listen,
    Capture,
    GuidedCapture,
    Summarize,
    Verify,
    Pack,
    Replay
}

public sealed record CliOptions(
    CaptureCommand Command,
    int? DeviceIndex = null,
    string? DeviceName = null,
    string? OutputPath = null,
    string? InputPath = null,
    string? FixturePath = null,
    string? Label = null,
    string? Notes = null,
    double DurationSeconds = 60,
    int Samples = 5,
    bool ShowAll = false);

public static class CliParser
{
    public static bool TryParse(string[] args, out CliOptions? options, out string? error)
    {
        options = null;
        error = null;
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            options = new(CaptureCommand.Help);
            return true;
        }

        string commandText = args[0].Replace("-", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse(commandText, true, out CaptureCommand command) || command == CaptureCommand.Help)
        {
            error = $"Unknown command '{args[0]}'.";
            return false;
        }

        int? device = null;
        string? deviceName = null;
        string? output = null;
        string? input = null;
        string? fixture = null;
        string? label = null;
        string? notes = null;
        double duration = command switch
        {
            CaptureCommand.Capture => 60,
            CaptureCommand.GuidedCapture => 20,
            _ => 0
        };
        int samples = 5;
        bool showAll = false;

        for (int index = 1; index < args.Length; index++)
        {
            string argument = args[index];
            if (argument == "--all") { showAll = true; continue; }
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                if (command is CaptureCommand.Summarize or CaptureCommand.Verify or CaptureCommand.Pack && input is null)
                { input = argument; continue; }
                if (command == CaptureCommand.Replay && fixture is null)
                { fixture = argument; continue; }
                error = $"Unexpected positional argument '{argument}'.";
                return false;
            }
            if (!TryReadValue(args, ref index, argument, out string? value, out error)) return false;
            switch (argument)
            {
                case "--device":
                case "--device-index":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 0)
                    { error = $"{argument} must be a non-negative integer."; return false; }
                    device = parsed;
                    break;
                case "--device-name": deviceName = value; break;
                case "--output": output = value; break;
                case "--input": input = value; break;
                case "--fixture": fixture = value; break;
                case "--label": label = value; break;
                case "--notes": notes = value; break;
                case "--duration":
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out duration) ||
                        !double.IsFinite(duration) || duration <= 0)
                    { error = "--duration must be a positive finite number."; return false; }
                    break;
                case "--samples":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out samples) || samples <= 0)
                    { error = "--samples must be a positive integer."; return false; }
                    break;
                default: error = $"Unknown option '{argument}'."; return false;
            }
        }

        if (device.HasValue && deviceName is not null) { error = "Select a device by index or name, not both."; return false; }
        if (command is CaptureCommand.Listen or CaptureCommand.Capture or CaptureCommand.GuidedCapture && !device.HasValue && string.IsNullOrWhiteSpace(deviceName))
        { error = $"{args[0]} requires --device <index> or --device-name <name>."; return false; }
        if (command is CaptureCommand.Summarize or CaptureCommand.Verify or CaptureCommand.Pack && string.IsNullOrWhiteSpace(input))
        { error = $"{args[0]} requires --input <bundle-directory>."; return false; }
        if (command == CaptureCommand.Replay && string.IsNullOrWhiteSpace(fixture))
        { error = "replay requires --fixture <path>."; return false; }

        options = new(command, device, deviceName, output, input, fixture, label, notes, duration, samples, showAll);
        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, string option, out string? value, out string? error)
    {
        value = null;
        error = null;
        if (!option.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
        { error = $"Option '{option}' requires a value."; return false; }
        value = args[++index];
        return true;
    }
}
