using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Devices
{
    public enum GameplayKitMappingOrigin
    {
        None,
        Primary,
        Fallback
    }

    public sealed class GameplayKitMappingResolution
    {
        internal GameplayKitMappingResolution(MidiKitMappingResult result, GameplayKitMappingOrigin origin)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Origin = origin;
        }

        public MidiKitMappingResult Result { get; }
        public GameplayKitMappingOrigin Origin { get; }
        public bool IsMapped => Result.Status == MidiKitMappingStatus.Mapped;
    }

    /// <summary>
    /// Resolves gameplay MIDI messages against the player's saved kit configuration first and
    /// the built-in generic profile second, so a wrong or partial saved mapping cannot make
    /// pads silently stop working. Explicit user decisions (disabled mappings or elements) are
    /// respected and never overridden by the fallback.
    /// </summary>
    public sealed class GameplayKitMappingResolver
    {
        public const int MaximumTrackedUnmappedNotes = 32;

        private readonly MidiKitMappingEngine engine = new MidiKitMappingEngine();
        private readonly UserKitConfiguration primary;
        private readonly UserKitConfiguration fallback;
        private readonly SortedSet<int> unmappedNotes = new SortedSet<int>();

        public GameplayKitMappingResolver(UserKitConfiguration primary, UserKitConfiguration fallback)
        {
            if (primary == null && fallback == null)
                throw new ArgumentException("At least one kit configuration is required.");
            if (primary != null && !primary.IsComplete)
                throw new ArgumentException("The primary configuration must be complete.", nameof(primary));
            if (fallback != null && !fallback.IsComplete)
                throw new ArgumentException("The fallback configuration must be complete.", nameof(fallback));

            this.primary = primary ?? fallback;
            this.fallback = primary == null || ReferenceEquals(primary, fallback) ? null : fallback;
        }

        public bool HasFallback => fallback != null;
        public int FallbackHitCount { get; private set; }
        public int UnmappedNoteOnCount { get; private set; }
        public IReadOnlyCollection<int> UnmappedNotes => unmappedNotes;

        public GameplayKitMappingResolution Resolve(RawMidiMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            MidiKitMappingResult result = engine.Map(message, primary);
            if (result.Status == MidiKitMappingStatus.Mapped)
                return new GameplayKitMappingResolution(result, GameplayKitMappingOrigin.Primary);

            if (fallback != null && result.Status == MidiKitMappingStatus.NoMatch)
            {
                MidiKitMappingResult fallbackResult = engine.Map(message, fallback);
                if (fallbackResult.Status == MidiKitMappingStatus.Mapped)
                {
                    if (IsPositiveNoteOn(message)) FallbackHitCount++;
                    return new GameplayKitMappingResolution(fallbackResult, GameplayKitMappingOrigin.Fallback);
                }
            }

            if (result.Status == MidiKitMappingStatus.NoMatch && IsPositiveNoteOn(message))
            {
                UnmappedNoteOnCount++;
                if (message.Data1.HasValue &&
                    (unmappedNotes.Count < MaximumTrackedUnmappedNotes || unmappedNotes.Contains(message.Data1.Value)))
                {
                    unmappedNotes.Add(message.Data1.Value);
                }
            }

            return new GameplayKitMappingResolution(result, GameplayKitMappingOrigin.None);
        }

        private static bool IsPositiveNoteOn(RawMidiMessage message) =>
            message.SemanticKind == RawMidiMessageKind.NoteOn && message.Value > 0;
    }
}
