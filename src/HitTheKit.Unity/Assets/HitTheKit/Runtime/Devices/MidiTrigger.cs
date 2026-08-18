using System;

namespace HitTheKit.Unity.Devices
{
    public sealed class MidiTrigger
    {
        public MidiTrigger(
            RawMidiMessageKind kind,
            int? channel,
            int? data1,
            int minimumValue = 0,
            int maximumValue = 127)
        {
            if (!Enum.IsDefined(typeof(RawMidiMessageKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (channel.HasValue && (channel.Value < 0 || channel.Value > 15))
            {
                throw new ArgumentOutOfRangeException(nameof(channel));
            }
            if (data1.HasValue && (data1.Value < 0 || data1.Value > 127))
            {
                throw new ArgumentOutOfRangeException(nameof(data1));
            }
            bool hasNoData1 = kind == RawMidiMessageKind.ChannelAftertouch || kind == RawMidiMessageKind.PitchBend;
            if (hasNoData1 && data1.HasValue)
            {
                throw new ArgumentException("This trigger kind does not have data1.", nameof(data1));
            }
            if (!hasNoData1 && !data1.HasValue)
            {
                throw new ArgumentException("This trigger kind requires data1.", nameof(data1));
            }
            int maximumAllowedValue = kind == RawMidiMessageKind.PitchBend ? 16383 : 127;
            if (minimumValue < 0 || minimumValue > maximumAllowedValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumValue));
            }
            if (maximumValue < 0 || maximumValue > maximumAllowedValue || maximumValue < minimumValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumValue));
            }

            Kind = kind;
            Channel = channel;
            Data1 = data1;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }

        public RawMidiMessageKind Kind { get; }
        public int? Channel { get; }
        public int? Data1 { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }

        public bool Matches(RawMidiMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return message.SemanticKind == Kind &&
                   (!Channel.HasValue || Channel.Value == message.Channel) &&
                   Data1 == message.Data1 &&
                   message.Value >= MinimumValue &&
                   message.Value <= MaximumValue;
        }

        public bool Overlaps(MidiTrigger other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            bool channelOverlaps = !Channel.HasValue || !other.Channel.HasValue || Channel == other.Channel;
            if (Kind != other.Kind || !channelOverlaps || Data1 != other.Data1)
            {
                return false;
            }

            int overlapMinimum = Math.Max(MinimumValue, other.MinimumValue);
            int overlapMaximum = Math.Min(MaximumValue, other.MaximumValue);
            if (Kind == RawMidiMessageKind.NoteOn)
            {
                overlapMinimum = Math.Max(1, overlapMinimum);
            }
            return overlapMinimum <= overlapMaximum;
        }

        internal bool HasSameDefinition(MidiTrigger other)
        {
            return other != null && Kind == other.Kind && Channel == other.Channel && Data1 == other.Data1 &&
                   MinimumValue == other.MinimumValue && MaximumValue == other.MaximumValue;
        }
    }
}
