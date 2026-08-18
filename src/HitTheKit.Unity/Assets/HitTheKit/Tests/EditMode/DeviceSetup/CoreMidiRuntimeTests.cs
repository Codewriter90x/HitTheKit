using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class CoreMidiRuntimeTests
    {
        [Test]
        public void Native_struct_layout_matches_C_ABI()
        {
            Assert.That(Marshal.SizeOf<CoreMidiNativeDeviceInfo>(), Is.EqualTo(1304));
            Assert.That(Marshal.SizeOf<CoreMidiNativeMessage>(), Is.EqualTo(32));
            Assert.That(Marshal.SizeOf<CoreMidiNativeDiagnostics>(), Is.EqualTo(48));
        }

        [Test]
        public void Generated_plugin_loads_when_present_and_absence_is_a_supported_state()
        {
            var api = new CoreMidiNativeApi();
            bool generatedPluginExists = File.Exists(Path.Combine(Application.dataPath, "Plugins/macOS/HitTheKitCoreMidi.dylib"));
            if (generatedPluginExists) Assert.That(api.IsAvailable, Is.True, api.AvailabilityMessage);
            if (!api.IsAvailable)
            {
                Assert.That(api.AvailabilityMessage, Is.Not.Empty);
                return;
            }
            Assert.That(api.GetApiVersion(), Is.EqualTo(CoreMidiNativeApi.ExpectedApiVersion));
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.That(api.CreateClient(), Is.Zero);
                Assert.That(api.RefreshDevices(), Is.GreaterThanOrEqualTo(0));
                api.CloseInput();
                api.DestroyClient();
            }
        }

        [Test]
        public void Api_version_mismatch_is_unavailable_without_creating_client()
        {
            var api = new FakeNativeApi { ApiVersion = 99 };
            using var session = new CoreMidiNativeSession(api);
            Assert.That(session.IsAvailable, Is.False);
            Assert.That(session.EnsureCreated(), Is.False);
            Assert.That(api.CreateCalls, Is.Zero);
        }

        [Test]
        public void Unavailable_plugin_returns_structured_failed_discovery()
        {
            var api = new FakeNativeApi { IsAvailable = false, AvailabilityMessage = "plug-in absent" };
            using var session = new CoreMidiNativeSession(api);
            DeviceDiscoverySnapshot snapshot = new CoreMidiDrumDeviceDiscovery(session).Refresh();
            Assert.That(snapshot.State, Is.EqualTo(DeviceDiscoveryState.Failed));
            Assert.That(snapshot.Devices, Is.Empty);
            Assert.That(snapshot.Message, Is.EqualTo("plug-in absent"));
        }

        [Test]
        public void Discovery_preserves_duplicate_names_with_stable_endpoint_identity()
        {
            var api = new FakeNativeApi();
            api.Devices.Add(Device(101, "Duplicate", "Maker"));
            api.Devices.Add(Device(202, "Duplicate", "Maker"));
            using var session = new CoreMidiNativeSession(api);
            var discovery = new CoreMidiDrumDeviceDiscovery(session);
            DeviceDiscoverySnapshot snapshot = discovery.Refresh();
            Assert.That(snapshot.State, Is.EqualTo(DeviceDiscoveryState.Ready));
            Assert.That(snapshot.Devices.Select(value => value.Id), Is.EqualTo(new[] { "coremidi.101", "coremidi.202" }));
            Assert.That(snapshot.Devices.Select(value => value.DisplayName).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Observed_HAMPBACK_identity_gets_candidate_without_auto_selection()
        {
            var info = Device(301, "eDrum -1", "DREAM S.A.S.");
            IReadOnlyList<DeviceProfileOption> profiles = DeviceSetupProfileCatalog.ForCoreMidiDevice(info);
            Assert.That(profiles, Has.Count.EqualTo(1));
            Assert.That(profiles[0].ProductionReady, Is.False);
            Assert.That(profiles[0].AutoSelectable, Is.False);
            Assert.That(profiles[0].RequiresConfirmation, Is.True);
        }

        [Test]
        public void Native_messages_convert_to_foundation_contract_and_preserve_velocity_zero()
        {
            RawMidiMessage note = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.NoteOn, 9, 36, 0));
            RawMidiMessage cc = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.ControlChange, 9, 4, 78));
            RawMidiMessage poly = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.PolyAftertouch, 9, 59, 127));
            RawMidiMessage channel = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.ChannelAftertouch, 9, -1, 64));
            RawMidiMessage pitch = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.PitchBend, 9, -1, 8192));
            RawMidiMessage program = CoreMidiGuidedMidiCaptureSource.Convert(Message(CoreMidiNativeMessageKind.ProgramChange, 9, 8, 8));
            Assert.That(note.Kind, Is.EqualTo(RawMidiMessageKind.NoteOn));
            Assert.That(note.SemanticKind, Is.EqualTo(RawMidiMessageKind.NoteOff));
            Assert.That(cc.Data1, Is.EqualTo(4));
            Assert.That(poly.Kind, Is.EqualTo(RawMidiMessageKind.PolyAftertouch));
            Assert.That(channel.Data1, Is.Null);
            Assert.That(pitch.Value, Is.EqualTo(8192));
            Assert.That(program.Data1, Is.EqualTo(8));
            Assert.That(note.TimestampSeconds, Is.EqualTo(1.25));
        }

        [Test]
        public void Capture_polls_on_caller_thread_reports_diagnostics_and_stops_events_after_dispose()
        {
            var api = new FakeNativeApi();
            api.Devices.Add(Device(101, "Input", "Maker"));
            api.Messages.Enqueue(Message(CoreMidiNativeMessageKind.NoteOn, 9, 36, 100));
            api.Messages.Enqueue(Message(CoreMidiNativeMessageKind.ControlChange, 9, 4, 90));
            api.Diagnostics = new CoreMidiNativeDiagnostics { QueueCapacity = 4096, DroppedMessages = 3, DeviceGeneration = 1 };
            using var session = new CoreMidiNativeSession(api);
            var capture = new CoreMidiGuidedMidiCaptureSource(session);
            var received = new List<RawMidiMessage>();
            capture.MessageReceived += received.Add;
            capture.SelectDevice("coremidi.101");
            capture.Start("map.kick-default");
            Assert.That(capture.Poll(), Is.EqualTo(2));
            Assert.That(received, Has.Count.EqualTo(2));
            Assert.That(capture.Diagnostics.DroppedMessages, Is.EqualTo(3));
            capture.Dispose();
            api.Messages.Enqueue(Message(CoreMidiNativeMessageKind.NoteOn, 9, 38, 100));
            Assert.That(capture.Poll(), Is.Zero);
            Assert.That(received, Has.Count.EqualTo(2));
            capture.Dispose();
        }

        [Test]
        public void Capture_rejects_background_polling_without_emitting_events()
        {
            var api = new FakeNativeApi();
            api.Devices.Add(Device(101, "Input", "Maker"));
            api.Messages.Enqueue(Message(CoreMidiNativeMessageKind.NoteOn, 9, 36, 100));
            using var session = new CoreMidiNativeSession(api);
            using var capture = new CoreMidiGuidedMidiCaptureSource(session);
            int received = 0;
            capture.MessageReceived += _ => received++;
            capture.SelectDevice("coremidi.101");
            capture.Start("map.kick-default");

            Exception failure = Task.Run(() =>
            {
                try { capture.Poll(); }
                catch (Exception exception) { return exception; }
                return null;
            }).GetAwaiter().GetResult();

            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(received, Is.Zero);
            Assert.That(capture.Poll(), Is.EqualTo(1));
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void Disconnect_and_explicit_reconnect_do_not_duplicate_events()
        {
            var api = new FakeNativeApi();
            api.Devices.Add(Device(101, "Input", "Maker"));
            using var session = new CoreMidiNativeSession(api);
            var capture = new CoreMidiGuidedMidiCaptureSource(session);
            int connections = 0;
            int messages = 0;
            capture.ConnectionChanged += _ => connections++;
            capture.MessageReceived += _ => messages++;
            capture.SelectDevice("coremidi.101");
            capture.Start("first");
            api.ConnectionState = 0;
            capture.Poll();
            Assert.That(capture.ConnectionState, Is.EqualTo(DeviceConnectionState.Disconnected));
            api.ConnectionState = 1;
            capture.Start("second");
            api.Messages.Enqueue(Message(CoreMidiNativeMessageKind.NoteOn, 9, 36, 100));
            capture.Poll();
            Assert.That(messages, Is.EqualTo(1));
            Assert.That(connections, Is.EqualTo(3));
        }

        [Test]
        public void Native_open_error_becomes_structured_disconnected_state()
        {
            var api = new FakeNativeApi { OpenResult = -5, LastError = "synthetic open failure" };
            api.Devices.Add(Device(101, "Input", "Maker"));
            using var session = new CoreMidiNativeSession(api);
            var capture = new CoreMidiGuidedMidiCaptureSource(session);
            capture.SelectDevice("coremidi.101");
            capture.Start("map.kick-default");
            Assert.That(capture.IsCapturing, Is.False);
            Assert.That(capture.ConnectionState, Is.EqualTo(DeviceConnectionState.Disconnected));
            Assert.That(capture.LastError, Is.EqualTo("synthetic open failure"));
        }

        [Test]
        public void Discovery_observes_hotplug_generation_only_after_native_notification()
        {
            var api = new FakeNativeApi();
            using var session = new CoreMidiNativeSession(api);
            var discovery = new CoreMidiDrumDeviceDiscovery(session);
            discovery.Refresh();
            Assert.That(discovery.HasDevicesChanged, Is.False);
            api.Diagnostics = new CoreMidiNativeDiagnostics { DeviceGeneration = 2 };
            Assert.That(discovery.HasDevicesChanged, Is.True);
            discovery.Refresh();
            Assert.That(discovery.HasDevicesChanged, Is.False);
        }

        private static CoreMidiNativeDeviceInfo Device(long id, string name, string manufacturer) => new CoreMidiNativeDeviceInfo
        {
            EndpointId = id, Name = name, DeviceName = name, Manufacturer = manufacturer, EntityName = name, IsOnline = 1, Protocol = 1
        };

        private static CoreMidiNativeMessage Message(CoreMidiNativeMessageKind kind, int channel, int data1, int data2) => new CoreMidiNativeMessage
        {
            Sequence = 1, MonotonicSeconds = 1.25, MessageKind = (int)kind, Channel = channel, Data1 = data1, Data2 = data2
        };

        private sealed class FakeNativeApi : ICoreMidiNativeApi
        {
            public bool IsAvailable { get; set; } = true;
            public string AvailabilityMessage { get; set; }
            public int ApiVersion { get; set; } = CoreMidiNativeApi.ExpectedApiVersion;
            public int CreateCalls { get; private set; }
            public int ConnectionState { get; set; } = 1;
            public int OpenResult { get; set; }
            public string LastError { get; set; } = "synthetic native error";
            public List<CoreMidiNativeDeviceInfo> Devices { get; } = new List<CoreMidiNativeDeviceInfo>();
            public Queue<CoreMidiNativeMessage> Messages { get; } = new Queue<CoreMidiNativeMessage>();
            public CoreMidiNativeDiagnostics Diagnostics { get; set; }
            public int GetApiVersion() => ApiVersion;
            public double GetMonotonicSeconds() => 10.0;
            public int CreateClient() { CreateCalls++; return 0; }
            public void DestroyClient() { }
            public int RefreshDevices() => Devices.Count;
            public int GetDeviceCount() => Devices.Count;
            public int GetDeviceInfo(int index, out CoreMidiNativeDeviceInfo info) { info = Devices[index]; info.Index = index; return 0; }
            public int OpenInput(long endpointId) { ConnectionState = 1; return OpenResult != 0 ? OpenResult : Devices.Any(value => value.EndpointId == endpointId) ? 0 : -1; }
            public void CloseInput() { }
            public int PollMessages(CoreMidiNativeMessage[] buffer, int capacity)
            {
                int count = 0;
                while (count < capacity && Messages.Count > 0) buffer[count++] = Messages.Dequeue();
                return count;
            }
            public int GetConnectionState() => ConnectionState;
            public int GetDiagnostics(out CoreMidiNativeDiagnostics diagnostics) { diagnostics = Diagnostics; return 0; }
            public string GetLastError() => LastError;
        }
    }
}
