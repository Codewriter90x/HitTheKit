using System;

namespace HitTheKit.Unity.Audio
{
    public sealed class DspSongClock
    {
        private readonly IDspTimeSource timeSource;

        public DspSongClock(IDspTimeSource timeSource)
        {
            this.timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public bool IsScheduled { get; private set; }
        public bool IsPaused { get; private set; }
        public double StartDspTime { get; private set; }
        public double DurationSeconds { get; private set; }
        private double pausedAtDspTime;

        public double PositionSeconds
        {
            get
            {
                EnsureScheduled();
                double now = IsPaused ? pausedAtDspTime : timeSource.Now;
                return now - StartDspTime;
            }
        }

        public bool HasStarted => IsScheduled && (IsPaused ? pausedAtDspTime : timeSource.Now) >= StartDspTime;
        public bool HasCompleted => IsScheduled && PositionSeconds >= DurationSeconds;

        public void Schedule(double startDspTime, double durationSeconds)
        {
            if (IsScheduled)
            {
                throw new InvalidOperationException("Reset the clock before scheduling it again.");
            }

            EnsureFinite(startDspTime, nameof(startDspTime));
            EnsureFinite(durationSeconds, nameof(durationSeconds));
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive.");
            }

            StartDspTime = startDspTime;
            DurationSeconds = durationSeconds;
            IsScheduled = true;
        }

        public void Reset()
        {
            IsScheduled = false;
            IsPaused = false;
            StartDspTime = 0;
            DurationSeconds = 0;
            pausedAtDspTime = 0;
        }

        public void Pause()
        {
            EnsureScheduled();
            if (IsPaused) return;
            pausedAtDspTime = timeSource.Now;
            IsPaused = true;
        }

        public void Resume()
        {
            EnsureScheduled();
            if (!IsPaused) return;
            StartDspTime += timeSource.Now - pausedAtDspTime;
            pausedAtDspTime = 0;
            IsPaused = false;
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }

        private void EnsureScheduled()
        {
            if (!IsScheduled)
            {
                throw new InvalidOperationException("The song clock has not been scheduled.");
            }
        }
    }
}
