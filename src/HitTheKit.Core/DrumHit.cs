using System;

namespace HitTheKit.Core
{
    public readonly struct DrumHit
    {
        public DrumHit(
            DrumPad pad,
            double timeSeconds,
            int velocity = 127,
            DrumArticulation articulation = DrumArticulation.Default)
        {
            if (double.IsNaN(timeSeconds) || double.IsInfinity(timeSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeSeconds),
                    "A drum hit time must be finite.");
            }

            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(velocity),
                    "Velocity must be between 0 and 127.");
            }

            DrumArticulationValidator.EnsureValid(pad, articulation);
            Pad = pad;
            TimeSeconds = timeSeconds;
            Velocity = velocity;
            Articulation = articulation;
        }

        public DrumPad Pad { get; }

        public double TimeSeconds { get; }

        public int Velocity { get; }

        public DrumArticulation Articulation { get; }
    }
}
