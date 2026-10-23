using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Input;

namespace HitTheKit.Unity.Devices
{
    public enum MidiMonitorEntryStatus
    {
        Mapped,
        MappedByFallback,
        Unmapped,
        Disabled,
        Ignored
    }

    public sealed class MidiMonitorEntry
    {
        internal MidiMonitorEntry(
            long sequence,
            string deviceName,
            RawMidiMessage message,
            MidiMonitorEntryStatus status,
            DrumPad? pad,
            string elementId)
        {
            Sequence = sequence;
            DeviceName = deviceName ?? string.Empty;
            Message = message;
            Status = status;
            Pad = pad;
            ElementId = elementId;
        }

        public long Sequence { get; }
        public string DeviceName { get; }
        public RawMidiMessage Message { get; }
        public MidiMonitorEntryStatus Status { get; }
        public DrumPad? Pad { get; }
        public string ElementId { get; }
        public int? Note => Message.Data1;
        public int Velocity => Message.Value;
        public bool NeedsAttention => Status == MidiMonitorEntryStatus.Unmapped;
    }

    /// <summary>
    /// Engine-independent model behind the live MIDI monitor (#52): it records incoming
    /// messages, shows which gameplay pad each one reaches with the active configuration,
    /// highlights unmapped notes and lets the player assign a note to a kit element on the spot.
    /// The caller persists <see cref="Configuration"/> after an assignment.
    /// </summary>
    public sealed class MidiMonitorModel
    {
        public const int DefaultCapacity = 64;
        public const string UserConfigurationId = "user.device-setup";

        private readonly UserKitConfiguration generic;
        private readonly int capacity;
        private readonly LinkedList<MidiMonitorEntry> entries = new LinkedList<MidiMonitorEntry>();
        private readonly MvpDrumInputMapper inputMapper = new MvpDrumInputMapper();
        private GameplayKitMappingResolver resolver;
        private long sequence;

        public MidiMonitorModel(UserKitConfiguration saved, UserKitConfiguration generic, int capacity = DefaultCapacity)
        {
            if (generic == null) throw new ArgumentNullException(nameof(generic));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.generic = generic;
            this.capacity = capacity;
            Configuration = saved;
            resolver = new GameplayKitMappingResolver(saved, generic);
        }

        /// <summary>The player's configuration, or null while only the generic profile is in use.</summary>
        public UserKitConfiguration Configuration { get; private set; }

        /// <summary>Newest first.</summary>
        public IReadOnlyCollection<MidiMonitorEntry> Entries => entries;
        public IReadOnlyCollection<int> UnmappedNotes => resolver.UnmappedNotes;
        public int UnmappedNoteOnCount => resolver.UnmappedNoteOnCount;
        public event Action<MidiMonitorEntry> EntryAdded;
        public event Action<UserKitConfiguration> ConfigurationChanged;

        public MidiMonitorEntry Record(RawMidiMessage message, string deviceName = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            MidiMonitorEntryStatus status;
            DrumPad? pad = null;
            string elementId = null;
            if (message.SemanticKind != RawMidiMessageKind.NoteOn || message.Value <= 0)
            {
                // NoteOff, controllers and aftertouch are shown for diagnostics but never play a pad.
                GameplayKitMappingResolution ignored = resolver.Resolve(message);
                status = MidiMonitorEntryStatus.Ignored;
                if (ignored.IsMapped) elementId = ignored.Result.Hit.ElementId;
            }
            else
            {
                GameplayKitMappingResolution resolution = resolver.Resolve(message);
                if (resolution.IsMapped)
                {
                    elementId = resolution.Result.Hit.ElementId;
                    if (inputMapper.Map(resolution.Result.Hit, true, out DrumInputEvent input) == MvpDrumInputMappingStatus.Mapped)
                        pad = input.Pad;
                    status = resolution.Origin == GameplayKitMappingOrigin.Fallback
                        ? MidiMonitorEntryStatus.MappedByFallback
                        : MidiMonitorEntryStatus.Mapped;
                    if (!pad.HasValue) status = MidiMonitorEntryStatus.Ignored;
                }
                else
                {
                    status = resolution.Result.Status == MidiKitMappingStatus.Disabled
                        ? MidiMonitorEntryStatus.Disabled
                        : MidiMonitorEntryStatus.Unmapped;
                }
            }

            var entry = new MidiMonitorEntry(++sequence, deviceName, message, status, pad, elementId);
            entries.AddFirst(entry);
            while (entries.Count > capacity) entries.RemoveLast();
            EntryAdded?.Invoke(entry);
            return entry;
        }

        public void Clear() => entries.Clear();

        /// <summary>
        /// Kit elements a note can be assigned to: the player's elements plus generic ones they lack.
        /// </summary>
        public IReadOnlyList<KitElement> AssignableElements()
        {
            var result = new List<KitElement>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (Configuration != null)
                foreach (KitElement element in Configuration.Elements)
                    if (seen.Add(element.Id)) result.Add(element);
            foreach (KitElement element in generic.Elements)
                if (seen.Add(element.Id)) result.Add(element);
            return result.AsReadOnly();
        }

        /// <summary>
        /// Routes every NoteOn with this note number (any channel) to the given element through a
        /// user override, replacing a previous override for the same note. Returns the new configuration.
        /// </summary>
        public UserKitConfiguration AssignNote(int note, string elementId)
        {
            if (note < 0 || note > 127) throw new ArgumentOutOfRangeException(nameof(note));
            KitElement.EnsureStableId(elementId, nameof(elementId));

            KitElement target = null;
            foreach (KitElement element in AssignableElements())
                if (string.Equals(element.Id, elementId, StringComparison.Ordinal)) target = element;
            if (target == null) throw new ArgumentException($"Unknown kit element '{elementId}'.", nameof(elementId));

            UserKitConfiguration source = Configuration ?? generic;
            string mappingId = OverrideMappingId(note);

            var elements = new List<KitElement>(source.Elements);
            bool hasTarget = false;
            foreach (KitElement element in elements)
                if (string.Equals(element.Id, target.Id, StringComparison.Ordinal)) hasTarget = true;
            if (!hasTarget) elements.Add(target);

            var mappings = new List<MidiMappingEntry>();
            foreach (MidiMappingEntry mapping in source.Mappings)
                if (!string.Equals(mapping.Id, mappingId, StringComparison.Ordinal)) mappings.Add(mapping);
            mappings.Add(new MidiMappingEntry(
                mappingId,
                new MidiTrigger(RawMidiMessageKind.NoteOn, null, note, 1, 127),
                target.Id,
                source: MidiMappingSource.UserOverride));

            var disabled = new List<string>();
            foreach (string id in source.DisabledElementIds)
                if (!string.Equals(id, target.Id, StringComparison.Ordinal)) disabled.Add(id);

            bool fromGeneric = Configuration == null;
            var updated = new UserKitConfiguration(
                UserKitConfigurationLoader.SupportedSchemaVersion,
                fromGeneric ? UserConfigurationId : source.ConfigurationId,
                fromGeneric ? "My drum kit" : source.DisplayName,
                source.BaseProfileId,
                source.BaseProfileVersion,
                source.MidiDeviceIdentity,
                elements,
                mappings,
                disabled,
                source.UserNotes);

            Configuration = updated;
            resolver = new GameplayKitMappingResolver(updated, generic);
            ConfigurationChanged?.Invoke(updated);
            return updated;
        }

        public static string OverrideMappingId(int note) => "user.note." + note.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
