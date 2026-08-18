using System;
using HitTheKit.Core;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayRunState
    {
        Countdown,
        Playing,
        Paused,
        Results
    }

    public sealed class GameplayScoreSnapshot
    {
        internal GameplayScoreSnapshot(int score, int combo, int maxCombo, int resolved, double accuracy)
        {
            Score = score;
            Combo = combo;
            MaxCombo = maxCombo;
            Resolved = resolved;
            Accuracy = accuracy;
        }

        public int Score { get; }
        public int Combo { get; }
        public int MaxCombo { get; }
        public int Resolved { get; }
        public double Accuracy { get; }
        public string Rank => Accuracy >= 97 ? "S" : Accuracy >= 90 ? "A" : Accuracy >= 80 ? "B" : Accuracy >= 70 ? "C" : "D";
    }

    public sealed class GameplayScoreTracker
    {
        private int score;
        private int combo;
        private int maxCombo;
        private int resolved;
        private double earnedAccuracy;

        public GameplayScoreSnapshot Snapshot =>
            new GameplayScoreSnapshot(score, combo, maxCombo, resolved, resolved == 0 ? 100 : earnedAccuracy / resolved * 100);

        public void Apply(HitResult result)
        {
            if (result == null)
            {
                combo = 0;
                return;
            }

            resolved++;
            int baseScore;
            double accuracy;
            switch (result.Grade)
            {
                case HitGrade.Perfect: baseScore = 1000; accuracy = 1; break;
                case HitGrade.Good: baseScore = 750; accuracy = 0.75; break;
                case HitGrade.Early:
                case HitGrade.Late: baseScore = 450; accuracy = 0.5; break;
                case HitGrade.Miss: baseScore = 0; accuracy = 0; break;
                default: throw new ArgumentOutOfRangeException();
            }

            earnedAccuracy += accuracy;
            if (result.Grade == HitGrade.Miss)
            {
                combo = 0;
                return;
            }

            combo++;
            maxCombo = Math.Max(maxCombo, combo);
            score += (int)Math.Round(baseScore * (1 + Math.Min(combo, 50) * 0.01), MidpointRounding.AwayFromZero);
        }

        public void Reset()
        {
            score = combo = maxCombo = resolved = 0;
            earnedAccuracy = 0;
        }
    }
}
