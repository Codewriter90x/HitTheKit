using System;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class DeviceDomainTests
    {
        [TestCase(KitPiece.Kick, KitArticulation.Default)]
        [TestCase(KitPiece.Snare, KitArticulation.Head)]
        [TestCase(KitPiece.Snare, KitArticulation.Rim)]
        [TestCase(KitPiece.HiHat, KitArticulation.HalfOpen)]
        [TestCase(KitPiece.HiHat, KitArticulation.Bow)]
        [TestCase(KitPiece.HiHat, KitArticulation.Edge)]
        [TestCase(KitPiece.HiHat, KitArticulation.Choke)]
        [TestCase(KitPiece.Crash1, KitArticulation.Choke)]
        [TestCase(KitPiece.Ride, KitArticulation.Bell)]
        [TestCase(KitPiece.Ride, KitArticulation.Choke)]
        [TestCase(KitPiece.Tom1, KitArticulation.Rim)]
        public void Element_combinations_accept_supported_pairs(KitPiece piece, KitArticulation articulation)
        {
            Assert.That(KitElementDefinitionValidator.IsValid(piece, articulation), Is.True);
        }

        [TestCase(KitPiece.Kick, KitArticulation.Bell)]
        [TestCase(KitPiece.Snare, KitArticulation.Open)]
        [TestCase(KitPiece.HiHat, KitArticulation.Head)]
        [TestCase(KitPiece.Ride, KitArticulation.Pedal)]
        public void Element_combinations_reject_invalid_pairs(KitPiece piece, KitArticulation articulation)
        {
            Assert.That(KitElementDefinitionValidator.IsValid(piece, articulation), Is.False);
        }

        [Test]
        public void Default_is_a_fallback_for_every_known_piece()
        {
            foreach (KitPiece piece in Enum.GetValues(typeof(KitPiece)))
            {
                Assert.That(KitElementDefinitionValidator.IsValid(piece, KitArticulation.Default), Is.True);
            }
        }

        [Test]
        public void Element_validator_rejects_out_of_domain_enums()
        {
            Assert.That(KitElementDefinitionValidator.IsValid((KitPiece)99, KitArticulation.Default), Is.False);
            Assert.That(KitElementDefinitionValidator.IsValid(KitPiece.Kick, (KitArticulation)99), Is.False);
        }

        [TestCase(-1)]
        [TestCase(16)]
        public void Raw_note_rejects_invalid_channel(int channel)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RawMidiMessage.NoteOn(channel, 36, 100));
        }

        [TestCase(-1)]
        [TestCase(128)]
        public void Raw_note_rejects_invalid_note(int note)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RawMidiMessage.NoteOn(0, note, 100));
        }

        [TestCase(-1)]
        [TestCase(128)]
        public void Raw_note_rejects_invalid_velocity(int velocity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RawMidiMessage.NoteOn(0, 36, velocity));
        }

        [Test]
        public void Velocity_zero_note_on_has_note_off_semantics()
        {
            RawMidiMessage message = RawMidiMessage.NoteOn(0, 36, 0);
            Assert.That(message.Kind, Is.EqualTo(RawMidiMessageKind.NoteOn));
            Assert.That(message.SemanticKind, Is.EqualTo(RawMidiMessageKind.NoteOff));
        }

        [Test]
        public void Control_change_and_aftertouch_preserve_values()
        {
            RawMidiMessage cc = RawMidiMessage.ControlChange(2, 4, 91, 1.25);
            RawMidiMessage poly = RawMidiMessage.PolyAftertouch(3, 49, 80);
            RawMidiMessage channel = RawMidiMessage.ChannelAftertouch(4, 72);
            Assert.That((cc.Channel, cc.Data1, cc.Value, cc.TimestampSeconds), Is.EqualTo((2, (int?)4, 91, (double?)1.25)));
            Assert.That(poly.Kind, Is.EqualTo(RawMidiMessageKind.PolyAftertouch));
            Assert.That(channel.Data1, Is.Null);
        }

        [Test]
        public void Pitch_bend_and_program_change_preserve_native_values()
        {
            RawMidiMessage pitch = RawMidiMessage.PitchBend(3, 8192, 2.5);
            RawMidiMessage program = RawMidiMessage.ProgramChange(4, 12, 2.6);
            Assert.That(pitch.Kind, Is.EqualTo(RawMidiMessageKind.PitchBend));
            Assert.That(pitch.Data1, Is.Null);
            Assert.That(pitch.Value, Is.EqualTo(8192));
            Assert.That(program.Kind, Is.EqualTo(RawMidiMessageKind.ProgramChange));
            Assert.That(program.Data1, Is.EqualTo(12));
            Assert.That(program.Value, Is.EqualTo(12));
            Assert.Throws<ArgumentOutOfRangeException>(() => RawMidiMessage.PitchBend(0, 16384));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Raw_message_rejects_non_finite_timestamp(double timestamp)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RawMidiMessage.NoteOn(0, 36, 100, timestamp));
        }

        [Test]
        public void Trigger_matches_exact_note_and_value_range()
        {
            var trigger = new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 50, 100);
            Assert.That(trigger.Matches(RawMidiMessage.NoteOn(9, 36, 50)), Is.True);
            Assert.That(trigger.Matches(RawMidiMessage.NoteOn(9, 36, 100)), Is.True);
            Assert.That(trigger.Matches(RawMidiMessage.NoteOn(9, 36, 49)), Is.False);
        }

        [Test]
        public void Trigger_supports_channel_wildcard_and_exact_channel()
        {
            var wildcard = new MidiTrigger(RawMidiMessageKind.NoteOn, null, 38, 1, 127);
            var exact = new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 38, 1, 127);
            Assert.That(wildcard.Matches(RawMidiMessage.NoteOn(4, 38, 80)), Is.True);
            Assert.That(exact.Matches(RawMidiMessage.NoteOn(4, 38, 80)), Is.False);
        }

        [Test]
        public void Trigger_matches_control_change_and_aftertouch()
        {
            var cc = new MidiTrigger(RawMidiMessageKind.ControlChange, null, 4, 0, 63);
            var poly = new MidiTrigger(RawMidiMessageKind.PolyAftertouch, 1, 49, 1, 127);
            var channel = new MidiTrigger(RawMidiMessageKind.ChannelAftertouch, 2, null, 20, 100);
            Assert.That(cc.Matches(RawMidiMessage.ControlChange(7, 4, 42)), Is.True);
            Assert.That(poly.Matches(RawMidiMessage.PolyAftertouch(1, 49, 50)), Is.True);
            Assert.That(channel.Matches(RawMidiMessage.ChannelAftertouch(2, 60)), Is.True);
        }

        [Test]
        public void Velocity_zero_note_on_matches_note_off_trigger_only()
        {
            RawMidiMessage message = RawMidiMessage.NoteOn(0, 36, 0);
            Assert.That(new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 0, 127).Matches(message), Is.False);
            Assert.That(new MidiTrigger(RawMidiMessageKind.NoteOff, null, 36, 0, 127).Matches(message), Is.True);
        }

        [Test]
        public void Trigger_rejects_invalid_ranges_and_shape()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiTrigger(RawMidiMessageKind.NoteOn, 16, 36));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiTrigger(RawMidiMessageKind.NoteOn, 0, 128));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiTrigger(RawMidiMessageKind.NoteOn, 0, 36, 90, 40));
            Assert.Throws<ArgumentException>(() => new MidiTrigger(RawMidiMessageKind.ChannelAftertouch, 0, 1));
        }

        [Test]
        public void Trigger_overlap_requires_a_message_that_can_match_both()
        {
            var wildcard = new MidiTrigger(RawMidiMessageKind.NoteOn, null, 36, 1, 100);
            var exact = new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 80, 127);
            var disjoint = new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 101, 127);
            var noteOff = new MidiTrigger(RawMidiMessageKind.NoteOff, 9, 36, 0, 127);
            var velocityZeroOnly = new MidiTrigger(RawMidiMessageKind.NoteOn, 9, 36, 0, 0);

            Assert.That(wildcard.Overlaps(exact), Is.True);
            Assert.That(wildcard.Overlaps(disjoint), Is.False);
            Assert.That(wildcard.Overlaps(noteOff), Is.False);
            Assert.That(velocityZeroOnly.Overlaps(velocityZeroOnly), Is.False);
        }
    }
}
