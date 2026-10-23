using System;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayKitMappingResolverTests
    {
        [Test]
        public void Saved_mapping_wins_over_generic_mapping()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[] { Element("snare.head", KitPiece.Snare, KitArticulation.Head) },
                new[] { Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture) });
            var resolver = new GameplayKitMappingResolver(saved, Generic());

            GameplayKitMappingResolution resolution = resolver.Resolve(RawMidiMessage.NoteOn(9, 38, 100, 1.0));

            Assert.That(resolution.IsMapped, Is.True);
            Assert.That(resolution.Origin, Is.EqualTo(GameplayKitMappingOrigin.Primary));
            Assert.That(resolution.Result.Hit.SourceMappingId, Is.EqualTo("saved.38"));
            Assert.That(resolver.FallbackHitCount, Is.Zero);
        }

        [Test]
        public void Notes_unknown_to_the_saved_configuration_fall_back_to_the_generic_profile()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[] { Element("snare.head", KitPiece.Snare, KitArticulation.Head) },
                new[] { Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture) });
            var resolver = new GameplayKitMappingResolver(saved, Generic());

            GameplayKitMappingResolution resolution = resolver.Resolve(RawMidiMessage.NoteOn(9, 36, 90, 1.0));

            Assert.That(resolution.IsMapped, Is.True);
            Assert.That(resolution.Origin, Is.EqualTo(GameplayKitMappingOrigin.Fallback));
            Assert.That(resolution.Result.Hit.Piece, Is.EqualTo(KitPiece.Kick));
            Assert.That(resolver.FallbackHitCount, Is.EqualTo(1));
        }

        [Test]
        public void A_wrong_saved_mapping_does_not_block_the_kit()
        {
            // The guided setup captured the kick on a note the module never sends again.
            UserKitConfiguration saved = Configuration("saved",
                new[] { Element("kick.default", KitPiece.Kick, KitArticulation.Default) },
                new[] { Mapping("saved.kick", "kick.default", 99, MidiMappingSource.WizardCapture) });
            var resolver = new GameplayKitMappingResolver(saved, Generic());

            Assert.That(resolver.Resolve(RawMidiMessage.NoteOn(9, 36, 90, 1.0)).IsMapped, Is.True);
            Assert.That(resolver.Resolve(RawMidiMessage.NoteOn(9, 38, 90, 1.1)).IsMapped, Is.True);
            Assert.That(resolver.UnmappedNoteOnCount, Is.Zero);
        }

        [Test]
        public void Disabled_saved_mappings_are_respected_and_not_overridden_by_the_fallback()
        {
            UserKitConfiguration saved = Configuration("saved",
                new[]
                {
                    Element("snare.head", KitPiece.Snare, KitArticulation.Head),
                    new KitElement("kick.default", KitPiece.Kick, KitArticulation.Default, "Kick", isOptional: true)
                },
                new[]
                {
                    Mapping("saved.38", "snare.head", 38, MidiMappingSource.WizardCapture),
                    Mapping("saved.kick", "kick.default", 36, MidiMappingSource.UserOverride, enabled: false)
                });
            var resolver = new GameplayKitMappingResolver(saved, Generic());

            GameplayKitMappingResolution resolution = resolver.Resolve(RawMidiMessage.NoteOn(9, 36, 90, 1.0));

            Assert.That(resolution.IsMapped, Is.False);
            Assert.That(resolution.Result.Status, Is.EqualTo(MidiKitMappingStatus.Disabled));
            Assert.That(resolver.FallbackHitCount, Is.Zero);
        }

        [Test]
        public void Unmapped_note_ons_are_counted_and_tracked_instead_of_disappearing()
        {
            var resolver = new GameplayKitMappingResolver(null, Generic());

            resolver.Resolve(RawMidiMessage.NoteOn(9, 22, 90, 1.0));
            resolver.Resolve(RawMidiMessage.NoteOn(9, 22, 90, 1.1));
            resolver.Resolve(RawMidiMessage.NoteOn(9, 26, 90, 1.2));
            resolver.Resolve(RawMidiMessage.NoteOn(9, 26, 0, 1.3));
            resolver.Resolve(RawMidiMessage.NoteOff(9, 26, 0, 1.4));

            Assert.That(resolver.UnmappedNoteOnCount, Is.EqualTo(3));
            Assert.That(resolver.UnmappedNotes, Is.EqualTo(new[] { 22, 26 }));
        }

        [Test]
        public void Unmapped_note_tracking_is_bounded()
        {
            var resolver = new GameplayKitMappingResolver(null, Generic());
            for (int note = 60; note < 127; note++) resolver.Resolve(RawMidiMessage.NoteOn(9, note, 90, 1.0));

            Assert.That(resolver.UnmappedNotes, Has.Count.EqualTo(GameplayKitMappingResolver.MaximumTrackedUnmappedNotes));
            Assert.That(resolver.UnmappedNoteOnCount, Is.EqualTo(127 - 60));
        }

        [Test]
        public void Works_with_only_the_generic_profile()
        {
            var resolver = new GameplayKitMappingResolver(null, Generic());

            GameplayKitMappingResolution resolution = resolver.Resolve(RawMidiMessage.NoteOn(9, 38, 90, 1.0));

            Assert.That(resolution.Origin, Is.EqualTo(GameplayKitMappingOrigin.Primary));
            Assert.That(resolver.HasFallback, Is.False);
        }

        [Test]
        public void Rejects_missing_configurations()
        {
            Assert.Throws<ArgumentException>(() => new GameplayKitMappingResolver(null, null));
            Assert.Throws<ArgumentNullException>(() => new GameplayKitMappingResolver(null, Generic()).Resolve(null));
        }

        private static UserKitConfiguration Generic()
        {
            return Configuration("generic",
                new[]
                {
                    Element("kick.default", KitPiece.Kick, KitArticulation.Default),
                    Element("snare.head", KitPiece.Snare, KitArticulation.Head)
                },
                new[]
                {
                    Mapping("gm.note.36", "kick.default", 36, MidiMappingSource.BuiltInProfile),
                    Mapping("gm.note.38", "snare.head", 38, MidiMappingSource.BuiltInProfile)
                });
        }

        private static KitElement Element(string id, KitPiece piece, KitArticulation articulation) =>
            new KitElement(id, piece, articulation, id);

        private static MidiMappingEntry Mapping(string id, string elementId, int note, MidiMappingSource source, bool enabled = true) =>
            new MidiMappingEntry(id, new MidiTrigger(RawMidiMessageKind.NoteOn, null, note, 1, 127), elementId, source: source, enabled: enabled);

        private static UserKitConfiguration Configuration(string id, KitElement[] elements, MidiMappingEntry[] mappings) =>
            new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "test." + id, id, null, null, null,
                elements, mappings, Array.Empty<string>());
    }
}
