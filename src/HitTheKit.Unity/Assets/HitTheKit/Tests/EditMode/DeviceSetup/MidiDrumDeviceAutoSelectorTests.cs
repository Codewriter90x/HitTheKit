using System;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class MidiDrumDeviceAutoSelectorTests
    {
        [Test]
        public void No_devices_means_keyboard_only()
        {
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(Array.Empty<DrumDeviceDescriptor>(), null);
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.NoDevice));
            Assert.That(selection.Device, Is.Null);
        }

        [Test]
        public void Remembered_device_is_used_when_connected()
        {
            DrumDeviceDescriptor kit = Device("coremidi.1", "USB MIDI Drum");
            DrumDeviceDescriptor other = Device("coremidi.2", "Audio Interface");
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { kit, other }, "coremidi.2");
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.Preferred));
            Assert.That(selection.Device, Is.SameAs(other));
        }

        [Test]
        public void Disconnected_remembered_device_is_ignored()
        {
            DrumDeviceDescriptor gone = Device("coremidi.1", "Old Kit", DeviceConnectionState.Disconnected);
            DrumDeviceDescriptor kit = Device("coremidi.2", "USB MIDI Drum");
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { gone, kit }, "coremidi.1");
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.OnlyCandidate));
            Assert.That(selection.Device, Is.SameAs(kit));
        }

        [Test]
        public void Virtual_ports_are_not_mistaken_for_a_drum_kit()
        {
            DrumDeviceDescriptor iac = Device("coremidi.1", "IAC Driver", port: "Bus 1");
            DrumDeviceDescriptor network = Device("coremidi.2", "Network Session 1");
            DrumDeviceDescriptor kit = Device("coremidi.3", "USB MIDI Drum");
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { iac, network, kit }, null);
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.OnlyCandidate));
            Assert.That(selection.Device, Is.SameAs(kit));
        }

        [Test]
        public void A_device_with_a_recognized_drum_profile_is_preferred()
        {
            DrumDeviceDescriptor audio = Device("coremidi.1", "Audio Interface");
            var hampback = new DrumDeviceDescriptor("coremidi.2", "HAMPBACK", "DREAM S.A.S.", "eDrum -1",
                DeviceConnectionState.Connected, new[] { SimulatedDrumDeviceDiscovery.CreateHampbackCandidateProfile() });
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { audio, hampback }, null);
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.RecognizedProfile));
            Assert.That(selection.Device, Is.SameAs(hampback));
        }

        [Test]
        public void Several_plausible_inputs_start_a_probe()
        {
            DrumDeviceDescriptor audio = Device("coremidi.1", "Audio Interface");
            DrumDeviceDescriptor kit = Device("coremidi.2", "USB MIDI Drum");
            DrumDeviceDescriptor iac = Device("coremidi.3", "IAC Driver");
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { audio, kit, iac }, null);
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.Probe));
            Assert.That(selection.ProbeCandidates, Is.EqualTo(new[] { audio, kit }));
        }

        [Test]
        public void Remembered_virtual_port_is_still_honoured()
        {
            DrumDeviceDescriptor iac = Device("coremidi.1", "IAC Driver");
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(new[] { iac }, "coremidi.1");
            Assert.That(selection.Kind, Is.EqualTo(MidiDrumDeviceSelectionKind.Preferred));
        }

        [Test]
        public void Probe_rotates_inputs_until_a_note_on_arrives_then_locks()
        {
            DrumDeviceDescriptor audio = Device("coremidi.1", "Audio Interface");
            DrumDeviceDescriptor kit = Device("coremidi.2", "USB MIDI Drum");
            var probe = new MidiDrumDeviceProbe(new[] { audio, kit }, 10.0, 0.5);

            Assert.That(probe.Current, Is.SameAs(audio));
            Assert.That(probe.Advance(10.2), Is.False);
            Assert.That(probe.Advance(10.5), Is.True);
            Assert.That(probe.Current, Is.SameAs(kit));

            Assert.That(probe.ReportMessage(RawMidiMessage.ControlChange(9, 4, 64, 10.6)), Is.False);
            Assert.That(probe.ReportMessage(RawMidiMessage.NoteOn(9, 38, 0, 10.6)), Is.False);
            Assert.That(probe.ReportMessage(RawMidiMessage.NoteOn(9, 38, 100, 10.7)), Is.True);
            Assert.That(probe.LockedDevice, Is.SameAs(kit));

            Assert.That(probe.Advance(20.0), Is.False);
            Assert.That(probe.Current, Is.SameAs(kit));
            Assert.That(probe.ReportMessage(RawMidiMessage.NoteOn(9, 36, 100, 20.1)), Is.False);
        }

        [Test]
        public void Probe_wraps_around_candidates()
        {
            DrumDeviceDescriptor first = Device("coremidi.1", "First");
            DrumDeviceDescriptor second = Device("coremidi.2", "Second");
            var probe = new MidiDrumDeviceProbe(new[] { first, second }, 0.0, 1.0);
            probe.Advance(1.0);
            probe.Advance(2.0);
            Assert.That(probe.Current, Is.SameAs(first));
        }

        [Test]
        public void Probe_rejects_invalid_arguments()
        {
            Assert.Throws<ArgumentNullException>(() => new MidiDrumDeviceProbe(null, 0.0));
            Assert.Throws<ArgumentException>(() => new MidiDrumDeviceProbe(Array.Empty<DrumDeviceDescriptor>(), 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiDrumDeviceProbe(new[] { Device("coremidi.1", "Kit") }, 0.0, 0.0));
        }

        private static DrumDeviceDescriptor Device(
            string id,
            string name,
            DeviceConnectionState state = DeviceConnectionState.Connected,
            string port = null)
        {
            return new DrumDeviceDescriptor(id, name, string.Empty, port ?? name, state, Array.Empty<DeviceProfileOption>());
        }
    }
}
