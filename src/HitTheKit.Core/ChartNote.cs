using System;

namespace HitTheKit.Core
{
    public sealed class ChartNote
    {
        public ChartNote(
            double timeSeconds,
            DrumPad pad,
            int? velocity = null,
            DrumArticulation articulation = DrumArticulation.Default)
        {
            if (!IsFinite(timeSeconds) || timeSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeSeconds),
                    "A chart note time must be finite and non-negative.");
            }

            if (velocity.HasValue && (velocity.Value < 1 || velocity.Value > 127))
                throw new ArgumentOutOfRangeException(nameof(velocity), "Target velocity must be between 1 and 127.");
            DrumArticulationValidator.EnsureValid(pad, articulation);

            TimeSeconds = timeSeconds;
            Pad = pad;
            Velocity = velocity;
            Articulation = articulation;
        }

        public double TimeSeconds { get; }

        public DrumPad Pad { get; }

        public int? Velocity { get; }

        public DrumArticulation Articulation { get; }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
