using System;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class MidiMonitorModelTests
    {
        [Test]
        public void Shows_which_pad_a_note_reaches()
        {
            var monitor = new MidiMonitorModel(null, Generic());

            MidiMonitorEntry entry = monitor.Record(RawMidiMessage.NoteOn(9, 38, 100, 1.0), "eDrum");

            Assert.That(entry.Status, Is.EqualTo(MidiMonitorEntryStatus.Mapped));
            Assert.That(entry.Pad, Is.EqualTo(DrumPad.Snare));
            Assert.That(entry.Note, Is.EqualTo(38));
            Assert.That(entry.Velocity, Is.EqualTo(100));
            Assert.That(entry.DeviceName, Is.EqualTo("eDrum"));
        }

        [Test]
        public void Highlights_unmapped_notes()
        {
            var monitor = new MidiMonitorModel(null, Generic());

            MidiMonitorEntry entry = monitor.Record(RawMidiMessage.NoteOn(9, 26, 90, 1.0));

            Assert.That(entry.Status, Is.EqualTo(MidiMonitorEntryStatus.Unmapped));
            Assert.That(entry.NeedsAttention, Is.True);
            Assert.That(entry.Pad, Is.Null);
            Assert.That(monitor.UnmappedNotes, Is.EqualTo(new[] { 26 }));
        }

        [Test]
        public void Note_offs_and_controllers_are_listed_but_never_play_a_pad()
        {
            var monitor = new MidiMonitorModel(null, Generic());

            Assert.That(monitor.Record(RawMidiMessage.NoteOff(9, 38, 0, 1.0)).Status, Is.EqualTo(MidiMonitorEntryStatus.Ignored));
            Assert.That(monitor.Record(RawMidiMessage.ControlChange(9, 4, 64, 1.1)).Status, Is.EqualTo(MidiMonitorEntryStatus.Ignored));
            Assert.That(monitor.Record(RawMidiMessage.NoteOn(9, 38, 0, 1.2)).Status, Is.EqualTo(MidiMonitorEntryStatus.Ignored));
            Assert.That(monitor.UnmappedNoteOnCount, Is.Zero);
        }

        [Test]
        public void Notes_missing_from_the_saved_configuration_are_marked_as_fallback()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[] { new KitElement("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare") },
                new[] { Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture) });
            var monitor = new MidiMonitorModel(saved, Generic());

            MidiMonitorEntry entry = monitor.Record(RawMidiMessage.NoteOn(9, 36, 90, 1.0));

            Assert.That(entry.Status, Is.EqualTo(MidiMonitorEntryStatus.MappedByFallback));
            Assert.That(entry.Pad, Is.EqualTo(DrumPad.Kick));
        }

        [Test]
        public void Keeps_the_newest_entries_up_to_capacity()
        {
            var monitor = new MidiMonitorModel(null, Generic(), capacity: 3);
            for (int index = 0; index < 5; index++) monitor.Record(RawMidiMessage.NoteOn(9, 36, 60 + index, index));

            Assert.That(monitor.Entries.Select(entry => entry.Velocity).ToArray(), Is.EqualTo(new[] { 64, 63, 62 }));
            Assert.That(monitor.Entries.First().Sequence, Is.EqualTo(5));
            monitor.Clear();
            Assert.That(monitor.Entries, Has.Count.EqualTo(0));
        }

        [Test]
        public void Assigning_an_unmapped_note_creates_a_user_configuration_from_the_generic_profile()
        {
            var monitor = new MidiMonitorModel(null, Generic());
            UserKitConfiguration changed = null;
            monitor.ConfigurationChanged += configuration => changed = configuration;
            monitor.Record(RawMidiMessage.NoteOn(9, 26, 90, 1.0));

            UserKitConfiguration updated = monitor.AssignNote(26, "crash1.bow");

            Assert.That(changed, Is.SameAs(updated));
            Assert.That(updated.ConfigurationId, Is.EqualTo(MidiMonitorModel.UserConfigurationId));
            Assert.That(updated.IsComplete, Is.True);
            MidiMonitorEntry entry = monitor.Record(RawMidiMessage.NoteOn(9, 26, 90, 2.0));
            Assert.That(entry.Status, Is.EqualTo(MidiMonitorEntryStatus.Mapped));
            Assert.That(entry.Pad, Is.EqualTo(DrumPad.Crash));
            Assert.That(entry.ElementId, Is.EqualTo("crash1.bow"));
        }

        [Test]
        public void Reassigning_a_mapped_note_overrides_the_profile_and_replaces_earlier_overrides()
        {
            var monitor = new MidiMonitorModel(null, Generic());

            monitor.AssignNote(38, "kick.default");
            UserKitConfiguration updated = monitor.AssignNote(38, "crash1.bow");

            Assert.That(updated.Mappings.Count(mapping => mapping.Id == MidiMonitorModel.OverrideMappingId(38)), Is.EqualTo(1));
            Assert.That(monitor.Record(RawMidiMessage.NoteOn(9, 38, 100, 1.0)).Pad, Is.EqualTo(DrumPad.Crash));
            Assert.That(monitor.Record(RawMidiMessage.NoteOn(9, 36, 100, 1.1)).Pad, Is.EqualTo(DrumPad.Kick));
        }

        [Test]
        public void Assigning_to_an_element_missing_from_the_saved_configuration_adds_it()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[] { new KitElement("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare") },
                new[] { Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture) });
            var monitor = new MidiMonitorModel(saved, Generic());

            UserKitConfiguration updated = monitor.AssignNote(59, "crash1.bow");

            Assert.That(updated.ConfigurationId, Is.EqualTo("test.saved"));
            Assert.That(updated.Elements.Select(element => element.Id).ToArray(), Is.EqualTo(new[] { "snare.head", "crash1.bow" }));
            Assert.That(monitor.Record(RawMidiMessage.NoteOn(9, 59, 100, 1.0)).Status, Is.EqualTo(MidiMonitorEntryStatus.Mapped));
        }

        [Test]
        public void Assignable_elements_combine_saved_and_generic_elements_without_duplicates()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[] { new KitElement("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare") },
                new[] { Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture) });
            var monitor = new MidiMonitorModel(saved, Generic());

            Assert.That(monitor.AssignableElements().Select(element => element.Id).ToArray(),
                Is.EqualTo(new[] { "snare.head", "kick.default", "crash1.bow" }));
        }

        [Test]
        public void Rejects_invalid_assignments()
        {
            var monitor = new MidiMonitorModel(null, Generic());
            Assert.Throws<ArgumentOutOfRangeException>(() => monitor.AssignNote(128, "kick.default"));
            Assert.Throws<ArgumentException>(() => monitor.AssignNote(26, "ride.bell"));
            Assert.Throws<ArgumentNullException>(() => monitor.Record(null));
            Assert.Throws<ArgumentNullException>(() => new MidiMonitorModel(null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiMonitorModel(null, Generic(), 0));
        }

        private static UserKitConfiguration Generic() => Configuration("generic",
            new[]
            {
                new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick"),
                new KitElement("snare.head", KitPiece.Snare, KitArticulation.Head, "Snare"),
                new KitElement("crash1.bow", KitPiece.Crash1, KitArticulation.Bow, "Crash", isOptional: true)
            },
            new[]
            {
                Mapping("gm.note.36", "kick.default", 36, MidiMappingSource.BuiltInProfile),
                Mapping("gm.note.38", "snare.head", 38, MidiMappingSource.BuiltInProfile),
                Mapping("gm.note.49", "crash1.bow", 49, MidiMappingSource.BuiltInProfile)
            });

        private static MidiMappingEntry Mapping(string id, string elementId, int note, MidiMappingSource source) =>
            new MidiMappingEntry(id, new MidiTrigger(RawMidiMessageKind.NoteOn, null, note, 1, 127), elementId, source: source);

        private static UserKitConfiguration Configuration(string id, KitElement[] elements, MidiMappingEntry[] mappings) =>
            new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "test." + id, id, null, null, null,
                elements, mappings, Array.Empty<string>());
    }
}
