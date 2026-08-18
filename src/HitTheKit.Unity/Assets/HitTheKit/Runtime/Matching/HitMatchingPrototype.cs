using System;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using UnityEngine;

namespace HitTheKit.Unity.Matching
{
    public sealed class HitMatchingPrototype : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour drumInputSource;
        [SerializeField] private ChartTimelinePrototype chartTimeline;
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private TimingWindowSettings timingWindows = new TimingWindowSettings();

        private IDrumInput subscribedInput;
        private bool invalidConfigurationLogged;

        public event Action<HitResult> HitResolved;
        public event Action<DrumInputEvent, HitResult> InputProcessed;

        public IDrumInput DrumInput => drumInputSource as IDrumInput;
        public ChartTimelinePrototype ChartTimeline => chartTimeline;
        public DspSongClockPrototype SongClock => songClock;
        public HitMatchingSession Session { get; private set; }
        public HitMatchingSnapshot Snapshot => Session?.Snapshot;

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                LogInvalidConfigurationOnce();
                enabled = false;
                return;
            }

            SubscribeInput();
            TryInitializeSession();
        }

        private void Update()
        {
            if (!TryInitializeSession() || songClock.Clock == null || !songClock.Clock.IsScheduled)
            {
                return;
            }

            double songPositionSeconds = songClock.PositionSeconds;
            Session.ProcessMisses(songPositionSeconds);
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void HandleInput(DrumInputEvent input)
        {
            if (!TryInitializeSession())
            {
                return;
            }

            double offset = PlayerPreferencesRuntime.Current.Snapshot.OffsetFor(input.Source);
            Session.ProcessInput(input.WithSongTime(input.SongTimeSeconds - offset), out _);
        }

        public void RestartSession()
        {
            Session = null;
            TryInitializeSession();
        }

        private bool TryInitializeSession()
        {
            if (Session != null) return true;
            if (!HasRequiredReferences())
            {
                LogInvalidConfigurationOnce();
                return false;
            }

            if (chartTimeline.Chart == null)
            {
                return false;
            }

            Session = new HitMatchingSession(
                chartTimeline.CreateMatchingNotes(),
                timingWindows.ToCore(),
                0);
            Session.HitResolved += result => HitResolved?.Invoke(result);
            Session.InputProcessed += (input, result) => InputProcessed?.Invoke(input, result);
            return true;
        }

        private bool HasRequiredReferences()
        {
            return drumInputSource != null && DrumInput != null &&
                   chartTimeline != null && songClock != null && timingWindows != null;
        }

        private void SubscribeInput()
        {
            IDrumInput input = DrumInput;
            if (input == null || ReferenceEquals(input, subscribedInput)) return;
            UnsubscribeInput();
            input.HitReceived += HandleInput;
            subscribedInput = input;
        }

        private void UnsubscribeInput()
        {
            if (subscribedInput == null) return;
            subscribedInput.HitReceived -= HandleInput;
            subscribedInput = null;
        }

        private void LogInvalidConfigurationOnce()
        {
            if (invalidConfigurationLogged) return;
            invalidConfigurationLogged = true;
            Debug.LogError("Hit matching requires an IDrumInput, chart timeline, DSP clock, and timing settings.", this);
        }
    }
}
