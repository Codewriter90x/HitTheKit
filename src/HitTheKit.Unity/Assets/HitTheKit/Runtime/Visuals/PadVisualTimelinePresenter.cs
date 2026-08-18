using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using UnityEngine;

namespace HitTheKit.Unity.Visuals
{
    public sealed class PadVisualTimelinePresenter : MonoBehaviour
    {
        [SerializeField] private DspSongClockPrototype songClock;
        [SerializeField] private ChartTimelinePrototype chartTimeline;
        [SerializeField] private DrumPadVisual kickVisual;
        [SerializeField] private DrumPadVisual snareVisual;
        [SerializeField] private DrumPadVisual hiHatVisual;

        private readonly PadVisualStateCalculator calculator = new PadVisualStateCalculator();

        public DspSongClockPrototype SongClock => songClock;
        public ChartTimelinePrototype ChartTimeline => chartTimeline;
        public DrumPadVisual KickVisual => kickVisual;
        public DrumPadVisual SnareVisual => snareVisual;
        public DrumPadVisual HiHatVisual => hiHatVisual;

        private void Start()
        {
            if (!HasValidReferences())
            {
                Debug.LogError("The pad visual presenter requires connected clock, timeline, and pad visuals.", this);
                ApplyInactive();
                enabled = false;
                return;
            }

            ApplyInactive();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!HasValidReferences() ||
                songClock.Clock == null ||
                !songClock.Clock.IsScheduled ||
                chartTimeline.Timeline == null)
            {
                ApplyInactive();
                return;
            }

            double position = songClock.PositionSeconds;
            double lookAhead = chartTimeline.LookAheadSeconds;
            kickVisual.ApplyState(calculator.Calculate(
                DrumPad.Kick,
                position,
                lookAhead,
                chartTimeline.UpcomingNotes));
            snareVisual.ApplyState(calculator.Calculate(
                DrumPad.Snare,
                position,
                lookAhead,
                chartTimeline.UpcomingNotes));
            hiHatVisual.ApplyState(calculator.Calculate(
                DrumPad.HiHat,
                position,
                lookAhead,
                chartTimeline.UpcomingNotes));
        }

        private bool HasValidReferences()
        {
            return songClock != null &&
                   chartTimeline != null &&
                   kickVisual != null && kickVisual.Pad == DrumPad.Kick &&
                   snareVisual != null && snareVisual.Pad == DrumPad.Snare &&
                   hiHatVisual != null && hiHatVisual.Pad == DrumPad.HiHat;
        }

        private void ApplyInactive()
        {
            if (kickVisual != null) kickVisual.ApplyState(PadVisualState.Inactive);
            if (snareVisual != null) snareVisual.ApplyState(PadVisualState.Inactive);
            if (hiHatVisual != null) hiHatVisual.ApplyState(PadVisualState.Inactive);
        }
    }
}
