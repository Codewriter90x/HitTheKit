using System;
using HitTheKit.Core;

namespace HitTheKit.Unity.MainMenu
{
    /// <summary>
    /// Recognizes two intentional kick hits as a menu confirmation gesture.
    /// The lower bound rejects electrical/buffer duplicates while the upper
    /// bound keeps two unrelated pedal hits from becoming an activation.
    /// </summary>
    public sealed class DoubleKickConfirmGesture
    {
        public const double MinimumIntervalSeconds = 0.06;
        public const double MaximumIntervalSeconds = 0.55;

        private double? firstKickSeconds;

        public bool Register(DrumPad pad, int velocity, double monotonicSeconds)
        {
            if (velocity < 0 || velocity > 127) throw new ArgumentOutOfRangeException(nameof(velocity));
            if (double.IsNaN(monotonicSeconds) || double.IsInfinity(monotonicSeconds))
                throw new ArgumentOutOfRangeException(nameof(monotonicSeconds));

            if (pad != DrumPad.Kick || velocity == 0)
            {
                Reset();
                return false;
            }

            if (!firstKickSeconds.HasValue)
            {
                firstKickSeconds = monotonicSeconds;
                return false;
            }

            double interval = monotonicSeconds - firstKickSeconds.Value;
            if (interval >= MinimumIntervalSeconds && interval <= MaximumIntervalSeconds)
            {
                Reset();
                return true;
            }

            firstKickSeconds = monotonicSeconds;
            return false;
        }

        public void Reset() => firstKickSeconds = null;
    }
}
