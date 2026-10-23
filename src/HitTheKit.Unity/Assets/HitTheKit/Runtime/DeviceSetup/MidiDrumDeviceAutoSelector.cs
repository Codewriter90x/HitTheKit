using System;
using System.Collections.Generic;
using HitTheKit.Unity.Devices;

namespace HitTheKit.Unity.DeviceSetup
{
    public enum MidiDrumDeviceSelectionKind
    {
        NoDevice,
        Preferred,
        RecognizedProfile,
        OnlyCandidate,
        Probe
    }

    public sealed class MidiDrumDeviceSelection
    {
        internal MidiDrumDeviceSelection(
            MidiDrumDeviceSelectionKind kind,
            DrumDeviceDescriptor device,
            IReadOnlyList<DrumDeviceDescriptor> probeCandidates)
        {
            Kind = kind;
            Device = device;
            ProbeCandidates = probeCandidates ?? Array.AsReadOnly(Array.Empty<DrumDeviceDescriptor>());
        }

        public MidiDrumDeviceSelectionKind Kind { get; }
        public DrumDeviceDescriptor Device { get; }
        public IReadOnlyList<DrumDeviceDescriptor> ProbeCandidates { get; }
    }

    /// <summary>
    /// Chooses which MIDI input to use for gameplay without asking the player. Order:
    /// the remembered device, a device with a recognized drum profile, the only plausible
    /// drum input, otherwise a probe across the plausible inputs (see <see cref="MidiDrumDeviceProbe"/>).
    /// Virtual and network ports (IAC driver, network sessions) are never treated as drum kits
    /// unless they are the remembered choice.
    /// </summary>
    public static class MidiDrumDeviceAutoSelector
    {
        private static readonly string[] VirtualPortMarkers =
        {
            "IAC Driver",
            "IAC Bus",
            "Network Session",
            "Network MIDI",
            "loopMIDI",
            "Midi Through"
        };

        public static MidiDrumDeviceSelection Select(IReadOnlyList<DrumDeviceDescriptor> devices, string preferredDeviceId)
        {
            if (devices == null) throw new ArgumentNullException(nameof(devices));

            var connected = new List<DrumDeviceDescriptor>();
            for (int index = 0; index < devices.Count; index++)
            {
                DrumDeviceDescriptor device = devices[index];
                if (device == null || device.ConnectionState != DeviceConnectionState.Connected) continue;
                if (!string.IsNullOrEmpty(preferredDeviceId) && string.Equals(device.Id, preferredDeviceId, StringComparison.Ordinal))
                    return new MidiDrumDeviceSelection(MidiDrumDeviceSelectionKind.Preferred, device, null);
                connected.Add(device);
            }

            var plausible = new List<DrumDeviceDescriptor>();
            foreach (DrumDeviceDescriptor device in connected)
                if (!IsVirtualPort(device)) plausible.Add(device);

            var recognized = new List<DrumDeviceDescriptor>();
            foreach (DrumDeviceDescriptor device in plausible)
                if (device.Profiles.Count > 0) recognized.Add(device);
            if (recognized.Count == 1)
                return new MidiDrumDeviceSelection(MidiDrumDeviceSelectionKind.RecognizedProfile, recognized[0], null);

            if (plausible.Count == 0)
                return new MidiDrumDeviceSelection(MidiDrumDeviceSelectionKind.NoDevice, null, null);
            if (plausible.Count == 1)
                return new MidiDrumDeviceSelection(MidiDrumDeviceSelectionKind.OnlyCandidate, plausible[0], null);

            List<DrumDeviceDescriptor> probe = recognized.Count > 1 ? recognized : plausible;
            return new MidiDrumDeviceSelection(MidiDrumDeviceSelectionKind.Probe, null, probe.AsReadOnly());
        }

        public static bool IsVirtualPort(DrumDeviceDescriptor device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            foreach (string marker in VirtualPortMarkers)
            {
                if (Contains(device.DisplayName, marker) || Contains(device.PortName, marker) || Contains(device.Manufacturer, marker))
                    return true;
            }
            return false;
        }

        private static bool Contains(string value, string marker) =>
            !string.IsNullOrEmpty(value) && value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// The native CoreMIDI bridge listens to one input at a time, so when several inputs are
    /// plausible the gameplay input rotates through them until a positive NoteOn arrives, then
    /// locks onto that input.
    /// </summary>
    public sealed class MidiDrumDeviceProbe
    {
        public const double DefaultDwellSeconds = 0.75;

        private readonly IReadOnlyList<DrumDeviceDescriptor> candidates;
        private readonly double dwellSeconds;
        private double currentSince;
        private int currentIndex;

        public MidiDrumDeviceProbe(IReadOnlyList<DrumDeviceDescriptor> candidates, double startSeconds, double dwellSeconds = DefaultDwellSeconds)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (candidates.Count == 0) throw new ArgumentException("At least one candidate is required.", nameof(candidates));
            if (double.IsNaN(dwellSeconds) || double.IsInfinity(dwellSeconds) || dwellSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(dwellSeconds));
            this.candidates = candidates;
            this.dwellSeconds = dwellSeconds;
            currentSince = startSeconds;
        }

        public DrumDeviceDescriptor Current => candidates[currentIndex];
        public DrumDeviceDescriptor LockedDevice { get; private set; }
        public bool IsLocked => LockedDevice != null;
        public int CandidateCount => candidates.Count;

        /// <summary>Returns true when the caller must switch the open input to <see cref="Current"/>.</summary>
        public bool Advance(double nowSeconds)
        {
            if (IsLocked || candidates.Count == 1) return false;
            if (nowSeconds - currentSince < dwellSeconds) return false;
            currentIndex = (currentIndex + 1) % candidates.Count;
            currentSince = nowSeconds;
            return true;
        }

        public bool ReportMessage(RawMidiMessage message)
        {
            if (IsLocked || message == null) return false;
            if (message.SemanticKind != RawMidiMessageKind.NoteOn || message.Value <= 0) return false;
            LockedDevice = Current;
            return true;
        }
    }
}
