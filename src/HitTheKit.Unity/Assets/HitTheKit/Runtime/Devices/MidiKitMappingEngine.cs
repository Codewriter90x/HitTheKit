using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Devices
{
    public enum MidiKitMappingStatus
    {
        Mapped,
        NoMatch,
        Ambiguous,
        Disabled,
        Invalid
    }

    public sealed class MidiKitMappingResult
    {
        internal MidiKitMappingResult(MidiKitMappingStatus status, NormalizedKitHit hit, string message)
        {
            Status = status;
            Hit = hit;
            Message = message;
        }

        public MidiKitMappingStatus Status { get; }
        public NormalizedKitHit Hit { get; }
        public string Message { get; }
    }

    public sealed class NormalizedKitHit
    {
        public NormalizedKitHit(
            string elementId,
            KitPiece piece,
            KitArticulation articulation,
            int velocity,
            RawMidiMessage originalMessage,
            string sourceMappingId,
            MidiMappingSource source)
        {
            KitElement.EnsureStableId(elementId, nameof(elementId));
            KitElementDefinitionValidator.EnsureValid(piece, articulation);
            if (velocity < 0 || velocity > 127) throw new ArgumentOutOfRangeException(nameof(velocity));
            KitElement.EnsureStableId(sourceMappingId, nameof(sourceMappingId));
            if (!Enum.IsDefined(typeof(MidiMappingSource), source))
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            ElementId = elementId;
            Piece = piece;
            Articulation = articulation;
            Velocity = velocity;
            OriginalMessage = originalMessage ?? throw new ArgumentNullException(nameof(originalMessage));
            TimestampSeconds = originalMessage.TimestampSeconds;
            SourceMappingId = sourceMappingId;
            Source = source;
        }

        public string ElementId { get; }
        public KitPiece Piece { get; }
        public KitArticulation Articulation { get; }
        public int Velocity { get; }
        public RawMidiMessage OriginalMessage { get; }
        public double? TimestampSeconds { get; }
        public string SourceMappingId { get; }
        public MidiMappingSource Source { get; }
    }

    public sealed class MidiKitMappingEngine
    {
        public bool TryMap(
            RawMidiMessage message,
            UserKitConfiguration configuration,
            out NormalizedKitHit hit)
        {
            MidiKitMappingResult result = Map(message, configuration);
            hit = result.Hit;
            return result.Status == MidiKitMappingStatus.Mapped;
        }

        public MidiKitMappingResult Map(RawMidiMessage message, UserKitConfiguration configuration)
        {
            if (message == null || configuration == null)
            {
                return Result(MidiKitMappingStatus.Invalid, "Message and configuration are required.");
            }
            if (!configuration.IsComplete)
            {
                return Result(MidiKitMappingStatus.Invalid, "Incomplete draft configurations cannot be used for runtime mapping.");
            }

            var candidates = new List<MidiMappingEntry>();
            for (int index = 0; index < configuration.Mappings.Count; index++)
            {
                MidiMappingEntry mapping = configuration.Mappings[index];
                if (mapping.Trigger.Matches(message)) candidates.Add(mapping);
            }
            if (candidates.Count == 0) return Result(MidiKitMappingStatus.NoMatch, "No trigger matched the message.");

            candidates.Sort(CompareMappings);
            int bestSource = SourceRank(candidates[0].Source);
            int bestPriority = candidates[0].Priority;
            var bestEnabled = new List<MidiMappingEntry>();
            bool bestWasDisabled = false;
            foreach (MidiMappingEntry candidate in candidates)
            {
                if (SourceRank(candidate.Source) != bestSource || candidate.Priority != bestPriority) break;
                if (candidate.Enabled) bestEnabled.Add(candidate); else bestWasDisabled = true;
            }
            if (bestEnabled.Count == 0 && bestWasDisabled)
            {
                return Result(MidiKitMappingStatus.Disabled, "The highest-precedence mapping is disabled.");
            }

            MidiMappingEntry selected = bestEnabled[0];
            for (int index = 1; index < bestEnabled.Count; index++)
            {
                if (!string.Equals(selected.ElementId, bestEnabled[index].ElementId, StringComparison.Ordinal))
                {
                    return Result(MidiKitMappingStatus.Ambiguous, "Multiple equally-ranked mappings target different elements.");
                }
            }

            KitElement element = FindElement(configuration.Elements, selected.ElementId);
            if (element == null) return Result(MidiKitMappingStatus.Invalid, "The selected mapping targets a missing element.");
            if (configuration.IsElementDisabled(element.Id))
            {
                return Result(MidiKitMappingStatus.Disabled, "The mapped kit element is disabled.");
            }

            int velocity = message.SemanticKind == RawMidiMessageKind.NoteOff ? 0 : message.Value;
            return new MidiKitMappingResult(
                MidiKitMappingStatus.Mapped,
                new NormalizedKitHit(
                    element.Id,
                    element.Piece,
                    element.Articulation,
                    velocity,
                    message,
                    selected.Id,
                    selected.Source),
                null);
        }

        private static int CompareMappings(MidiMappingEntry left, MidiMappingEntry right)
        {
            int bySource = SourceRank(right.Source).CompareTo(SourceRank(left.Source));
            if (bySource != 0) return bySource;
            int byPriority = right.Priority.CompareTo(left.Priority);
            return byPriority != 0 ? byPriority : string.CompareOrdinal(left.Id, right.Id);
        }

        private static int SourceRank(MidiMappingSource source)
        {
            switch (source)
            {
                case MidiMappingSource.UserOverride: return 3;
                case MidiMappingSource.WizardCapture: return 2;
                case MidiMappingSource.BuiltInProfile: return 1;
                default: return 0;
            }
        }

        private static KitElement FindElement(IReadOnlyList<KitElement> elements, string id)
        {
            for (int index = 0; index < elements.Count; index++)
            {
                if (string.Equals(elements[index].Id, id, StringComparison.Ordinal)) return elements[index];
            }
            return null;
        }

        private static MidiKitMappingResult Result(MidiKitMappingStatus status, string message)
        {
            return new MidiKitMappingResult(status, null, message);
        }
    }

    public enum MvpDrumInputMappingStatus
    {
        Mapped,
        UnsupportedInCurrentGameplay,
        Disabled,
        Invalid
    }

    public sealed class MvpDrumInputMapper
    {
        public MvpDrumInputMappingStatus Map(
            NormalizedKitHit hit,
            bool enabled,
            out DrumInputEvent drumInputEvent)
        {
            drumInputEvent = default;
            if (hit == null || !hit.TimestampSeconds.HasValue)
            {
                return MvpDrumInputMappingStatus.Invalid;
            }
            if (!enabled) return MvpDrumInputMappingStatus.Disabled;

            if (hit.Piece == KitPiece.HiHat && hit.Articulation == KitArticulation.Choke)
            {
                return MvpDrumInputMappingStatus.UnsupportedInCurrentGameplay;
            }
            if (hit.OriginalMessage.SemanticKind != RawMidiMessageKind.NoteOn || hit.Velocity <= 0)
            {
                return MvpDrumInputMappingStatus.UnsupportedInCurrentGameplay;
            }

            DrumPad pad;
            switch (hit.Piece)
            {
                case KitPiece.Kick: pad = DrumPad.Kick; break;
                case KitPiece.Snare: pad = DrumPad.Snare; break;
                case KitPiece.HiHat: pad = DrumPad.HiHat; break;
                case KitPiece.Tom1: pad = DrumPad.Tom1; break;
                case KitPiece.Tom2:
                case KitPiece.Tom3:
                case KitPiece.Tom4: pad = DrumPad.Tom2; break;
                case KitPiece.FloorTom: pad = DrumPad.FloorTom; break;
                case KitPiece.Crash1:
                case KitPiece.Crash2:
                case KitPiece.Crash3: pad = DrumPad.Crash; break;
                case KitPiece.Ride: pad = DrumPad.Ride; break;
                default:
                    return MvpDrumInputMappingStatus.Invalid;
            }

            drumInputEvent = new DrumInputEvent(
                pad,
                hit.Velocity,
                hit.TimestampSeconds.Value,
                DrumInputSource.Midi,
                ToDrumArticulation(hit.Articulation));
            return MvpDrumInputMappingStatus.Mapped;
        }

        private static DrumArticulation ToDrumArticulation(KitArticulation articulation)
        {
            switch (articulation)
            {
                case KitArticulation.Default: return DrumArticulation.Default;
                case KitArticulation.Head: return DrumArticulation.Head;
                case KitArticulation.Rim: return DrumArticulation.Rim;
                case KitArticulation.Bow: return DrumArticulation.Bow;
                case KitArticulation.Edge: return DrumArticulation.Edge;
                case KitArticulation.Bell: return DrumArticulation.Bell;
                case KitArticulation.Closed: return DrumArticulation.Closed;
                case KitArticulation.HalfOpen: return DrumArticulation.HalfOpen;
                case KitArticulation.Open: return DrumArticulation.Open;
                case KitArticulation.Pedal: return DrumArticulation.Pedal;
                case KitArticulation.Choke: return DrumArticulation.Choke;
                default: throw new ArgumentOutOfRangeException(nameof(articulation));
            }
        }
    }
}
