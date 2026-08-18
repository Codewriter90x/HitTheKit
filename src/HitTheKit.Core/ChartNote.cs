using System;

namespace HitTheKit.Core
{
    public sealed class ChartNote
    {
        public ChartNote(double timeSeconds, DrumPad pad)
        {
            if (!IsFinite(timeSeconds) || timeSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeSeconds),
                    "A chart note time must be finite and non-negative.");
            }

            TimeSeconds = timeSeconds;
            Pad = pad;
        }

        public double TimeSeconds { get; }

        public DrumPad Pad { get; }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
