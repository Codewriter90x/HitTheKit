using System;

namespace HitTheKit.Core
{
    public enum GuidedSoundCheckState
    {
        Idle,
        Running,
        Complete
    }

    public enum GuidedSoundCheckInput
    {
        Keyboard,
        Midi
    }

    public sealed class GuidedSoundCheckSnapshot
    {
        internal GuidedSoundCheckSnapshot(
            GuidedSoundCheckState state,
            GuidedSoundCheckInput source,
            int targetCount,
            int acceptedCount,
            int missedCount,
            double? nextTargetTimeSeconds,
            TimingCalibrationSnapshot calibration)
        {
            State = state;
            Source = source;
            TargetCount = targetCount;
            AcceptedCount = acceptedCount;
            MissedCount = missedCount;
            NextTargetTimeSeconds = nextTargetTimeSeconds;
            Calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        }

        public GuidedSoundCheckState State { get; }
        public GuidedSoundCheckInput Source { get; }
        public int TargetCount { get; }
        public int AcceptedCount { get; }
        public int MissedCount { get; }
        public int ResolvedCount => AcceptedCount + MissedCount;
        public double? NextTargetTimeSeconds { get; }
        public TimingCalibrationSnapshot Calibration { get; }
        public bool CanApplyRecommendation => State == GuidedSoundCheckState.Complete && Calibration.HasRecommendation;
    }

    public sealed class GuidedLatencySoundCheck
    {
        public const int DefaultTargetCount = 12;
        public const double DefaultIntervalSeconds = 0.5;

        private readonly TimingCalibrationAdvisor advisor = new TimingCalibrationAdvisor();
        private readonly int targetCount;
        private GuidedSoundCheckState state;
        private GuidedSoundCheckInput source;
        private double firstTargetTimeSeconds;
        private double intervalSeconds;
        private int targetIndex;
        private int acceptedCount;
        private int missedCount;

        public GuidedLatencySoundCheck(int targetCount = DefaultTargetCount)
        {
            if (targetCount < TimingCalibrationAdvisor.MinimumSamples)
                throw new ArgumentOutOfRangeException(nameof(targetCount));
            this.targetCount = targetCount;
        }

        public GuidedSoundCheckSnapshot Snapshot => new GuidedSoundCheckSnapshot(
            state,
            source,
            targetCount,
            acceptedCount,
            missedCount,
            state == GuidedSoundCheckState.Running ? TargetTime(targetIndex) : (double?)null,
            advisor.Snapshot);

        public void Begin(GuidedSoundCheckInput source, double firstTargetTimeSeconds, double intervalSeconds = DefaultIntervalSeconds)
        {
            if (source != GuidedSoundCheckInput.Keyboard && source != GuidedSoundCheckInput.Midi)
                throw new ArgumentOutOfRangeException(nameof(source));
            if (!IsFinite(firstTargetTimeSeconds) || firstTargetTimeSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(firstTargetTimeSeconds));
            if (!IsFinite(intervalSeconds) || intervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));

            this.source = source;
            this.firstTargetTimeSeconds = firstTargetTimeSeconds;
            this.intervalSeconds = intervalSeconds;
            targetIndex = 0;
            acceptedCount = 0;
            missedCount = 0;
            advisor.Reset();
            state = GuidedSoundCheckState.Running;
        }

        public bool TryRecord(GuidedSoundCheckInput inputSource, double hitTimeSeconds)
        {
            if (!IsFinite(hitTimeSeconds)) throw new ArgumentOutOfRangeException(nameof(hitTimeSeconds));
            if (state != GuidedSoundCheckState.Running || inputSource != source) return false;
            Advance(hitTimeSeconds);
            if (state != GuidedSoundCheckState.Running) return false;

            double delta = hitTimeSeconds - TargetTime(targetIndex);
            if (Math.Abs(delta) > TimingWindows.Default.HitSeconds) return false;
            advisor.Add(delta);
            acceptedCount++;
            targetIndex++;
            CompleteIfResolved();
            return true;
        }

        public void Advance(double currentTimeSeconds)
        {
            if (!IsFinite(currentTimeSeconds)) throw new ArgumentOutOfRangeException(nameof(currentTimeSeconds));
            if (state != GuidedSoundCheckState.Running) return;
            while (targetIndex < targetCount &&
                   currentTimeSeconds > TargetTime(targetIndex) + TimingWindows.Default.HitSeconds)
            {
                missedCount++;
                targetIndex++;
            }
            CompleteIfResolved();
        }

        public double RecommendOffsetSeconds(double currentOffsetSeconds)
        {
            if (!Snapshot.CanApplyRecommendation)
                throw new InvalidOperationException("The sound check does not have enough accepted hits.");
            return advisor.RecommendOffsetSeconds(currentOffsetSeconds);
        }

        public void Reset()
        {
            state = GuidedSoundCheckState.Idle;
            targetIndex = 0;
            acceptedCount = 0;
            missedCount = 0;
            advisor.Reset();
        }

        private double TargetTime(int index) => firstTargetTimeSeconds + index * intervalSeconds;

        private void CompleteIfResolved()
        {
            if (targetIndex >= targetCount) state = GuidedSoundCheckState.Complete;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
