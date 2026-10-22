using System;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Devices;
using HitTheKit.Unity.Gameplay;
using UnityEngine;

namespace HitTheKit.Unity.Input
{
    public enum GameplayMidiState
    {
        Unavailable,
        NoDevice,
        Ambiguous,
        Connected,
        Disconnected
    }

    [DefaultExecutionOrder(-200)]
    public sealed class CoreMidiGameplayInput : MonoBehaviour, IDrumInput
    {
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private TextAsset genericProfile;
        private CoreMidiNativeSession nativeSession;
        private CoreMidiGuidedMidiCaptureSource capture;
        private GameplayKitMappingResolver resolver;
        private MidiDrumDeviceProbe probe;
        private string configurationLabel;
        private readonly MvpDrumInputMapper inputMapper = new MvpDrumInputMapper();
        private readonly MonotonicMidiTimestampMapper timestampMapper = new MonotonicMidiTimestampMapper();

        public event Action<DrumInputEvent> HitReceived;
        public GameplayMidiState State { get; private set; } = GameplayMidiState.Unavailable;
        public string StatusMessage { get; private set; } = "CORE MIDI NON DISPONIBILE";

        private void Start()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (genericProfile == null)
            {
                StatusMessage = "CORE MIDI NON CONFIGURATO";
                return;
            }

            ElectronicDrumProfile profile = new ElectronicDrumProfileLoader().Load(genericProfile.text);
            UserKitConfiguration generic = UserKitConfiguration.FromProfile(profile, "gameplay.generic-gm", "Gameplay Generic GM");
            bool usesSavedConfiguration = DeviceSetupConfigurationRuntime.TryLoadComplete(out UserKitConfiguration saved);
            // A saved configuration wins, but notes it does not know still resolve through the generic profile.
            resolver = new GameplayKitMappingResolver(usesSavedConfiguration ? saved : generic, generic);
            configurationLabel = usesSavedConfiguration ? "CONFIGURAZIONE PERSONALE" : "PROFILO GENERICO";

            nativeSession = new CoreMidiNativeSession(NativeMidiApiFactory.Create());
            if (!nativeSession.IsAvailable)
            {
                #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                StatusMessage = "MIDI WINDOWS NON DISPONIBILE";
#else
                StatusMessage = "CORE MIDI PLUGIN ASSENTE";
#endif
                return;
            }

            var discovery = new CoreMidiDrumDeviceDiscovery(nativeSession, DeviceSetupProfileCatalog.ForCoreMidiDevice);
            DeviceDiscoverySnapshot snapshot = discovery.Refresh();
            string preferredDeviceId = PlayerPreferencesRuntime.Current.Snapshot.SelectedMidiDeviceId;
            MidiDrumDeviceSelection selection = MidiDrumDeviceAutoSelector.Select(snapshot.Devices, preferredDeviceId);
            if (selection.Kind == MidiDrumDeviceSelectionKind.NoDevice)
            {
                State = GameplayMidiState.NoDevice;
                StatusMessage = "TASTIERA · NESSUNA BATTERIA MIDI";
                return;
            }

            capture = new CoreMidiGuidedMidiCaptureSource(nativeSession);
            capture.MessageReceived += HandleMessage;
            capture.ConnectionChanged += HandleConnection;

            if (selection.Kind == MidiDrumDeviceSelectionKind.Probe)
            {
                probe = new MidiDrumDeviceProbe(selection.ProbeCandidates, Time.unscaledTimeAsDouble);
                OpenDevice(probe.Current);
                if (capture.IsCapturing) StatusMessage = "TASTIERA + MIDI · COLPISCI UN PAD PER RICONOSCERE LA BATTERIA";
                return;
            }

            // Preferred, recognized or single inputs are re-derived on every launch; only a probe
            // result is remembered, because it is the one choice the player had to make by playing.
            OpenDevice(selection.Device);
#else
            StatusMessage = "TASTIERA · MIDI NON SUPPORTATO SU QUESTO SISTEMA";
#endif
        }

        private void Update()
        {
            if (probe != null && !probe.IsLocked && probe.Advance(Time.unscaledTimeAsDouble))
            {
                OpenDevice(probe.Current);
                if (capture.IsCapturing) StatusMessage = "TASTIERA + MIDI · COLPISCI UN PAD PER RICONOSCERE LA BATTERIA";
            }
            capture?.Poll();
        }

        public GameplayKitMappingResolver MappingResolver => resolver;

        private void OpenDevice(DrumDeviceDescriptor device)
        {
            capture.SelectDevice(device.Id);
            capture.Start("gameplay");
            timestampMapper.Reset();
            State = capture.IsCapturing ? GameplayMidiState.Connected : GameplayMidiState.Disconnected;
            StatusMessage = capture.IsCapturing
                ? $"TASTIERA + MIDI · {device.DisplayName} · {configurationLabel}"
                : "TASTIERA · MIDI DISCONNESSO";
        }

        private static void RememberDevice(DrumDeviceDescriptor device)
        {
            try
            {
                PlayerPreferencesRuntime.Current.SelectMidiDevice(device.Id);
            }
            catch (Exception)
            {
                // Remembering the device is a convenience; gameplay must continue if preferences cannot be saved.
            }
        }

        private void OnDestroy()
        {
            if (capture != null)
            {
                capture.MessageReceived -= HandleMessage;
                capture.ConnectionChanged -= HandleConnection;
                capture.Dispose();
                capture = null;
            }
            nativeSession?.Dispose();
            nativeSession = null;
        }

        private void HandleConnection(DeviceConnectionState state)
        {
            if (state == DeviceConnectionState.Disconnected) timestampMapper.Reset();
            State = state == DeviceConnectionState.Connected ? GameplayMidiState.Connected : GameplayMidiState.Disconnected;
            if (State == GameplayMidiState.Disconnected) StatusMessage = "TASTIERA · MIDI DISCONNESSO";
        }

        private void HandleMessage(RawMidiMessage nativeMessage)
        {
            if (nativeMessage == null) return;
            if (songClock != null && (songClock.Clock == null || songClock.Clock.IsPaused)) return;
            double songPosition = songClock == null ? Time.unscaledTimeAsDouble : songClock.PositionSeconds;
            double timestamp = songPosition;
            if (nativeMessage.TimestampSeconds.HasValue)
            {
                double eventMonotonicSeconds = nativeMessage.TimestampSeconds.Value;
                double currentMonotonicSeconds = nativeSession.Api.GetMonotonicSeconds();
                if (IsFinite(eventMonotonicSeconds) && IsFinite(currentMonotonicSeconds))
                    timestamp = timestampMapper.Map(eventMonotonicSeconds, currentMonotonicSeconds, songPosition);
            }
            RawMidiMessage message = WithTimestamp(nativeMessage, timestamp);
            if (probe != null && probe.ReportMessage(message))
            {
                RememberDevice(probe.LockedDevice);
                StatusMessage = $"TASTIERA + MIDI · {probe.LockedDevice.DisplayName} · {configurationLabel}";
            }
            GameplayKitMappingResolution resolution = resolver.Resolve(message);
            if (!resolution.IsMapped) return;
            MidiKitMappingResult mapped = resolution.Result;
            if (inputMapper.Map(mapped.Hit, true, out DrumInputEvent input) == MvpDrumInputMappingStatus.Mapped)
                HitReceived?.Invoke(input);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static RawMidiMessage WithTimestamp(RawMidiMessage message, double timestamp)
        {
            switch (message.Kind)
            {
                case RawMidiMessageKind.NoteOn: return RawMidiMessage.NoteOn(message.Channel, message.Data1.Value, message.Value, timestamp);
                case RawMidiMessageKind.NoteOff: return RawMidiMessage.NoteOff(message.Channel, message.Data1.Value, message.Value, timestamp);
                case RawMidiMessageKind.ControlChange: return RawMidiMessage.ControlChange(message.Channel, message.Data1.Value, message.Value, timestamp);
                case RawMidiMessageKind.PolyAftertouch: return RawMidiMessage.PolyAftertouch(message.Channel, message.Data1.Value, message.Value, timestamp);
                case RawMidiMessageKind.ChannelAftertouch: return RawMidiMessage.ChannelAftertouch(message.Channel, message.Value, timestamp);
                case RawMidiMessageKind.PitchBend: return RawMidiMessage.PitchBend(message.Channel, message.Value, timestamp);
                case RawMidiMessageKind.ProgramChange: return RawMidiMessage.ProgramChange(message.Channel, message.Data1.Value, timestamp);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
