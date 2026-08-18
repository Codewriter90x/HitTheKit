using System;

namespace HitTheKit.Unity.Devices
{
    public enum RawMidiMessageKind
    {
        NoteOn,
        NoteOff,
        ControlChange,
        PolyAftertouch,
        ChannelAftertouch,
        PitchBend,
        ProgramChange
    }

    public sealed class RawMidiMessage
    {
        private RawMidiMessage(
            RawMidiMessageKind kind,
            int channel,
            int? data1,
            int value,
            double? timestampSeconds)
        {
            if (!Enum.IsDefined(typeof(RawMidiMessageKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            ValidateSevenBit(channel, 15, nameof(channel));
            if (data1.HasValue)
            {
                ValidateSevenBit(data1.Value, 127, nameof(data1));
            }
            ValidateSevenBit(value, kind == RawMidiMessageKind.PitchBend ? 16383 : 127, nameof(value));

            bool hasNoData1 = kind == RawMidiMessageKind.ChannelAftertouch || kind == RawMidiMessageKind.PitchBend;
            if (hasNoData1 && data1.HasValue)
            {
                throw new ArgumentException("This MIDI message kind does not have a data1 value.", nameof(data1));
            }
            if (!hasNoData1 && !data1.HasValue)
            {
                throw new ArgumentException("This MIDI message kind requires data1.", nameof(data1));
            }
            if (timestampSeconds.HasValue && !IsFinite(timestampSeconds.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(timestampSeconds), "Timestamp must be finite.");
            }

            Kind = kind;
            Channel = channel;
            Data1 = data1;
            Value = value;
            TimestampSeconds = timestampSeconds;
        }

        public RawMidiMessageKind Kind { get; }
        public RawMidiMessageKind SemanticKind =>
            Kind == RawMidiMessageKind.NoteOn && Value == 0 ? RawMidiMessageKind.NoteOff : Kind;
        public int Channel { get; }
        public int? Data1 { get; }
        public int Value { get; }
        public double? TimestampSeconds { get; }

        public static RawMidiMessage NoteOn(int channel, int noteNumber, int velocity, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.NoteOn, channel, noteNumber, velocity, timestampSeconds);
        }

        public static RawMidiMessage NoteOff(int channel, int noteNumber, int velocity = 0, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.NoteOff, channel, noteNumber, velocity, timestampSeconds);
        }

        public static RawMidiMessage ControlChange(int channel, int controllerNumber, int value, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.ControlChange, channel, controllerNumber, value, timestampSeconds);
        }

        public static RawMidiMessage PolyAftertouch(int channel, int noteNumber, int pressure, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.PolyAftertouch, channel, noteNumber, pressure, timestampSeconds);
        }

        public static RawMidiMessage ChannelAftertouch(int channel, int pressure, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.ChannelAftertouch, channel, null, pressure, timestampSeconds);
        }

        public static RawMidiMessage PitchBend(int channel, int value, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.PitchBend, channel, null, value, timestampSeconds);
        }

        public static RawMidiMessage ProgramChange(int channel, int program, double? timestampSeconds = null)
        {
            return new RawMidiMessage(RawMidiMessageKind.ProgramChange, channel, program, program, timestampSeconds);
        }

        private static void ValidateSevenBit(int value, int maximum, string parameterName)
        {
            if (value < 0 || value > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, $"Value must be between 0 and {maximum}.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
