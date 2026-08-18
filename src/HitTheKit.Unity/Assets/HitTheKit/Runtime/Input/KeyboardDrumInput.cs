using System;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Gameplay;
using UnityEngine;

namespace HitTheKit.Unity.Input
{
    [DefaultExecutionOrder(-200)]
    public sealed class KeyboardDrumInput : MonoBehaviour, IDrumInput
    {
        public const int KeyboardVelocity = 100;

        [SerializeField] private DspSongClockPrototype songClock;

        public event Action<DrumInputEvent> HitReceived;

        public DspSongClockPrototype SongClock => songClock;

        private void Update()
        {
            if (songClock == null || songClock.Clock == null || !songClock.Clock.IsScheduled || songClock.Clock.IsPaused)
            {
                return;
            }

            double songTimeSeconds = songClock.PositionSeconds;
            PlayerPreferencesSnapshot preferences = PlayerPreferencesRuntime.Current.Snapshot;
            EmitIfPressed(DrumPad.Kick, preferences.KickKey, songTimeSeconds);
            EmitIfPressed(DrumPad.Snare, preferences.SnareKey, songTimeSeconds);
            EmitIfPressed(DrumPad.HiHat, preferences.HiHatKey, songTimeSeconds);
            EmitIfPressed(DrumPad.Tom1, preferences.Tom1Key, songTimeSeconds);
            EmitIfPressed(DrumPad.Tom2, preferences.Tom2Key, songTimeSeconds);
            EmitIfPressed(DrumPad.FloorTom, preferences.FloorTomKey, songTimeSeconds);
            EmitIfPressed(DrumPad.Crash, preferences.CrashKey, songTimeSeconds);
            EmitIfPressed(DrumPad.Ride, preferences.RideKey, songTimeSeconds);
        }

        public static bool TryMapKey(KeyCode key, out DrumPad pad)
        {
            switch (key)
            {
                case KeyCode.F:
                    pad = DrumPad.Kick;
                    return true;
                case KeyCode.J:
                    pad = DrumPad.Snare;
                    return true;
                case KeyCode.K:
                    pad = DrumPad.HiHat;
                    return true;
                case KeyCode.G:
                    pad = DrumPad.Tom1;
                    return true;
                case KeyCode.H:
                    pad = DrumPad.Tom2;
                    return true;
                case KeyCode.L:
                    pad = DrumPad.FloorTom;
                    return true;
                case KeyCode.D:
                    pad = DrumPad.Crash;
                    return true;
                case KeyCode.S:
                    pad = DrumPad.Ride;
                    return true;
                default:
                    pad = default;
                    return false;
            }
        }

        private void EmitIfPressed(DrumPad pad, KeyCode key, double songTimeSeconds)
        {
            if (UnityEngine.Input.GetKeyDown(key))
                HitReceived?.Invoke(new DrumInputEvent(pad, KeyboardVelocity, songTimeSeconds));
        }
    }
}
