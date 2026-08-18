using System;
using System.Collections.Generic;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public enum SimulatedCaptureScenario
    {
        CleanStandardKit,
        ContaminatedRide,
        MissingHiHatContinuous,
        ConflictingTrigger,
        DisconnectedMidStep,
        HampbackCapture2
    }

    public sealed class SimulatedDrumDeviceDiscovery : IDrumDeviceDiscovery
    {
        private IReadOnlyList<DrumDeviceDescriptor> devices;

        public SimulatedDrumDeviceDiscovery()
        {
            devices = DefaultDevices();
        }

        public DeviceDiscoverySnapshot Refresh()
        {
            return new DeviceDiscoverySnapshot(DeviceDiscoveryState.Ready, devices, devices.Count == 0 ? "No simulated devices available." : null);
        }

        public void ReplaceDevices(IReadOnlyList<DrumDeviceDescriptor> replacement)
        {
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            var copy = new DrumDeviceDescriptor[replacement.Count];
            for (int index = 0; index < copy.Length; index++) copy[index] = replacement[index] ?? throw new ArgumentException("Device is null.", nameof(replacement));
            devices = Array.AsReadOnly(copy);
        }

        public void Disconnect(string deviceId)
        {
            var replacement = new DrumDeviceDescriptor[devices.Count];
            for (int index = 0; index < devices.Count; index++)
            {
                DrumDeviceDescriptor device = devices[index];
                replacement[index] = device.Id == deviceId
                    ? new DrumDeviceDescriptor(device.Id, device.DisplayName, device.Manufacturer, device.PortName, DeviceConnectionState.Disconnected, device.Profiles)
                    : device;
            }
            devices = Array.AsReadOnly(replacement);
        }

        public static IReadOnlyList<DrumDeviceDescriptor> DefaultDevices()
        {
            DeviceProfileOption hampback = CreateHampbackCandidateProfile();
            var generic = new DeviceProfileOption(
                "generic-midi-drums-v1",
                "Generic MIDI Drum Kit",
                "Verified fallback",
                "Built-in generic mapping",
                "Generic GM-oriented fallback; confirm every step for this device.",
                true,
                false,
                true);

            return Array.AsReadOnly(new[]
            {
                new DrumDeviceDescriptor("device.hampback", "HAMPBACK", "DREAM S.A.S.", "eDrum -1", DeviceConnectionState.Connected, new[] { hampback }),
                new DrumDeviceDescriptor("device.generic", "Generic MIDI Drum Kit", "Generic", "MIDI Drum Port", DeviceConnectionState.Connected, new[] { generic }),
                new DrumDeviceDescriptor("device.unknown", "Unknown Electronic Drum", "Unknown", "USB MIDI Device", DeviceConnectionState.Connected, Array.Empty<DeviceProfileOption>())
            });
        }

        public static DeviceProfileOption CreateHampbackCandidateProfile()
        {
            return new DeviceProfileOption(
                "dream-edrum-hampback-candidate-001",
                "HAMPBACK exploratory candidate",
                "Candidate — not verified",
                "Verified exploratory capture #1",
                "Requires confirmation; not production-ready.",
                false,
                false,
                true,
                new[]
                {
                    Candidate("kick.default", 36, "High"),
                    Candidate("snare.head", 38, "High"),
                    Candidate("snare.rim", 40, "High"),
                    Candidate("tom1.head", 48, "High"),
                    Candidate("tom2.head", 45, "High"),
                    Candidate("floortom.head", 43, "High"),
                    Candidate("crash1.bow", 49, "High"),
                    Candidate("hihat.closed", 42, "Medium"),
                    Candidate("hihat.open", 46, "High"),
                    Candidate("hihat.pedal", 44, "High")
                },
                new[] { "Ride bow/bell 51/59", "Crash choke 26/46", "Hi-hat continuous CC unresolved", "Crash 2 skipped" },
                new[]
                {
                    Issue("ride.bow", KitMappingReviewIssueKind.Conflict, "Ride bow candidates 51/59 require recapture.", true),
                    Issue("ride.bell", KitMappingReviewIssueKind.Conflict, "Ride bell overlaps observed Ride evidence.", false),
                    Issue("crash1.choke", KitMappingReviewIssueKind.Conflict, "Crash choke strike candidates 26/46 require recapture.", false),
                    Issue("ride.choke", KitMappingReviewIssueKind.Conflict, "Ride choke strike candidates 51/59 require recapture.", false),
                    Issue("hihat.continuous", KitMappingReviewIssueKind.Insufficient, "No continuous hi-hat Control Change was observed.", false),
                    Issue("crash2.bow", KitMappingReviewIssueKind.Insufficient, "Crash 2 capture was skipped.", false)
                });
        }

        private static DeviceProfileMappingCandidate Candidate(string elementId, int note, string confidence) =>
            new DeviceProfileMappingCandidate(elementId, RawMidiMessageKind.NoteOn, 9, note, confidence, true);

        private static KitMappingReviewIssue Issue(
            string elementId,
            KitMappingReviewIssueKind kind,
            string description,
            bool blocksCompletion) =>
            new KitMappingReviewIssue(elementId, kind, description, blocksCompletion);
    }

    public sealed class SimulatedGuidedMidiCaptureSource : IGuidedMidiCaptureSource, ISimulatedGuidedMidiCaptureControl
    {
        private readonly Dictionary<string, RawMidiMessage[]> sequences;
        private int currentIndex;

        public SimulatedGuidedMidiCaptureSource(SimulatedCaptureScenario scenario = SimulatedCaptureScenario.CleanStandardKit)
        {
            Scenario = scenario;
            sequences = BuildSequences(scenario);
            ConnectionState = DeviceConnectionState.Connected;
        }

        public event Action<RawMidiMessage> MessageReceived;
        public event Action<DeviceConnectionState> ConnectionChanged;
        public DeviceConnectionState ConnectionState { get; private set; }
        public bool IsCapturing { get; private set; }
        public string ActiveStepId { get; private set; }
        public SimulatedCaptureScenario Scenario { get; }

        public void SelectDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("Device ID is required.", nameof(deviceId));
        }

        public void Start(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) throw new ArgumentException("Step ID is required.", nameof(stepId));
            if (ConnectionState != DeviceConnectionState.Connected) throw new InvalidOperationException("The simulated device is disconnected.");
            ActiveStepId = stepId;
            currentIndex = 0;
            IsCapturing = true;
        }

        public void Stop()
        {
            IsCapturing = false;
            ActiveStepId = null;
            currentIndex = 0;
        }

        public bool EmitNext()
        {
            if (!IsCapturing || ConnectionState != DeviceConnectionState.Connected) return false;
            RawMidiMessage[] current = GetCurrentSequence();
            if (currentIndex >= current.Length) return false;
            RawMidiMessage message = current[currentIndex++];
            MessageReceived?.Invoke(message);
            if (Scenario == SimulatedCaptureScenario.DisconnectedMidStep && currentIndex == 1) Disconnect();
            return true;
        }

        public int EmitAll()
        {
            int emitted = 0;
            while (EmitNext()) emitted++;
            return emitted;
        }

        public bool HasDataFor(string stepId)
        {
            return !string.IsNullOrWhiteSpace(stepId) && sequences.ContainsKey(stepId);
        }

        public void Disconnect()
        {
            if (ConnectionState == DeviceConnectionState.Disconnected) return;
            ConnectionState = DeviceConnectionState.Disconnected;
            IsCapturing = false;
            ConnectionChanged?.Invoke(ConnectionState);
        }

        public void Reconnect()
        {
            if (ConnectionState == DeviceConnectionState.Connected) return;
            ConnectionState = DeviceConnectionState.Connected;
            ConnectionChanged?.Invoke(ConnectionState);
        }

        private RawMidiMessage[] GetCurrentSequence()
        {
            if (ActiveStepId != null && sequences.TryGetValue(ActiveStepId, out RawMidiMessage[] result)) return result;
            return Array.Empty<RawMidiMessage>();
        }

        private static Dictionary<string, RawMidiMessage[]> BuildSequences(SimulatedCaptureScenario scenario)
        {
            var result = new Dictionary<string, RawMidiMessage[]>(StringComparer.Ordinal)
            {
                ["test-kit"] = Hits(36),
                ["map.kick-default"] = KickHits(36),
                ["map.snare-head"] = Hits(38),
                ["map.snare-rim"] = Hits(40),
                ["map.hihat-closed"] = Hits(42),
                ["map.hihat-open"] = Hits(46),
                ["map.hihat-halfopen"] = Hits(23),
                ["map.hihat-pedal"] = Hits(44),
                ["map.tom1-head"] = Hits(48),
                ["map.tom1-rim"] = Hits(50),
                ["map.tom2-head"] = Hits(45),
                ["map.tom2-rim"] = Hits(47),
                ["map.tom3-head"] = Hits(47),
                ["map.tom4-head"] = Hits(41),
                ["map.floortom-head"] = Hits(43),
                ["map.crash1-bow"] = Hits(49),
                ["map.crash1-edge"] = Hits(57),
                ["map.crash2-bow"] = Hits(55),
                ["map.crash3-bow"] = Hits(57),
                ["map.ride-bow"] = Hits(51),
                ["map.ride-edge"] = Hits(53),
                ["map.ride-bell"] = Hits(59),
                ["map.ride-choke"] = new[] { RawMidiMessage.PolyAftertouch(9, 59, 127) },
                ["map.hihat-continuous"] = new[] { RawMidiMessage.ControlChange(9, 4, 12), RawMidiMessage.ControlChange(9, 4, 98) },
                ["map.crash1-choke"] = new[] { RawMidiMessage.PolyAftertouch(9, 49, 127) },
                ["map.crash2-choke"] = new[] { RawMidiMessage.PolyAftertouch(9, 55, 127) },
            };

            if (scenario == SimulatedCaptureScenario.ContaminatedRide)
                result["map.ride-bow"] = new[] { RawMidiMessage.NoteOn(9, 51, 90), RawMidiMessage.NoteOn(9, 59, 94) };
            if (scenario == SimulatedCaptureScenario.ConflictingTrigger)
                result["map.snare-head"] = Hits(36);
            if (scenario == SimulatedCaptureScenario.MissingHiHatContinuous)
                result["map.hihat-continuous"] = Array.Empty<RawMidiMessage>();
            if (scenario == SimulatedCaptureScenario.HampbackCapture2)
            {
                result["map.kick-default"] = KickHits(36);
                result["map.ride-bow"] = Hits(51);
                result["map.ride-bell"] = Hits(59);
                result["map.crash1-bow"] = Hits(49);
                result["map.crash1-choke"] = new[] { RawMidiMessage.PolyAftertouch(9, 49, 127) };
                result["map.ride-choke"] = new[] { RawMidiMessage.PolyAftertouch(9, 59, 127) };
                result["map.hihat-closed"] = Hits(42);
                result["map.hihat-open"] = Hits(46);
                result["map.hihat-pedal"] = Hits(44);
                result["map.hihat-continuous"] = new[] { RawMidiMessage.ControlChange(9, 4, 8), RawMidiMessage.ControlChange(9, 4, 118) };
            }
            return result;
        }

        private static RawMidiMessage[] Hits(int note) => new[]
        {
            RawMidiMessage.NoteOn(9, note, 84),
            RawMidiMessage.NoteOn(9, note, 108)
        };

        private static RawMidiMessage[] KickHits(int note) => new[]
        {
            RawMidiMessage.NoteOn(9, note, 76),
            RawMidiMessage.NoteOn(9, note, 84),
            RawMidiMessage.NoteOn(9, note, 92),
            RawMidiMessage.NoteOn(9, note, 100),
            RawMidiMessage.NoteOn(9, note, 108)
        };
    }

    public sealed class InMemoryUserKitConfigurationStore : IUserKitConfigurationStore
    {
        private readonly Dictionary<string, string> serialized = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly UserKitConfigurationSerializer serializer = new UserKitConfigurationSerializer();
        private readonly UserKitConfigurationLoader loader = new UserKitConfigurationLoader();

        public void Save(UserKitConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (serialized.ContainsKey(configuration.ConfigurationId))
                throw new InvalidOperationException($"Configuration '{configuration.ConfigurationId}' already exists.");
            serialized.Add(configuration.ConfigurationId, serializer.Serialize(configuration));
        }

        public bool TryLoad(string configurationId, out UserKitConfiguration configuration)
        {
            if (configurationId != null && serialized.TryGetValue(configurationId, out string json))
            {
                configuration = loader.Load(json);
                return true;
            }
            configuration = null;
            return false;
        }

        public IReadOnlyList<UserKitConfiguration> List()
        {
            var keys = new List<string>(serialized.Keys);
            keys.Sort(StringComparer.Ordinal);
            var result = new UserKitConfiguration[keys.Count];
            for (int index = 0; index < keys.Count; index++) result[index] = loader.Load(serialized[keys[index]]);
            return Array.AsReadOnly(result);
        }
    }
}
