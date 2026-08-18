using System;
using System.IO;
using HitTheKit.Core;
using HitTheKit.Unity.Input;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public enum PlayerLanguage
    {
        Italian,
        English
    }

    public sealed class PlayerPreferencesSnapshot
    {
        internal PlayerPreferencesSnapshot(PlayerPreferencesDocument document)
        {
            KeyboardOffsetSeconds = document.keyboardOffsetSeconds;
            MidiOffsetSeconds = document.midiOffsetSeconds;
            AudioMuted = document.audioMuted;
            MasterVolume = document.masterVolume;
            ReducedMotion = document.reducedMotion;
            HighContrast = document.highContrast;
            ShowTimingDiagnostics = document.showTimingDiagnostics;
            MetronomeEnabled = document.metronomeEnabled;
            Language = ParseLanguage(document.language);
            Fullscreen = document.fullscreen;
            WindowWidth = document.windowWidth;
            WindowHeight = document.windowHeight;
            FirstRunCompleted = document.firstRunCompleted;
            SelectedMidiDeviceId = EmptyToNull(document.selectedMidiDeviceId);
            KickKey = ParseKey(document.kickKey);
            SnareKey = ParseKey(document.snareKey);
            HiHatKey = ParseKey(document.hiHatKey);
            Tom1Key = ParseKey(document.tom1Key);
            Tom2Key = ParseKey(document.tom2Key);
            FloorTomKey = ParseKey(document.floorTomKey);
            CrashKey = ParseKey(document.crashKey);
            RideKey = ParseKey(document.rideKey);
        }

        public double KeyboardOffsetSeconds { get; }
        public double MidiOffsetSeconds { get; }
        public bool AudioMuted { get; }
        public float MasterVolume { get; }
        public bool ReducedMotion { get; }
        public bool HighContrast { get; }
        public bool ShowTimingDiagnostics { get; }
        public bool MetronomeEnabled { get; }
        public PlayerLanguage Language { get; }
        public bool Fullscreen { get; }
        public int WindowWidth { get; }
        public int WindowHeight { get; }
        public bool FirstRunCompleted { get; }
        public string SelectedMidiDeviceId { get; }
        public KeyCode KickKey { get; }
        public KeyCode SnareKey { get; }
        public KeyCode HiHatKey { get; }
        public KeyCode Tom1Key { get; }
        public KeyCode Tom2Key { get; }
        public KeyCode FloorTomKey { get; }
        public KeyCode CrashKey { get; }
        public KeyCode RideKey { get; }

        public double OffsetFor(DrumInputSource source) =>
            source == DrumInputSource.Midi ? MidiOffsetSeconds : KeyboardOffsetSeconds;

        public KeyCode KeyFor(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return KickKey;
                case DrumPad.Snare: return SnareKey;
                case DrumPad.HiHat: return HiHatKey;
                case DrumPad.Tom1: return Tom1Key;
                case DrumPad.Tom2: return Tom2Key;
                case DrumPad.FloorTom: return FloorTomKey;
                case DrumPad.Crash: return CrashKey;
                case DrumPad.Ride: return RideKey;
                default: throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        private static KeyCode ParseKey(string value) =>
            Enum.TryParse(value, false, out KeyCode key) ? key : KeyCode.None;

        private static PlayerLanguage ParseLanguage(string value) =>
            Enum.TryParse(value, false, out PlayerLanguage language) ? language : PlayerLanguage.Italian;

        private static string EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public sealed class PlayerPreferencesService
    {
        public const int SupportedSchemaVersion = 1;
        public const double MaximumOffsetSeconds = 0.250;
        private readonly IGameplaySettingsPersistence persistence;
        private PlayerPreferencesDocument document = PlayerPreferencesDocument.Default();

        public PlayerPreferencesService(IGameplaySettingsPersistence persistence)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            try
            {
                if (persistence.TryLoad(out string json)) document = Deserialize(json);
            }
            catch (Exception exception)
            {
                document = PlayerPreferencesDocument.Default();
                LastError = $"Saved preferences could not be loaded: {exception.Message}";
            }
        }

        public PlayerPreferencesSnapshot Snapshot => new PlayerPreferencesSnapshot(document);
        public string LastError { get; private set; } = string.Empty;

        public void SetInputOffset(DrumInputSource source, double seconds)
        {
            ValidateOffset(seconds);
            if (source == DrumInputSource.Midi) document.midiOffsetSeconds = seconds;
            else if (source == DrumInputSource.Keyboard) document.keyboardOffsetSeconds = seconds;
            else throw new ArgumentOutOfRangeException(nameof(source));
            Save();
        }

        public void SetAudioMuted(bool value) { document.audioMuted = value; Save(); }
        public void SetMasterVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            document.masterVolume = value;
            Save();
        }
        public void SetReducedMotion(bool value) { document.reducedMotion = value; Save(); }
        public void SetHighContrast(bool value) { document.highContrast = value; Save(); }
        public void SetShowTimingDiagnostics(bool value) { document.showTimingDiagnostics = value; Save(); }
        public void SetMetronomeEnabled(bool value) { document.metronomeEnabled = value; Save(); }
        public void SetLanguage(PlayerLanguage value)
        {
            if (!Enum.IsDefined(typeof(PlayerLanguage), value)) throw new ArgumentOutOfRangeException(nameof(value));
            document.language = value.ToString();
            Save();
        }
        public void SetFirstRunCompleted(bool value) { document.firstRunCompleted = value; Save(); }
        public void SelectMidiDevice(string deviceId)
        {
            if (deviceId != null && (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length > 256))
                throw new ArgumentException("MIDI device ID is invalid.", nameof(deviceId));
            document.selectedMidiDeviceId = deviceId ?? string.Empty;
            Save();
        }
        public void SetDisplay(bool fullscreen, int width, int height)
        {
            if (width < 960 || width > 7680) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 540 || height > 4320) throw new ArgumentOutOfRangeException(nameof(height));
            document.fullscreen = fullscreen;
            document.windowWidth = width;
            document.windowHeight = height;
            Save();
        }
        public void SetKeyBinding(DrumPad pad, KeyCode key)
        {
            if (key == KeyCode.None) throw new ArgumentOutOfRangeException(nameof(key));
            foreach (DrumPad candidate in Enum.GetValues(typeof(DrumPad)))
                if (candidate != pad && Snapshot.KeyFor(candidate) == key)
                    throw new InvalidOperationException($"Key '{key}' is already assigned to {candidate}.");
            SetKey(document, pad, key.ToString());
            Save();
        }

        public void Reset()
        {
            document = PlayerPreferencesDocument.Default();
            Save();
        }

        public void ApplyAudio()
        {
            PlayerPreferencesSnapshot snapshot = Snapshot;
            AudioListener.volume = snapshot.AudioMuted ? 0f : snapshot.MasterVolume;
        }

        public void ApplyDisplay()
        {
            PlayerPreferencesSnapshot snapshot = Snapshot;
            Screen.fullScreen = snapshot.Fullscreen;
            if (!snapshot.Fullscreen)
                Screen.SetResolution(snapshot.WindowWidth, snapshot.WindowHeight, FullScreenMode.Windowed);
        }

        internal static string Serialize(PlayerPreferencesDocument value) =>
            JsonUtility.ToJson(value ?? throw new ArgumentNullException(nameof(value)), true) + "\n";

        internal static PlayerPreferencesDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"schemaVersion\""))
                throw new InvalidOperationException("Preferences JSON is empty or missing schemaVersion.");
            PlayerPreferencesDocument value;
            try { value = JsonUtility.FromJson<PlayerPreferencesDocument>(json); }
            catch (Exception exception) { throw new InvalidOperationException("Preferences JSON is malformed.", exception); }
            if (value == null || value.schemaVersion != SupportedSchemaVersion)
                throw new InvalidOperationException("Preferences schema version is unsupported.");
            Validate(value);
            return value;
        }

        private void Save()
        {
            Validate(document);
            persistence.Save(Serialize(document));
            LastError = string.Empty;
        }

        private static void Validate(PlayerPreferencesDocument value)
        {
            ValidateOffset(value.keyboardOffsetSeconds);
            ValidateOffset(value.midiOffsetSeconds);
            if (float.IsNaN(value.masterVolume) || float.IsInfinity(value.masterVolume) || value.masterVolume < 0 || value.masterVolume > 1)
                throw new InvalidOperationException("Master volume is outside the supported range.");
            if (!Enum.TryParse(value.language, false, out PlayerLanguage _))
                throw new InvalidOperationException("Language is invalid.");
            if (value.windowWidth < 960 || value.windowWidth > 7680 || value.windowHeight < 540 || value.windowHeight > 4320)
                throw new InvalidOperationException("Window size is outside the supported range.");
            var seen = new System.Collections.Generic.HashSet<KeyCode>();
            foreach (DrumPad pad in Enum.GetValues(typeof(DrumPad)))
            {
                KeyCode key = new PlayerPreferencesSnapshot(value).KeyFor(pad);
                if (key == KeyCode.None || !seen.Add(key))
                    throw new InvalidOperationException("Keyboard bindings are invalid or duplicated.");
            }
        }

        private static void ValidateOffset(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > MaximumOffsetSeconds)
                throw new ArgumentOutOfRangeException(nameof(value), "Input offset must be between -250 and +250 ms.");
        }

        private static void SetKey(PlayerPreferencesDocument value, DrumPad pad, string key)
        {
            switch (pad)
            {
                case DrumPad.Kick: value.kickKey = key; break;
                case DrumPad.Snare: value.snareKey = key; break;
                case DrumPad.HiHat: value.hiHatKey = key; break;
                case DrumPad.Tom1: value.tom1Key = key; break;
                case DrumPad.Tom2: value.tom2Key = key; break;
                case DrumPad.FloorTom: value.floorTomKey = key; break;
                case DrumPad.Crash: value.crashKey = key; break;
                case DrumPad.Ride: value.rideKey = key; break;
                default: throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }
    }

    [Serializable]
    public sealed class PlayerPreferencesDocument
    {
        public int schemaVersion;
        public double keyboardOffsetSeconds;
        public double midiOffsetSeconds;
        public bool audioMuted;
        public float masterVolume;
        public bool reducedMotion;
        public bool highContrast;
        public bool showTimingDiagnostics;
        public bool metronomeEnabled;
        public string language;
        public bool fullscreen;
        public int windowWidth;
        public int windowHeight;
        public bool firstRunCompleted;
        public string selectedMidiDeviceId;
        public string kickKey;
        public string snareKey;
        public string hiHatKey;
        public string tom1Key;
        public string tom2Key;
        public string floorTomKey;
        public string crashKey;
        public string rideKey;

        public static PlayerPreferencesDocument Default() => new PlayerPreferencesDocument
        {
            schemaVersion = PlayerPreferencesService.SupportedSchemaVersion,
            masterVolume = 1f,
            showTimingDiagnostics = true,
            language = PlayerLanguage.Italian.ToString(),
            fullscreen = true,
            windowWidth = 1920,
            windowHeight = 1080,
            selectedMidiDeviceId = string.Empty,
            kickKey = KeyCode.F.ToString(),
            snareKey = KeyCode.J.ToString(),
            hiHatKey = KeyCode.K.ToString(),
            tom1Key = KeyCode.G.ToString(),
            tom2Key = KeyCode.H.ToString(),
            floorTomKey = KeyCode.L.ToString(),
            crashKey = KeyCode.D.ToString(),
            rideKey = KeyCode.S.ToString()
        };
    }

    public static class PlayerPreferencesRuntime
    {
        private static PlayerPreferencesService current;

        public static PlayerPreferencesService Current => current ??= new PlayerPreferencesService(
            new AtomicJsonFileGameplaySettingsPersistence(DefaultPath));
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "HitTheKit", "player-preferences.json");

        public static void UseForTests(PlayerPreferencesService service) =>
            current = service ?? throw new ArgumentNullException(nameof(service));
        public static void ResetForTests() => current = null;
    }
}
