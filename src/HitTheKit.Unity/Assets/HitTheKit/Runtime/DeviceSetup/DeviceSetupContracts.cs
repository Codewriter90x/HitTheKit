using System;
using System.Collections.Generic;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public sealed class DeviceProfileMappingCandidate
    {
        public DeviceProfileMappingCandidate(string elementId, RawMidiMessageKind kind, int channel, int data1, string confidence, bool requiresConfirmation)
        {
            KitElement.EnsureStableId(elementId, nameof(elementId));
            if (!Enum.IsDefined(typeof(RawMidiMessageKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (channel < 0 || channel > 15) throw new ArgumentOutOfRangeException(nameof(channel));
            if (data1 < 0 || data1 > 127) throw new ArgumentOutOfRangeException(nameof(data1));
            if (string.IsNullOrWhiteSpace(confidence)) throw new ArgumentException("Confidence is required.", nameof(confidence));
            ElementId = elementId;
            Kind = kind;
            Channel = channel;
            Data1 = data1;
            Confidence = confidence;
            RequiresConfirmation = requiresConfirmation;
        }

        public string ElementId { get; }
        public RawMidiMessageKind Kind { get; }
        public int Channel { get; }
        public int Data1 { get; }
        public string Confidence { get; }
        public bool RequiresConfirmation { get; }
        public string TriggerLabel => $"{Kind} ch{Channel + 1} data{Data1}";
    }

    public enum DeviceConnectionState
    {
        Connected,
        Disconnected
    }

    public enum DeviceDiscoveryState
    {
        Idle,
        Refreshing,
        Ready,
        Failed
    }

    public sealed class DeviceProfileOption
    {
        private readonly IReadOnlyList<DeviceProfileMappingCandidate> candidateMappings;
        private readonly IReadOnlyList<string> unresolvedMappings;
        private readonly IReadOnlyList<KitMappingReviewIssue> reviewIssues;

        public DeviceProfileOption(
            string id,
            string displayName,
            string status,
            string origin,
            string reason,
            bool productionReady,
            bool autoSelectable,
            bool requiresConfirmation,
            IReadOnlyList<DeviceProfileMappingCandidate> candidateMappings = null,
            IReadOnlyList<string> unresolvedMappings = null,
            IReadOnlyList<KitMappingReviewIssue> reviewIssues = null)
        {
            KitElement.EnsureStableId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", nameof(status));
            Id = id;
            DisplayName = displayName;
            Status = status;
            Origin = string.IsNullOrWhiteSpace(origin) ? "Built-in simulation" : origin;
            Reason = reason ?? string.Empty;
            ProductionReady = productionReady;
            AutoSelectable = autoSelectable;
            RequiresConfirmation = requiresConfirmation;
            this.candidateMappings = Copy(candidateMappings);
            this.unresolvedMappings = CopyStrings(unresolvedMappings);
            this.reviewIssues = CopyIssues(reviewIssues);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Status { get; }
        public string Origin { get; }
        public string Reason { get; }
        public bool ProductionReady { get; }
        public bool AutoSelectable { get; }
        public bool RequiresConfirmation { get; }
        public IReadOnlyList<DeviceProfileMappingCandidate> CandidateMappings => candidateMappings;
        public IReadOnlyList<string> UnresolvedMappings => unresolvedMappings;
        public IReadOnlyList<KitMappingReviewIssue> ReviewIssues => reviewIssues;
        public bool CanUseAsStartingPoint => RequiresConfirmation || ProductionReady;

        public DeviceProfileMappingCandidate CandidateFor(string elementId)
        {
            for (int index = 0; index < candidateMappings.Count; index++)
                if (string.Equals(candidateMappings[index].ElementId, elementId, StringComparison.Ordinal)) return candidateMappings[index];
            return null;
        }

        private static IReadOnlyList<DeviceProfileMappingCandidate> Copy(IReadOnlyList<DeviceProfileMappingCandidate> source)
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<DeviceProfileMappingCandidate>());
            var copy = new DeviceProfileMappingCandidate[source.Count];
            for (int index = 0; index < source.Count; index++) copy[index] = source[index] ?? throw new ArgumentException("Candidate mapping is null.", nameof(source));
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<string>());
            var copy = new string[source.Count];
            for (int index = 0; index < source.Count; index++) copy[index] = source[index] ?? string.Empty;
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<KitMappingReviewIssue> CopyIssues(IReadOnlyList<KitMappingReviewIssue> source)
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<KitMappingReviewIssue>());
            var copy = new KitMappingReviewIssue[source.Count];
            for (int index = 0; index < source.Count; index++)
                copy[index] = source[index] ?? throw new ArgumentException("Review issue is null.", nameof(source));
            return Array.AsReadOnly(copy);
        }
    }

    public sealed class DrumDeviceDescriptor
    {
        private readonly IReadOnlyList<DeviceProfileOption> profiles;

        public DrumDeviceDescriptor(
            string id,
            string displayName,
            string manufacturer,
            string portName,
            DeviceConnectionState connectionState,
            IReadOnlyList<DeviceProfileOption> profiles)
        {
            KitElement.EnsureStableId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(portName)) throw new ArgumentException("Port name is required.", nameof(portName));
            if (!Enum.IsDefined(typeof(DeviceConnectionState), connectionState)) throw new ArgumentOutOfRangeException(nameof(connectionState));
            var copy = new DeviceProfileOption[profiles?.Count ?? 0];
            for (int index = 0; index < copy.Length; index++) copy[index] = profiles[index] ?? throw new ArgumentException("Profile is null.", nameof(profiles));
            Id = id;
            DisplayName = displayName;
            Manufacturer = manufacturer ?? string.Empty;
            PortName = portName;
            ConnectionState = connectionState;
            this.profiles = Array.AsReadOnly(copy);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Manufacturer { get; }
        public string PortName { get; }
        public DeviceConnectionState ConnectionState { get; }
        public IReadOnlyList<DeviceProfileOption> Profiles => profiles;
    }

    public sealed class DeviceDiscoverySnapshot
    {
        private readonly IReadOnlyList<DrumDeviceDescriptor> devices;

        public DeviceDiscoverySnapshot(DeviceDiscoveryState state, IReadOnlyList<DrumDeviceDescriptor> devices, string message = null)
        {
            if (!Enum.IsDefined(typeof(DeviceDiscoveryState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            var copy = new DrumDeviceDescriptor[devices?.Count ?? 0];
            for (int index = 0; index < copy.Length; index++) copy[index] = devices[index] ?? throw new ArgumentException("Device is null.", nameof(devices));
            State = state;
            this.devices = Array.AsReadOnly(copy);
            Message = message;
        }

        public DeviceDiscoveryState State { get; }
        public IReadOnlyList<DrumDeviceDescriptor> Devices => devices;
        public string Message { get; }
    }

    public interface IDrumDeviceDiscovery
    {
        DeviceDiscoverySnapshot Refresh();
    }

    public interface IGuidedMidiCaptureSource
    {
        event Action<RawMidiMessage> MessageReceived;
        event Action<DeviceConnectionState> ConnectionChanged;
        DeviceConnectionState ConnectionState { get; }
        bool IsCapturing { get; }
        string ActiveStepId { get; }
        void SelectDevice(string deviceId);
        void Start(string stepId);
        void Stop();
    }

    public interface ISimulatedGuidedMidiCaptureControl
    {
        bool HasDataFor(string stepId);
        int EmitAll();
    }

    public interface IUserKitConfigurationStore
    {
        void Save(UserKitConfiguration configuration);
        bool TryLoad(string configurationId, out UserKitConfiguration configuration);
        IReadOnlyList<UserKitConfiguration> List();
    }

    public enum DeviceSetupLanguage
    {
        Italian,
        English
    }

    public interface ILocalizedTextProvider
    {
        DeviceSetupLanguage Language { get; set; }
        string Get(string key, string fallback = null);
    }
}
