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
        private UserKitConfiguration configuration;
        private readonly MidiKitMappingEngine mappingEngine = new MidiKitMappingEngine();
        private readonly MvpDrumInputMapper inputMapper = new MvpDrumInputMapper();
        private readonly MonotonicMidiTimestampMapper timestampMapper = new MonotonicMidiTimestampMapper();

        public event Action<DrumInputEvent> HitReceived;
        public GameplayMidiState State { get; private set; } = GameplayMidiState.Unavailable;
        public string StatusMessage { get; private set; } = "CORE MIDI NON DISPONIBILE";

        private void Start()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (genericProfile == null)
            {
                StatusMessage = "CORE MIDI NON CONFIGURATO";
                return;
            }

            bool usesSavedConfiguration = DeviceSetupConfigurationRuntime.TryLoadComplete(out configuration);
            if (!usesSavedConfiguration)
            {
                ElectronicDrumProfile profile = new ElectronicDrumProfileLoader().Load(genericProfile.text);
                configuration = UserKitConfiguration.FromProfile(profile, "gameplay.generic-gm", "Gameplay Generic GM");
            }
            nativeSession = new CoreMidiNativeSession(new CoreMidiNativeApi());
            if (!nativeSession.IsAvailable)
            {
                StatusMessage = "CORE MIDI PLUGIN ASSENTE";
                return;
            }

            var discovery = new CoreMidiDrumDeviceDiscovery(nativeSession);
            DeviceDiscoverySnapshot snapshot = discovery.Refresh();
            if (snapshot.Devices.Count == 0)
            {
                State = GameplayMidiState.NoDevice;
                StatusMessage = "TASTIERA · NESSUNA BATTERIA MIDI";
                return;
            }
            DrumDeviceDescriptor selectedDevice = null;
            string preferredDeviceId = PlayerPreferencesRuntime.Current.Snapshot.SelectedMidiDeviceId;
            for (int index = 0; index < snapshot.Devices.Count; index++)
                if (string.Equals(snapshot.Devices[index].Id, preferredDeviceId, StringComparison.Ordinal))
                    selectedDevice = snapshot.Devices[index];
            if (selectedDevice == null && snapshot.Devices.Count == 1) selectedDevice = snapshot.Devices[0];
            if (selectedDevice == null)
            {
                State = GameplayMidiState.Ambiguous;
                StatusMessage = $"TASTIERA · {snapshot.Devices.Count} MIDI (SELEZIONA IN CONFIGURA)";
                return;
            }

            capture = new CoreMidiGuidedMidiCaptureSource(nativeSession);
            capture.MessageReceived += HandleMessage;
            capture.ConnectionChanged += HandleConnection;
            capture.SelectDevice(selectedDevice.Id);
            capture.Start("gameplay");
            State = capture.IsCapturing ? GameplayMidiState.Connected : GameplayMidiState.Disconnected;
            StatusMessage = capture.IsCapturing
                ? $"TASTIERA + MIDI · {selectedDevice.DisplayName}" +
                  (usesSavedConfiguration ? " · CONFIGURAZIONE PERSONALE" : " · PROFILO GENERICO")
                : "TASTIERA · MIDI DISCONNESSO";
#else
            StatusMessage = "TASTIERA · CORE MIDI SOLO SU MACOS";
#endif
        }

        private void Update()
        {
            capture?.Poll();
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
            MidiKitMappingResult mapped = mappingEngine.Map(message, configuration);
            if (mapped.Status != MidiKitMappingStatus.Mapped) return;
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
