using System;
using System.Collections.Generic;

namespace HitTheKit.Core
{
    public sealed class TimingCalibrationSnapshot
    {
        internal TimingCalibrationSnapshot(int sampleCount, double medianDeltaSeconds, double medianAbsoluteDeviationSeconds)
        {
            SampleCount = sampleCount;
            MedianDeltaSeconds = medianDeltaSeconds;
            MedianAbsoluteDeviationSeconds = medianAbsoluteDeviationSeconds;
        }

        public int SampleCount { get; }
        public double MedianDeltaSeconds { get; }
        public double MedianAbsoluteDeviationSeconds { get; }
        public bool HasRecommendation => SampleCount >= TimingCalibrationAdvisor.MinimumSamples;
    }

    public sealed class TimingCalibrationAdvisor
    {
        public const int MinimumSamples = 8;
        public const int MaximumSamples = 64;
        public const double MaximumOffsetSeconds = 0.250;

        private readonly Queue<double> samples = new Queue<double>(MaximumSamples);

        public TimingCalibrationSnapshot Snapshot => Calculate();

        public void Add(double deltaSeconds)
        {
            if (!IsFinite(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Timing delta must be finite.");
            if (Math.Abs(deltaSeconds) > TimingWindows.Default.HitSeconds)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Only matched-hit deltas can be calibrated.");

            if (samples.Count == MaximumSamples) samples.Dequeue();
            samples.Enqueue(deltaSeconds);
        }

        public double RecommendOffsetSeconds(double currentOffsetSeconds)
        {
            if (!IsFinite(currentOffsetSeconds) || Math.Abs(currentOffsetSeconds) > MaximumOffsetSeconds)
                throw new ArgumentOutOfRangeException(nameof(currentOffsetSeconds));

            TimingCalibrationSnapshot snapshot = Calculate();
            if (!snapshot.HasRecommendation) return currentOffsetSeconds;
            return Clamp(currentOffsetSeconds + snapshot.MedianDeltaSeconds, -MaximumOffsetSeconds, MaximumOffsetSeconds);
        }

        public void Reset() => samples.Clear();

        private TimingCalibrationSnapshot Calculate()
        {
            if (samples.Count == 0) return new TimingCalibrationSnapshot(0, 0, 0);
            double[] ordered = samples.ToArray();
            Array.Sort(ordered);
            double median = Median(ordered);
            var deviations = new double[ordered.Length];
            for (int index = 0; index < ordered.Length; index++)
                deviations[index] = Math.Abs(ordered[index] - median);
            Array.Sort(deviations);
            return new TimingCalibrationSnapshot(ordered.Length, median, Median(deviations));
        }

        private static double Median(IReadOnlyList<double> ordered)
        {
            int middle = ordered.Count / 2;
            return ordered.Count % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2d
                : ordered[middle];
        }

        private static double Clamp(double value, double minimum, double maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
