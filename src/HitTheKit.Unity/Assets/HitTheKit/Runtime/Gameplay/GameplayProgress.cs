using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using HitTheKit.Core;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplaySessionKind
    {
        FreePlay,
        Lesson
    }

    public sealed class GameplaySessionResult
    {
        public GameplaySessionResult(
            string sessionId,
            string completedAtUtc,
            GameplaySessionKind kind,
            GameplayLessonId? lessonId,
            double speedMultiplier,
            double activeSeconds,
            int score,
            double accuracy,
            int maxCombo,
            int perfectCount,
            int goodCount,
            int earlyCount,
            int lateCount,
            int missCount)
        {
            if (!Guid.TryParseExact(sessionId, "N", out _))
                throw new ArgumentException("Session ID must be a canonical GUID without separators.", nameof(sessionId));
            if (!DateTimeOffset.TryParseExact(completedAtUtc, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTimeOffset completed) || completed.Offset != TimeSpan.Zero)
                throw new ArgumentException("Completion time must be a canonical UTC timestamp.", nameof(completedAtUtc));
            if (!Enum.IsDefined(typeof(GameplaySessionKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == GameplaySessionKind.Lesson)
            {
                if (!lessonId.HasValue || !GameplayLearningPath.Find(lessonId.Value).IsPlayable)
                    throw new ArgumentException("A lesson result requires a playable lesson.", nameof(lessonId));
                if (!GameplayStudySpeeds.IsSupported(speedMultiplier))
                    throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            }
            else if (lessonId.HasValue)
            {
                throw new ArgumentException("A free-play result cannot reference a lesson.", nameof(lessonId));
            }

            if (!IsFinite(activeSeconds) || activeSeconds < 0 || activeSeconds > GameplayProgressService.MaximumSessionSeconds)
                throw new ArgumentOutOfRangeException(nameof(activeSeconds));
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            if (!IsFinite(accuracy) || accuracy < 0 || accuracy > 100)
                throw new ArgumentOutOfRangeException(nameof(accuracy));
            if (maxCombo < 0) throw new ArgumentOutOfRangeException(nameof(maxCombo));
            ValidateCount(perfectCount, nameof(perfectCount));
            ValidateCount(goodCount, nameof(goodCount));
            ValidateCount(earlyCount, nameof(earlyCount));
            ValidateCount(lateCount, nameof(lateCount));
            ValidateCount(missCount, nameof(missCount));

            int resolved = checked(perfectCount + goodCount + earlyCount + lateCount + missCount);
            if (maxCombo > resolved) throw new ArgumentException("Maximum combo cannot exceed resolved notes.", nameof(maxCombo));
            double expectedAccuracy = resolved == 0
                ? 100
                : (perfectCount + goodCount * 0.75 + (earlyCount + lateCount) * 0.5) / resolved * 100;
            if (Math.Abs(expectedAccuracy - accuracy) > 0.051)
                throw new ArgumentException("Accuracy does not match the result breakdown.", nameof(accuracy));

            SessionId = sessionId;
            CompletedAtUtc = completedAtUtc;
            Kind = kind;
            LessonId = lessonId;
            SpeedMultiplier = speedMultiplier;
            ActiveSeconds = activeSeconds;
            Score = score;
            Accuracy = accuracy;
            MaxCombo = maxCombo;
            PerfectCount = perfectCount;
            GoodCount = goodCount;
            EarlyCount = earlyCount;
            LateCount = lateCount;
            MissCount = missCount;
        }

        public string SessionId { get; }
        public string CompletedAtUtc { get; }
        public GameplaySessionKind Kind { get; }
        public GameplayLessonId? LessonId { get; }
        public double SpeedMultiplier { get; }
        public double ActiveSeconds { get; }
        public int Score { get; }
        public double Accuracy { get; }
        public int MaxCombo { get; }
        public int PerfectCount { get; }
        public int GoodCount { get; }
        public int EarlyCount { get; }
        public int LateCount { get; }
        public int MissCount { get; }
        public int ResolvedCount => PerfectCount + GoodCount + EarlyCount + LateCount + MissCount;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void ValidateCount(int value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class GameplayProgressSnapshot
    {
        internal GameplayProgressSnapshot(
            string profileId,
            double totalPracticeSeconds,
            long completedSessionCount,
            IDictionary<string, double> bestAccuracies,
            IList<GameplaySessionResult> recentSessions)
        {
            ProfileId = profileId;
            TotalPracticeSeconds = totalPracticeSeconds;
            CompletedSessionCount = completedSessionCount;
            BestAccuracies = new ReadOnlyDictionary<string, double>(
                new Dictionary<string, double>(bestAccuracies, StringComparer.Ordinal));
            RecentSessions = Array.AsReadOnly(recentSessions.ToArray());
        }

        public string ProfileId { get; }
        public double TotalPracticeSeconds { get; }
        public double TotalPracticeHours => TotalPracticeSeconds / 3600.0;
        public long CompletedSessionCount { get; }
        public IReadOnlyDictionary<string, double> BestAccuracies { get; }
        public IReadOnlyList<GameplaySessionResult> RecentSessions { get; }
    }

    public interface IGameplayProgressPersistence
    {
        bool TryLoad(out string json);
        void Save(string json);
    }

    public sealed class InMemoryGameplayProgressPersistence : IGameplayProgressPersistence
    {
        private string json;

        public bool TryLoad(out string value)
        {
            value = json;
            return json != null;
        }

        public void Save(string value)
        {
            json = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Content => json;
    }

    public sealed class GameplayProgressOperationResult
    {
        internal GameplayProgressOperationResult(bool succeeded, string path, string message)
        {
            Succeeded = succeeded;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string Path { get; }
        public string Message { get; }
    }

    public sealed class GameplayProgressService
    {
        public const int SupportedSchemaVersion = 1;
        public const int MaximumRecentSessions = 1000;
        public const double MaximumSessionSeconds = 7 * 24 * 60 * 60;
        public const double MaximumLifetimeSeconds = 100 * 365.25 * 24 * 60 * 60;

        private readonly IGameplayProgressPersistence persistence;
        private PlayerProgressState state;
        private bool persistenceBlocked;

        public GameplayProgressService(IGameplayProgressPersistence persistence)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            state = PlayerProgressState.Empty();
            try
            {
                if (persistence.TryLoad(out string json)) state = GameplayProgressJson.Deserialize(json);
            }
            catch (Exception exception)
            {
                persistenceBlocked = true;
                LastError = $"Saved progress could not be loaded: {exception.Message}";
            }
        }

        public GameplayProgressSnapshot Snapshot => state.Snapshot();
        public string LastError { get; private set; } = string.Empty;
        public bool HasLoadError => persistenceBlocked;

        public double? BestAccuracy(GameplayLessonId lessonId, double speedMultiplier)
        {
            GameplayLearningPath.Find(lessonId);
            ValidateSpeed(speedMultiplier);
            return state.BestAccuracies.TryGetValue(Key(lessonId, speedMultiplier), out double value)
                ? value
                : (double?)null;
        }

        public void RecordLessonResult(GameplayLessonId lessonId, double speedMultiplier, double accuracy)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(lessonId);
            if (!lesson.IsPlayable) throw new InvalidOperationException("Results can only be recorded for playable lessons.");
            ValidateSpeed(speedMultiplier);
            ValidateAccuracy(accuracy);
            state.RecordBest(Key(lessonId, speedMultiplier), accuracy);
            Persist();
        }

        public void AddPracticeTime(double activeSeconds)
        {
            if (!IsFinite(activeSeconds) || activeSeconds <= 0 || activeSeconds > MaximumSessionSeconds)
                throw new ArgumentOutOfRangeException(nameof(activeSeconds));
            state.AddPracticeTime(activeSeconds);
            Persist();
        }

        public void RecordCompletedSession(GameplaySessionResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            state.AddCompletedSession(result);
            if (result.Kind == GameplaySessionKind.Lesson)
                state.RecordBest(Key(result.LessonId.Value, result.SpeedMultiplier), result.Accuracy);
            Persist();
        }

        public GameplayProgressOperationResult ExportBackup(string path)
        {
            try
            {
                AtomicJsonFileGameplayProgressPersistence.WriteExternal(path, GameplayProgressJson.Serialize(state));
                return new GameplayProgressOperationResult(true, path, "Progress backup exported.");
            }
            catch (Exception exception)
            {
                return new GameplayProgressOperationResult(false, path, $"Backup export failed: {exception.Message}");
            }
        }

        public GameplayProgressOperationResult ImportBackup(string path, string recoveryPath)
        {
            PlayerProgressState imported;
            try
            {
                imported = GameplayProgressJson.Deserialize(
                    AtomicJsonFileGameplayProgressPersistence.ReadExternal(path));
            }
            catch (Exception exception)
            {
                return new GameplayProgressOperationResult(false, path, $"Backup import failed: {exception.Message}");
            }

            PlayerProgressState previous = state;
            bool previousPersistenceBlocked = persistenceBlocked;
            string previousLastError = LastError;
            try
            {
                AtomicJsonFileGameplayProgressPersistence.WriteExternal(
                    recoveryPath, GameplayProgressJson.Serialize(previous));
                state = imported;
                persistenceBlocked = false;
                LastError = string.Empty;
                persistence.Save(GameplayProgressJson.Serialize(state));
                return new GameplayProgressOperationResult(true, path, "Progress backup imported.");
            }
            catch (Exception exception)
            {
                state = previous;
                persistenceBlocked = previousPersistenceBlocked;
                LastError = previousLastError;
                return new GameplayProgressOperationResult(false, path, $"Backup import could not be committed: {exception.Message}");
            }
        }

        public string ExportJson() => GameplayProgressJson.Serialize(state);

        public void Reset()
        {
            state = PlayerProgressState.Empty();
            persistenceBlocked = false;
            LastError = string.Empty;
            Persist();
        }

        private void Persist()
        {
            if (persistenceBlocked) return;
            try
            {
                persistence.Save(GameplayProgressJson.Serialize(state));
                LastError = string.Empty;
            }
            catch (Exception exception)
            {
                LastError = $"Progress could not be saved: {exception.Message}";
            }
        }

        internal static string Key(GameplayLessonId lessonId, double speedMultiplier) =>
            $"{lessonId}:{speedMultiplier:0.00}";

        internal static void ValidateSpeed(double speedMultiplier)
        {
            if (!GameplayStudySpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
        }

        internal static void ValidateAccuracy(double accuracy)
        {
            if (!IsFinite(accuracy) || accuracy < 0 || accuracy > 100)
                throw new ArgumentOutOfRangeException(nameof(accuracy));
        }

        internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal sealed class PlayerProgressState
    {
        internal PlayerProgressState(
            string profileId,
            double totalPracticeSeconds,
            long completedSessionCount,
            IDictionary<string, double> bestAccuracies,
            IList<GameplaySessionResult> recentSessions)
        {
            ProfileId = profileId;
            TotalPracticeSeconds = totalPracticeSeconds;
            CompletedSessionCount = completedSessionCount;
            BestAccuracies = new Dictionary<string, double>(bestAccuracies, StringComparer.Ordinal);
            RecentSessions = new List<GameplaySessionResult>(recentSessions);
        }

        internal string ProfileId { get; }
        internal double TotalPracticeSeconds { get; private set; }
        internal long CompletedSessionCount { get; private set; }
        internal Dictionary<string, double> BestAccuracies { get; }
        internal List<GameplaySessionResult> RecentSessions { get; }

        internal static PlayerProgressState Empty() => new PlayerProgressState(
            Guid.NewGuid().ToString("N"), 0, 0,
            new Dictionary<string, double>(StringComparer.Ordinal),
            new List<GameplaySessionResult>());

        internal GameplayProgressSnapshot Snapshot() => new GameplayProgressSnapshot(
            ProfileId, TotalPracticeSeconds, CompletedSessionCount, BestAccuracies, RecentSessions);

        internal void RecordBest(string key, double accuracy)
        {
            if (!BestAccuracies.TryGetValue(key, out double previous) || accuracy > previous)
                BestAccuracies[key] = accuracy;
        }

        internal void AddPracticeTime(double seconds)
        {
            double next = TotalPracticeSeconds + seconds;
            if (!GameplayProgressService.IsFinite(next) || next > GameplayProgressService.MaximumLifetimeSeconds)
                throw new InvalidOperationException("Lifetime practice duration exceeds the supported range.");
            TotalPracticeSeconds = next;
        }

        internal void AddCompletedSession(GameplaySessionResult result)
        {
            if (RecentSessions.Any(value => string.Equals(value.SessionId, result.SessionId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Session '{result.SessionId}' is already recorded.");
            CompletedSessionCount = checked(CompletedSessionCount + 1);
            RecentSessions.Add(result);
            if (RecentSessions.Count > GameplayProgressService.MaximumRecentSessions)
                RecentSessions.RemoveAt(0);
        }
    }

    public static class GameplayDurationFormatter
    {
        public static string Format(double totalSeconds)
        {
            if (!GameplayProgressService.IsFinite(totalSeconds) || totalSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(totalSeconds));
            long minutes = (long)Math.Floor(totalSeconds / 60.0);
            long hours = minutes / 60;
            long remainingMinutes = minutes % 60;
            if (hours == 0) return $"{remainingMinutes}m";
            return $"{hours}h {remainingMinutes:00}m";
        }
    }

    public sealed class GameplayPracticeTimer
    {
        public const double FlushIntervalSeconds = 15.0;
        public const double MaximumCountedFrameSeconds = 1.0;

        public double PendingSeconds { get; private set; }
        public double CurrentAttemptSeconds { get; private set; }

        public bool Tick(GameplayRunState state, bool applicationFocused, double unscaledDeltaSeconds)
        {
            if (state != GameplayRunState.Playing || !applicationFocused) return false;
            if (!GameplayProgressService.IsFinite(unscaledDeltaSeconds) || unscaledDeltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaSeconds));
            double counted = Math.Min(unscaledDeltaSeconds, MaximumCountedFrameSeconds);
            PendingSeconds += counted;
            CurrentAttemptSeconds += counted;
            return PendingSeconds >= FlushIntervalSeconds;
        }

        public double DrainPending()
        {
            double value = PendingSeconds;
            PendingSeconds = 0;
            return value;
        }

        public void ResetAttempt() => CurrentAttemptSeconds = 0;
    }
}
