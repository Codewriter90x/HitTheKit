using System;
using HitTheKit.Core;
using HitTheKit.Unity.Devices;
using HitTheKit.Unity.Input;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class MidiKitMappingEngineTests
    {
        [TestCase(36, "kick.default", KitPiece.Kick, KitArticulation.Default)]
        [TestCase(38, "snare.head", KitPiece.Snare, KitArticulation.Head)]
        [TestCase(40, "snare.rim", KitPiece.Snare, KitArticulation.Rim)]
        [TestCase(42, "hihat.closed", KitPiece.HiHat, KitArticulation.Closed)]
        [TestCase(46, "hihat.open", KitPiece.HiHat, KitArticulation.Open)]
        [TestCase(44, "hihat.pedal", KitPiece.HiHat, KitArticulation.Pedal)]
        public void Maps_note_articulations(int note, string elementId, KitPiece piece, KitArticulation articulation)
        {
            UserKitConfiguration configuration = Configuration(
                Element(elementId, piece, articulation),
                Mapping("mapping.test", elementId, RawMidiMessageKind.NoteOn, null, note));
            MidiKitMappingResult result = new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(12, note, 96, 2.5), configuration);
            Assert.That(result.Status, Is.EqualTo(MidiKitMappingStatus.Mapped));
            Assert.That((result.Hit.Piece, result.Hit.Articulation), Is.EqualTo((piece, articulation)));
            Assert.That((result.Hit.Velocity, result.Hit.TimestampSeconds, result.Hit.SourceMappingId),
                Is.EqualTo((96, (double?)2.5, "mapping.test")));
            Assert.That(result.Hit.Source, Is.EqualTo(MidiMappingSource.BuiltInProfile));
        }

        [Test]
        public void Maps_crash_choke_and_ride_bell()
        {
            var elements = new[]
            {
                Element("crash1.choke", KitPiece.Crash1, KitArticulation.Choke),
                Element("ride.bell", KitPiece.Ride, KitArticulation.Bell)
            };
            var mappings = new[]
            {
                Mapping("mapping.choke", "crash1.choke", RawMidiMessageKind.PolyAftertouch, 9, 49),
                Mapping("mapping.bell", "ride.bell", RawMidiMessageKind.NoteOn, 9, 53)
            };
            UserKitConfiguration configuration = Configuration(elements, mappings);
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.PolyAftertouch(9, 49, 80), configuration).Hit.Articulation,
                Is.EqualTo(KitArticulation.Choke));
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(9, 53, 80), configuration).Hit.Articulation,
                Is.EqualTo(KitArticulation.Bell));
        }

        [Test]
        public void Honors_exact_and_wildcard_channels()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            UserKitConfiguration exact = Configuration(kick, Mapping("mapping.exact", kick.Id, RawMidiMessageKind.NoteOn, 9, 36));
            UserKitConfiguration wildcard = Configuration(kick, Mapping("mapping.wildcard", kick.Id, RawMidiMessageKind.NoteOn, null, 36));
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(8, 36, 100), exact).Status, Is.EqualTo(MidiKitMappingStatus.NoMatch));
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(8, 36, 100), wildcard).Status, Is.EqualTo(MidiKitMappingStatus.Mapped));
        }

        [Test]
        public void Velocity_zero_does_not_match_note_on_mapping()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            UserKitConfiguration configuration = Configuration(kick, Mapping("mapping.note", kick.Id, RawMidiMessageKind.NoteOn, null, 36));
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(0, 36, 0), configuration).Status,
                Is.EqualTo(MidiKitMappingStatus.NoMatch));
        }

        [Test]
        public void Complete_rejects_disabled_required_element_and_optional_disabled_is_explicit()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            MidiMappingEntry disabledMapping = Mapping("mapping.disabled", kick.Id, RawMidiMessageKind.NoteOn, null, 36, enabled: false);
            Assert.Throws<ArgumentException>(() => Configuration(kick, disabledMapping));
            Assert.Throws<ArgumentException>(() => Configuration(
                new[] { kick },
                new[] { Mapping("mapping.enabled", kick.Id, RawMidiMessageKind.NoteOn, null, 36) },
                new[] { kick.Id }));

            KitElement optionalRim = new KitElement(
                "snare.rim",
                KitPiece.Snare,
                KitArticulation.Rim,
                "Snare rim",
                true);
            UserKitConfiguration disabledOptional = Configuration(
                new[] { optionalRim },
                new[] { Mapping("mapping.optional", optionalRim.Id, RawMidiMessageKind.NoteOn, null, 40) },
                new[] { optionalRim.Id });
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(0, 40, 100), disabledOptional).Status,
                Is.EqualTo(MidiKitMappingStatus.Disabled));
        }

        [Test]
        public void Mapping_precedence_and_ambiguity_are_deterministic()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            KitElement snare = Element("snare.head", KitPiece.Snare, KitArticulation.Head);
            var builtIn = new MidiMappingEntry("mapping.builtin",
                new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 1, 100), kick.Id, 0, MidiMappingSource.BuiltInProfile);
            var user = new MidiMappingEntry("mapping.user",
                new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 1, 127), snare.Id, 0, MidiMappingSource.UserOverride);
            UserKitConfiguration overrideConfiguration = Configuration(new[] { kick, snare }, new[] { builtIn, user });
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(2, 36, 80), overrideConfiguration).Hit.ElementId,
                Is.EqualTo(snare.Id));

            var user2 = new MidiMappingEntry("mapping.user2",
                new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 20, 110), kick.Id, 0, MidiMappingSource.UserOverride);
            UserKitConfiguration ambiguous = Configuration(new[] { kick, snare }, new[] { user, user2 });
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(2, 36, 80), ambiguous).Status,
                Is.EqualTo(MidiKitMappingStatus.Ambiguous));
        }

        [Test]
        public void Exact_disabled_user_override_suppresses_built_in_mapping()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            var trigger = new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 1, 127);
            var builtIn = new MidiMappingEntry("mapping.builtin", trigger, kick.Id, source: MidiMappingSource.BuiltInProfile);
            var disabledOverride = new MidiMappingEntry(
                "mapping.user-disabled",
                trigger,
                kick.Id,
                source: MidiMappingSource.UserOverride,
                enabled: false);

            UserKitConfiguration configuration = Configuration(new[] { kick }, new[] { builtIn, disabledOverride });
            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(0, 36, 100), configuration).Status,
                Is.EqualTo(MidiKitMappingStatus.Disabled));
        }

        [Test]
        public void Draft_configuration_is_not_accepted_for_runtime_mapping()
        {
            KitElement kick = Element("kick.default", KitPiece.Kick, KitArticulation.Default);
            MidiMappingEntry mapping = Mapping("mapping.kick", kick.Id, RawMidiMessageKind.NoteOn, null, 36);
            var draft = new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "test.draft", "Draft", null, null, null,
                new[] { kick }, new[] { mapping }, Array.Empty<string>(), isComplete: false);

            Assert.That(new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(0, 36, 100), draft).Status,
                Is.EqualTo(MidiKitMappingStatus.Invalid));
        }

        [Test]
        public void No_mapping_returns_no_match_without_hit()
        {
            UserKitConfiguration configuration = Configuration(
                Element("kick.default", KitPiece.Kick, KitArticulation.Default),
                Mapping("mapping.kick", "kick.default", RawMidiMessageKind.NoteOn, null, 36));
            MidiKitMappingResult result = new MidiKitMappingEngine().Map(RawMidiMessage.NoteOn(0, 37, 100), configuration);
            Assert.That(result.Status, Is.EqualTo(MidiKitMappingStatus.NoMatch));
            Assert.That(result.Hit, Is.Null);
        }

        [TestCase(KitPiece.Kick, KitArticulation.Default, DrumPad.Kick)]
        [TestCase(KitPiece.Snare, KitArticulation.Head, DrumPad.Snare)]
        [TestCase(KitPiece.Snare, KitArticulation.Rim, DrumPad.Snare)]
        [TestCase(KitPiece.HiHat, KitArticulation.Closed, DrumPad.HiHat)]
        [TestCase(KitPiece.HiHat, KitArticulation.Open, DrumPad.HiHat)]
        [TestCase(KitPiece.HiHat, KitArticulation.Pedal, DrumPad.HiHat)]
        [TestCase(KitPiece.Tom1, KitArticulation.Head, DrumPad.Tom1)]
        [TestCase(KitPiece.Tom2, KitArticulation.Head, DrumPad.Tom2)]
        [TestCase(KitPiece.FloorTom, KitArticulation.Head, DrumPad.FloorTom)]
        [TestCase(KitPiece.Crash1, KitArticulation.Bow, DrumPad.Crash)]
        [TestCase(KitPiece.Ride, KitArticulation.Bell, DrumPad.Ride)]
        public void Mvp_bridge_maps_supported_piece_articulations(
            KitPiece piece,
            KitArticulation articulation,
            DrumPad expectedPad)
        {
            NormalizedKitHit hit = Hit(piece, articulation, 3.25);
            MvpDrumInputMappingStatus status = new MvpDrumInputMapper().Map(hit, true, out var drumInput);
            Assert.That(status, Is.EqualTo(MvpDrumInputMappingStatus.Mapped));
            Assert.That((drumInput.Pad, drumInput.Velocity, drumInput.SongTimeSeconds),
                Is.EqualTo((expectedPad, 90, 3.25)));
            Assert.That(drumInput.Source, Is.EqualTo(DrumInputSource.Midi));
        }

        [Test]
        public void Mvp_bridge_rejects_continuous_control_messages_as_gameplay_hits()
        {
            RawMidiMessage message = RawMidiMessage.ControlChange(9, 4, 82, 1.5);
            var hit = new NormalizedKitHit(
                "hihat.continuous",
                KitPiece.HiHat,
                KitArticulation.HalfOpen,
                82,
                message,
                "mapping.hihat.continuous",
                MidiMappingSource.WizardCapture);

            Assert.That(new MvpDrumInputMapper().Map(hit, true, out _),
                Is.EqualTo(MvpDrumInputMappingStatus.UnsupportedInCurrentGameplay));
        }

        [TestCase(KitPiece.HiHat, KitArticulation.Choke)]
        public void Mvp_bridge_reports_unsupported_pieces(KitPiece piece, KitArticulation articulation)
        {
            Assert.That(new MvpDrumInputMapper().Map(Hit(piece, articulation, 1), true, out _),
                Is.EqualTo(MvpDrumInputMappingStatus.UnsupportedInCurrentGameplay));
        }

        [Test]
        public void Mvp_bridge_reports_disabled_and_invalid()
        {
            Assert.That(new MvpDrumInputMapper().Map(Hit(KitPiece.Kick, KitArticulation.Default, 1), false, out _),
                Is.EqualTo(MvpDrumInputMappingStatus.Disabled));
            Assert.That(new MvpDrumInputMapper().Map(null, true, out _), Is.EqualTo(MvpDrumInputMappingStatus.Invalid));
            Assert.That(new MvpDrumInputMapper().Map(Hit(KitPiece.Kick, KitArticulation.Default, null), true, out _),
                Is.EqualTo(MvpDrumInputMappingStatus.Invalid));
        }

        private static NormalizedKitHit Hit(KitPiece piece, KitArticulation articulation, double? timestamp)
        {
            RawMidiMessage message = RawMidiMessage.NoteOn(0, 36, 90, timestamp);
            return new NormalizedKitHit(
                "test.element",
                piece,
                articulation,
                90,
                message,
                "mapping.test",
                MidiMappingSource.BuiltInProfile);
        }

        private static KitElement Element(string id, KitPiece piece, KitArticulation articulation)
        {
            return new KitElement(id, piece, articulation, id);
        }

        private static MidiMappingEntry Mapping(
            string id,
            string elementId,
            RawMidiMessageKind kind,
            int? channel,
            int data1,
            bool enabled = true)
        {
            return new MidiMappingEntry(id, new MidiTrigger(kind, channel, data1, kind == RawMidiMessageKind.NoteOn ? 1 : 0, 127),
                elementId, enabled: enabled);
        }

        private static UserKitConfiguration Configuration(KitElement element, MidiMappingEntry mapping)
        {
            return Configuration(new[] { element }, new[] { mapping });
        }

        private static UserKitConfiguration Configuration(
            KitElement[] elements,
            MidiMappingEntry[] mappings,
            string[] disabled = null)
        {
            return new UserKitConfiguration(UserKitConfigurationLoader.SupportedSchemaVersion, "test.configuration", "Test", null, null, null,
                elements, mappings, disabled ?? Array.Empty<string>());
        }
    }
}
