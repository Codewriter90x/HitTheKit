using System;

namespace HitTheKit.Core
{
    public sealed class TimingWindows
    {
        public TimingWindows(
            double perfectSeconds,
            double goodSeconds,
            double hitSeconds)
        {
            ValidateWindow(perfectSeconds, nameof(perfectSeconds));
            ValidateWindow(goodSeconds, nameof(goodSeconds));
            ValidateWindow(hitSeconds, nameof(hitSeconds));

            if (perfectSeconds > goodSeconds)
            {
                throw new ArgumentException(
                    "The perfect window cannot exceed the good window.",
                    nameof(perfectSeconds));
            }

            if (goodSeconds > hitSeconds)
            {
                throw new ArgumentException(
                    "The good window cannot exceed the hit window.",
                    nameof(goodSeconds));
            }

            PerfectSeconds = perfectSeconds;
            GoodSeconds = goodSeconds;
            HitSeconds = hitSeconds;
        }

        public static TimingWindows Default { get; } = new TimingWindows(
            perfectSeconds: 0.040,
            goodSeconds: 0.090,
            hitSeconds: 0.150);

        public double PerfectSeconds { get; }

        public double GoodSeconds { get; }

        public double HitSeconds { get; }

        private static void ValidateWindow(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A timing window must be finite and non-negative.");
            }
        }
    }
}
