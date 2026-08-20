using System;
using System.Collections.Generic;
using HitTheKit.Unity.Matching;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayAutoTempoStatus
    {
        Unavailable,
        Repeat,
        Advance,
        Mastered
    }

    public sealed class GameplayAutoTempoRecommendation
    {
        internal GameplayAutoTempoRecommendation(
            GameplayAutoTempoStatus status,
            double currentSpeed,
            double nextSpeed,
            string message)
        {
            Status = status;
            CurrentSpeed = currentSpeed;
            NextSpeed = nextSpeed;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public GameplayAutoTempoStatus Status { get; }
        public double CurrentSpeed { get; }
        public double NextSpeed { get; }
        public string Message { get; }
        public bool CanAdvance => Status == GameplayAutoTempoStatus.Advance;
    }

    public static class GameplayAutoTempoCoach
    {
        public const double MinimumAccuracy = 85.0;
        public const double MaximumMissRatio = 0.10;
        public const double MaximumNoMatchRatio = 0.10;

        public static GameplayAutoTempoRecommendation Evaluate(
            GameplaySessionDefinition session,
            GameplayScoreSnapshot score,
            HitMatchingSnapshot matching)
        {
            if (score == null) throw new ArgumentNullException(nameof(score));
            if (matching == null) throw new ArgumentNullException(nameof(matching));
            return Evaluate(
                session,
                score.Accuracy,
                matching.MissCount,
                matching.NoMatchCount,
                matching.TotalNoteCount);
        }

        public static GameplayAutoTempoRecommendation Evaluate(
            GameplaySessionDefinition session,
            double accuracy,
            int misses,
            int noMatches,
            int totalNotes)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (double.IsNaN(accuracy) || double.IsInfinity(accuracy) || accuracy < 0 || accuracy > 100)
                throw new ArgumentOutOfRangeException(nameof(accuracy));
            if (misses < 0) throw new ArgumentOutOfRangeException(nameof(misses));
            if (noMatches < 0) throw new ArgumentOutOfRangeException(nameof(noMatches));
            if (totalNotes <= 0) throw new ArgumentOutOfRangeException(nameof(totalNotes));

            IReadOnlyList<double> speeds = SpeedsFor(session);
            if (speeds == null)
            {
                return new GameplayAutoTempoRecommendation(
                    GameplayAutoTempoStatus.Unavailable,
                    session.SpeedMultiplier,
                    session.SpeedMultiplier,
                    "AUTO TEMPO DISPONIBILE PER LEZIONI E BRANI DELLA LIBRERIA");
            }

            int currentIndex = FindSpeed(speeds, session.SpeedMultiplier);
            if (currentIndex < 0)
                throw new InvalidOperationException("The current session speed is outside the Auto Tempo progression.");

            double missRatio = misses / (double)totalNotes;
            double noMatchRatio = noMatches / (double)totalNotes;
            bool passed = accuracy >= MinimumAccuracy &&
                          missRatio <= MaximumMissRatio &&
                          noMatchRatio <= MaximumNoMatchRatio;
            if (!passed)
            {
                return new GameplayAutoTempoRecommendation(
                    GameplayAutoTempoStatus.Repeat,
                    session.SpeedMultiplier,
                    session.SpeedMultiplier,
                    $"RIPETI {session.SpeedMultiplier:0.##}× · SERVONO {MinimumAccuracy:0}% E POCHI ERRORI");
            }

            if (currentIndex == speeds.Count - 1)
            {
                return new GameplayAutoTempoRecommendation(
                    GameplayAutoTempoStatus.Mastered,
                    session.SpeedMultiplier,
                    session.SpeedMultiplier,
                    "TEMPO OBIETTIVO RAGGIUNTO · 100%");
            }

            double next = speeds[currentIndex + 1];
            return new GameplayAutoTempoRecommendation(
                GameplayAutoTempoStatus.Advance,
                session.SpeedMultiplier,
                next,
                $"PRONTO PER {next:0.##}× · PRECISIONE {accuracy:0.0}%");
        }

        private static IReadOnlyList<double> SpeedsFor(GameplaySessionDefinition session)
        {
            if (session.IsChartCreator) return null;
            if (session.Kind == GameplaySessionKind.Lesson) return GameplayStudySpeeds.All;
            if (!string.IsNullOrWhiteSpace(session.SongId)) return GameplaySongSpeeds.All;
            return null;
        }

        private static int FindSpeed(IReadOnlyList<double> speeds, double value)
        {
            for (int index = 0; index < speeds.Count; index++)
                if (Math.Abs(speeds[index] - value) < 0.0001) return index;
            return -1;
        }
    }
}
