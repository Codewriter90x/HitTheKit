using System;
using UnityEngine;

namespace HitTheKit.Unity.Visuals
{
    public sealed class PadHitFeedbackState
    {
        public bool IsActive => RemainingSeconds > 0;
        public Color Color { get; private set; }
        public double RemainingSeconds { get; private set; }

        public void Begin(Color color, double durationSeconds)
        {
            EnsureFinite(color.r, nameof(color));
            EnsureFinite(color.g, nameof(color));
            EnsureFinite(color.b, nameof(color));
            EnsureFinite(color.a, nameof(color));
            EnsureFinite(durationSeconds, nameof(durationSeconds));
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Feedback duration must be positive.");
            }

            Color = color;
            RemainingSeconds = durationSeconds;
        }

        public void Advance(double elapsedSeconds)
        {
            EnsureFinite(elapsedSeconds, nameof(elapsedSeconds));
            if (elapsedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be non-negative.");
            }

            if (!IsActive) return;
            RemainingSeconds = Math.Max(0, RemainingSeconds - elapsedSeconds);
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }
    }
}
