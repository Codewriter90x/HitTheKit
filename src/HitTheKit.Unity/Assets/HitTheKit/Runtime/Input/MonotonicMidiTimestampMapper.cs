using System;

namespace HitTheKit.Unity.Input
{
    public sealed class MonotonicMidiTimestampMapper
    {
        private const double MaximumRepresentedAgeSeconds = 60.0;

        public double Map(double eventMonotonicSeconds, double currentMonotonicSeconds, double songPositionSeconds)
        {
            Validate(eventMonotonicSeconds, nameof(eventMonotonicSeconds));
            Validate(currentMonotonicSeconds, nameof(currentMonotonicSeconds));
            Validate(songPositionSeconds, nameof(songPositionSeconds));

            double ageSeconds = currentMonotonicSeconds - eventMonotonicSeconds;
            if (ageSeconds < 0) ageSeconds = 0;
            if (ageSeconds > MaximumRepresentedAgeSeconds) ageSeconds = MaximumRepresentedAgeSeconds;
            return songPositionSeconds - ageSeconds;
        }

        public void Reset() { }

        private static void Validate(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Clock values must be finite.");
        }
    }
}
