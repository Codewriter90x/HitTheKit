using HitTheKit.Unity.Devices;
using Melanchall.DryWetMidi.Core;

namespace HitTheKit.MidiCapture;

public sealed record AdaptedMidiEvent(CaptureEvent Capture, RawMidiMessage? FoundationMessage);

public static class MidiEventAdapter
{
    private const int SysExPreviewLimit = 64;

    public static AdaptedMidiEvent Adapt(MidiEvent midiEvent, long sequence, double elapsedSeconds, string? stepId = null, int? stepAttempt = null)
    {
        ArgumentNullException.ThrowIfNull(midiEvent);
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));

        return midiEvent switch
        {
            NoteOnEvent value => Foundation(
                sequence,
                elapsedSeconds,
                stepId,
                RawMidiMessage.NoteOn((int)value.Channel, (int)value.NoteNumber, (int)value.Velocity, elapsedSeconds),
                (int)value.Velocity == 0,
                stepAttempt),
            NoteOffEvent value => Foundation(
                sequence,
                elapsedSeconds,
                stepId,
                RawMidiMessage.NoteOff((int)value.Channel, (int)value.NoteNumber, (int)value.Velocity, elapsedSeconds),
                stepAttempt: stepAttempt),
            ControlChangeEvent value => Foundation(
                sequence,
                elapsedSeconds,
                stepId,
                RawMidiMessage.ControlChange((int)value.Channel, (int)value.ControlNumber, (int)value.ControlValue, elapsedSeconds),
                stepAttempt: stepAttempt),
            NoteAftertouchEvent value => Foundation(
                sequence,
                elapsedSeconds,
                stepId,
                RawMidiMessage.PolyAftertouch((int)value.Channel, (int)value.NoteNumber, (int)value.AftertouchValue, elapsedSeconds),
                stepAttempt: stepAttempt),
            ChannelAftertouchEvent value => Foundation(
                sequence,
                elapsedSeconds,
                stepId,
                RawMidiMessage.ChannelAftertouch((int)value.Channel, (int)value.AftertouchValue, elapsedSeconds),
                stepAttempt: stepAttempt),
            PitchBendEvent value => Raw(value, sequence, elapsedSeconds, stepId, "pitchBend", null, (int)value.PitchValue, stepAttempt),
            ProgramChangeEvent value => Raw(value, sequence, elapsedSeconds, stepId, "programChange", (int)value.ProgramNumber, null, stepAttempt),
            SysExEvent value => SysEx(value, sequence, elapsedSeconds, stepId, stepAttempt),
            _ => Raw(midiEvent, sequence, elapsedSeconds, stepId, ToCamelCase(midiEvent.EventType.ToString()), null, null, stepAttempt)
        };
    }

    private static AdaptedMidiEvent Foundation(
        long sequence,
        double elapsed,
        string? stepId,
        RawMidiMessage foundation,
        bool noteOffEquivalent = false,
        int? stepAttempt = null)
    {
        string kind = foundation.Kind switch
        {
            RawMidiMessageKind.NoteOn => "noteOn",
            RawMidiMessageKind.NoteOff => "noteOff",
            RawMidiMessageKind.ControlChange => "controlChange",
            RawMidiMessageKind.PolyAftertouch => "polyAftertouch",
            RawMidiMessageKind.ChannelAftertouch => "channelAftertouch",
            _ => throw new ArgumentOutOfRangeException(nameof(foundation))
        };
        var capture = new CaptureEvent(
            CaptureEvent.CurrentSchemaVersion,
            sequence,
            elapsed,
            stepId,
            kind,
            foundation.Channel,
            foundation.Data1,
            foundation.Value,
            noteOffEquivalent,
            true,
            Description: Describe(kind, foundation.Channel, foundation.Data1, foundation.Value, noteOffEquivalent),
            StepAttempt: stepAttempt);
        return new(capture, foundation);
    }

    private static AdaptedMidiEvent Raw(
        MidiEvent midiEvent,
        long sequence,
        double elapsed,
        string? stepId,
        string kind,
        int? data1,
        int? data2,
        int? stepAttempt)
    {
        int? channel = midiEvent is ChannelEvent channelEvent ? (int)channelEvent.Channel : null;
        var capture = new CaptureEvent(
            CaptureEvent.CurrentSchemaVersion,
            sequence,
            elapsed,
            stepId,
            kind,
            channel,
            data1,
            data2,
            Description: Describe(kind, channel, data1, data2, false),
            StepAttempt: stepAttempt);
        return new(capture, null);
    }

    private static AdaptedMidiEvent SysEx(SysExEvent midiEvent, long sequence, double elapsed, string? stepId, int? stepAttempt)
    {
        byte[] data = midiEvent.Data ?? Array.Empty<byte>();
        byte[] preview = data.Take(SysExPreviewLimit).ToArray();
        var capture = new CaptureEvent(
            CaptureEvent.CurrentSchemaVersion,
            sequence,
            elapsed,
            stepId,
            midiEvent is NormalSysExEvent ? "normalSysEx" : "escapeSysEx",
            null,
            null,
            null,
            UnknownHexPrefix: Convert.ToHexString(preview).ToLowerInvariant(),
            UnknownLength: data.Length,
            IsTruncated: data.Length > SysExPreviewLimit,
            Description: $"SysEx length={data.Length}{(data.Length > SysExPreviewLimit ? " truncated" : string.Empty)}",
            StepAttempt: stepAttempt);
        return new(capture, null);
    }

    public static string FormatCompact(CaptureEvent value)
    {
        string channel = value.Channel.HasValue ? $" ch{value.Channel.Value + 1}" : string.Empty;
        string data1 = value.Data1.HasValue ? $" data1={value.Data1.Value}" : string.Empty;
        string data2 = value.Data2.HasValue ? $" value={value.Data2.Value}" : string.Empty;
        string semantic = value.IsNoteOffEquivalent ? " (NoteOff equivalent)" : string.Empty;
        return $"{value.ElapsedSeconds,8:0.000} #{value.Sequence} {value.RawKind}{channel}{data1}{data2}{semantic}";
    }

    private static string Describe(string kind, int? channel, int? data1, int? data2, bool noteOffEquivalent)
    {
        string semantic = noteOffEquivalent ? "; semantic=noteOff" : string.Empty;
        return $"{kind}; channel={(channel.HasValue ? channel.Value + 1 : null)}; data1={data1}; value={data2}{semantic}";
    }

    private static string ToCamelCase(string value) =>
        value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
