using System.Runtime.InteropServices;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class WinMmMidiNativeApiTests
    {
        [TestCase(0x00643A99u, CoreMidiNativeMessageKind.NoteOn, 9, 58, 100)]
        [TestCase(0x00262489u, CoreMidiNativeMessageKind.NoteOff, 9, 36, 38)]
        [TestCase(0x007F1AA9u, CoreMidiNativeMessageKind.PolyAftertouch, 9, 26, 127)]
        [TestCase(0x004004B9u, CoreMidiNativeMessageKind.ControlChange, 9, 4, 64)]
        public void Decodes_channel_voice_messages(uint packed, CoreMidiNativeMessageKind kind, int channel, int data1, int data2)
        {
            Assert.That(WinMmMidiNativeApi.TryDecodeShortMessage(packed, out int decodedKind, out int decodedChannel, out int decoded1, out int decoded2), Is.True);
            Assert.That((CoreMidiNativeMessageKind)decodedKind, Is.EqualTo(kind));
            Assert.That(decodedChannel, Is.EqualTo(channel));
            Assert.That(decoded1, Is.EqualTo(data1));
            Assert.That(decoded2, Is.EqualTo(data2));
        }

        [Test]
        public void Decodes_messages_with_one_data_byte_like_the_coremidi_parser()
        {
            WinMmMidiNativeApi.TryDecodeShortMessage(0x000005C9u, out int kind, out _, out int data1, out int data2);
            Assert.That((CoreMidiNativeMessageKind)kind, Is.EqualTo(CoreMidiNativeMessageKind.ProgramChange));
            Assert.That((data1, data2), Is.EqualTo((5, 0)));

            WinMmMidiNativeApi.TryDecodeShortMessage(0x00004DD9u, out kind, out _, out data1, out data2);
            Assert.That((CoreMidiNativeMessageKind)kind, Is.EqualTo(CoreMidiNativeMessageKind.ChannelAftertouch));
            Assert.That((data1, data2), Is.EqualTo((-1, 0x4D)));

            WinMmMidiNativeApi.TryDecodeShortMessage(0x004000E9u, out kind, out _, out data1, out data2);
            Assert.That((CoreMidiNativeMessageKind)kind, Is.EqualTo(CoreMidiNativeMessageKind.PitchBend));
            Assert.That(data2, Is.EqualTo(8192));
        }

        [TestCase(0x000000F8u)] // timing clock
        [TestCase(0x000000FEu)] // active sensing
        [TestCase(0x000000F0u)] // system exclusive start
        [TestCase(0x00643A00u)] // missing status byte
        [TestCase(0x00FF3A99u)] // invalid data byte
        public void Ignores_realtime_system_and_malformed_messages(uint packed)
        {
            Assert.That(WinMmMidiNativeApi.TryDecodeShortMessage(packed, out _, out _, out _, out _), Is.False);
        }

        [Test]
        public void Decoded_note_on_flows_into_the_existing_capture_conversion()
        {
            WinMmMidiNativeApi.TryDecodeShortMessage(0x00642699u, out int kind, out int channel, out int data1, out int data2);
            RawMidiMessage message = CoreMidiGuidedMidiCaptureSource.Convert(new CoreMidiNativeMessage
            {
                Sequence = 1, MonotonicSeconds = 2.5, MessageKind = kind, Channel = channel, Data1 = data1, Data2 = data2
            });

            Assert.That(message.SemanticKind, Is.EqualTo(RawMidiMessageKind.NoteOn));
            Assert.That(message.Data1, Is.EqualTo(38));
            Assert.That(message.Value, Is.EqualTo(100));
            Assert.That(message.TimestampSeconds, Is.EqualTo(2.5));
        }

        [Test]
        public void Endpoint_ids_are_stable_and_distinguish_identical_device_names()
        {
            long first = WinMmMidiNativeApi.StableEndpointId("eDrum", 0);
            Assert.That(WinMmMidiNativeApi.StableEndpointId("eDrum", 0), Is.EqualTo(first));
            Assert.That(WinMmMidiNativeApi.StableEndpointId("eDrum", 1), Is.Not.EqualTo(first));
            Assert.That(WinMmMidiNativeApi.StableEndpointId("USB MIDI Interface", 0), Is.Not.EqualTo(first));
            Assert.That(first, Is.GreaterThan(0));
            Assert.That(CoreMidiDrumDeviceDiscovery.TryParseDeviceId(CoreMidiDrumDeviceDiscovery.DeviceId(first), out long parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(first));
        }

        [Test]
        public void Reports_unavailable_outside_windows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Assert.Ignore("Runs on non-Windows editors only.");
            var api = new WinMmMidiNativeApi();
            Assert.That(api.IsAvailable, Is.False);
            Assert.That(api.CreateClient(), Is.Not.EqualTo(0));
            Assert.That(new CoreMidiNativeSession(api).IsAvailable, Is.False);
        }
    }
}
