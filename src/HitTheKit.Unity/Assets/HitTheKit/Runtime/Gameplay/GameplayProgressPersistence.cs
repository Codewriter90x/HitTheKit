using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class GameplayProgressLoadException : Exception
    {
        public GameplayProgressLoadException(string message) : base(message) { }
        public GameplayProgressLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class AtomicJsonFileGameplayProgressPersistence : IGameplayProgressPersistence
    {
        public const long MaximumDocumentBytes = 5 * 1024 * 1024;
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);
        private readonly string path;

        public AtomicJsonFileGameplayProgressPersistence(string path)
        {
            this.path = RequirePath(path);
        }

        public bool TryLoad(out string json)
        {
            if (!File.Exists(path))
            {
                json = null;
                return false;
            }

            json = ReadExternal(path);
            return true;
        }

        public void Save(string json) => WriteExternal(path, json);

        public static string ReadExternal(string path)
        {
            string safePath = RequirePath(path);
            var info = new FileInfo(safePath);
            if (!info.Exists) throw new FileNotFoundException("Progress backup file does not exist.", safePath);
            if (info.Length <= 0 || info.Length > MaximumDocumentBytes)
                throw new GameplayProgressLoadException("Progress document size is outside the supported range.");
            return File.ReadAllText(safePath, Utf8NoBom);
        }

        public static void WriteExternal(string path, string json)
        {
            string safePath = RequirePath(path);
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Progress JSON is required.", nameof(json));
            byte[] bytes = Utf8NoBom.GetBytes(json);
            if (bytes.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Progress document exceeds the maximum backup size.");

            string directory = Path.GetDirectoryName(safePath);
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Progress path requires a directory.", nameof(path));
            Directory.CreateDirectory(directory);
            string temporaryPath = safePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(safePath)) File.Replace(temporaryPath, safePath, null);
                else File.Move(temporaryPath, safePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        private static string RequirePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A progress file path is required.", nameof(value));
            return Path.GetFullPath(value);
        }
    }

    internal static class GameplayProgressJson
    {
        internal static string Serialize(PlayerProgressState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var document = new PlayerProgressDocumentDto
            {
                schemaVersion = GameplayProgressService.SupportedSchemaVersion,
                profileId = state.ProfileId,
                totalPracticeSeconds = state.TotalPracticeSeconds,
                completedSessionCount = state.CompletedSessionCount,
                bestResults = new List<LessonBestResultDto>(),
                recentSessions = new List<GameplaySessionResultDto>()
            };

            var bestKeys = new List<string>(state.BestAccuracies.Keys);
            bestKeys.Sort(StringComparer.Ordinal);
            for (int index = 0; index < bestKeys.Count; index++)
            {
                ParseBestKey(bestKeys[index], out GameplayLessonId lessonId, out double speedMultiplier);
                document.bestResults.Add(new LessonBestResultDto
                {
                    lessonId = lessonId.ToString(),
                    speedMultiplier = speedMultiplier,
                    accuracy = state.BestAccuracies[bestKeys[index]]
                });
            }

            for (int index = 0; index < state.RecentSessions.Count; index++)
            {
                GameplaySessionResult result = state.RecentSessions[index];
                document.recentSessions.Add(new GameplaySessionResultDto
                {
                    sessionId = result.SessionId,
                    completedAtUtc = result.CompletedAtUtc,
                    kind = result.Kind.ToString(),
                    lessonId = result.LessonId.HasValue ? result.LessonId.Value.ToString() : string.Empty,
                    speedMultiplier = result.SpeedMultiplier,
                    activeSeconds = result.ActiveSeconds,
                    score = result.Score,
                    accuracy = result.Accuracy,
                    maxCombo = result.MaxCombo,
                    perfectCount = result.PerfectCount,
                    goodCount = result.GoodCount,
                    earlyCount = result.EarlyCount,
                    lateCount = result.LateCount,
                    missCount = result.MissCount
                });
            }

            return JsonUtility.ToJson(document, true) + "\n";
        }

        internal static PlayerProgressState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new GameplayProgressLoadException("Progress JSON is empty.");
            RequireTopLevelFields(json);
            PlayerProgressDocumentDto document;
            try
            {
                document = JsonUtility.FromJson<PlayerProgressDocumentDto>(json);
            }
            catch (Exception exception)
            {
                throw new GameplayProgressLoadException("Progress JSON is malformed.", exception);
            }

            if (document == null) throw new GameplayProgressLoadException("Progress JSON did not contain a document.");
            if (document.schemaVersion != GameplayProgressService.SupportedSchemaVersion)
                throw new GameplayProgressLoadException($"Unsupported progress schema version '{document.schemaVersion}'.");
            if (!Guid.TryParseExact(document.profileId, "N", out _))
                throw new GameplayProgressLoadException("profileId must be a canonical GUID without separators.");
            if (!GameplayProgressService.IsFinite(document.totalPracticeSeconds) ||
                document.totalPracticeSeconds < 0 ||
                document.totalPracticeSeconds > GameplayProgressService.MaximumLifetimeSeconds)
                throw new GameplayProgressLoadException("totalPracticeSeconds is outside the supported range.");
            if (document.completedSessionCount < 0)
                throw new GameplayProgressLoadException("completedSessionCount cannot be negative.");
            if (document.bestResults == null) throw new GameplayProgressLoadException("bestResults is required.");
            if (document.recentSessions == null) throw new GameplayProgressLoadException("recentSessions is required.");
            if (document.bestResults.Count > GameplayLearningPath.All.Count * GameplayStudySpeeds.All.Count)
                throw new GameplayProgressLoadException("bestResults contains too many entries.");
            if (document.recentSessions.Count > GameplayProgressService.MaximumRecentSessions)
                throw new GameplayProgressLoadException("recentSessions exceeds the supported history size.");
            if (document.completedSessionCount < document.recentSessions.Count)
                throw new GameplayProgressLoadException("completedSessionCount cannot be smaller than recentSessions.");

            var best = new Dictionary<string, double>(StringComparer.Ordinal);
            for (int index = 0; index < document.bestResults.Count; index++)
            {
                LessonBestResultDto item = document.bestResults[index]
                    ?? throw new GameplayProgressLoadException($"bestResults[{index}] is null.");
                GameplayLessonId lessonId = ParseExactEnum<GameplayLessonId>(
                    item.lessonId, $"bestResults[{index}].lessonId");
                if (!GameplayLearningPath.Find(lessonId).IsPlayable)
                    throw new GameplayProgressLoadException($"bestResults[{index}] references an unavailable lesson.");
                if (!GameplayStudySpeeds.IsSupported(item.speedMultiplier))
                    throw new GameplayProgressLoadException($"bestResults[{index}].speedMultiplier is unsupported.");
                if (!GameplayProgressService.IsFinite(item.accuracy) || item.accuracy < 0 || item.accuracy > 100)
                    throw new GameplayProgressLoadException($"bestResults[{index}].accuracy is invalid.");
                string key = GameplayProgressService.Key(lessonId, item.speedMultiplier);
                if (best.ContainsKey(key))
                    throw new GameplayProgressLoadException($"bestResults[{index}] duplicates '{key}'.");
                best.Add(key, item.accuracy);
            }

            var sessions = new List<GameplaySessionResult>(document.recentSessions.Count);
            var sessionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < document.recentSessions.Count; index++)
            {
                GameplaySessionResultDto item = document.recentSessions[index]
                    ?? throw new GameplayProgressLoadException($"recentSessions[{index}] is null.");
                GameplaySessionKind kind = ParseExactEnum<GameplaySessionKind>(
                    item.kind, $"recentSessions[{index}].kind");
                GameplayLessonId? lessonId = null;
                if (kind == GameplaySessionKind.Lesson)
                    lessonId = ParseExactEnum<GameplayLessonId>(item.lessonId, $"recentSessions[{index}].lessonId");
                else if (!string.IsNullOrEmpty(item.lessonId))
                    throw new GameplayProgressLoadException($"recentSessions[{index}].lessonId must be empty for free play.");

                GameplaySessionResult result;
                try
                {
                    result = new GameplaySessionResult(
                        item.sessionId, item.completedAtUtc, kind, lessonId, item.speedMultiplier,
                        item.activeSeconds, item.score, item.accuracy, item.maxCombo,
                        item.perfectCount, item.goodCount, item.earlyCount, item.lateCount, item.missCount);
                }
                catch (Exception exception)
                {
                    throw new GameplayProgressLoadException($"recentSessions[{index}] is invalid: {exception.Message}", exception);
                }

                if (!sessionIds.Add(result.SessionId))
                    throw new GameplayProgressLoadException($"recentSessions[{index}] duplicates sessionId '{result.SessionId}'.");
                sessions.Add(result);
            }

            return new PlayerProgressState(
                document.profileId, document.totalPracticeSeconds, document.completedSessionCount, best, sessions);
        }

        private static T ParseExactEnum<T>(string value, string field) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Enum.TryParse(value, false, out T parsed) ||
                !string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
                throw new GameplayProgressLoadException($"{field} is missing or invalid.");
            return parsed;
        }

        private static void ParseBestKey(string key, out GameplayLessonId lessonId, out double speedMultiplier)
        {
            string[] parts = key.Split(':');
            if (parts.Length != 2 || !Enum.TryParse(parts[0], false, out lessonId) ||
                !double.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out speedMultiplier))
                throw new InvalidOperationException($"Stored best-result key '{key}' is invalid.");
        }

        private static void RequireTopLevelFields(string json)
        {
            string[] required =
            {
                "\"schemaVersion\"", "\"profileId\"", "\"totalPracticeSeconds\"",
                "\"completedSessionCount\"", "\"bestResults\"", "\"recentSessions\""
            };
            for (int index = 0; index < required.Length; index++)
                if (json.IndexOf(required[index], StringComparison.Ordinal) < 0)
                    throw new GameplayProgressLoadException($"Required progress field {required[index]} is missing.");
        }
    }

    public static class GameplayProgressRuntime
    {
        private const string ProgressFileName = "player-progress.json";
        private const string BackupFileName = "HitTheKit-progress-backup.json";
        private const string RecoveryFileName = "HitTheKit-progress-before-import.json";
        private static GameplayProgressService current;

        public static GameplayProgressService Current
        {
            get
            {
                if (current == null) current = CreateDefault();
                return current;
            }
        }

        public static string DefaultBackupDirectory
        {
            get
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrWhiteSpace(documents)) documents = Application.persistentDataPath;
                return Path.Combine(documents, "HitTheKit Backups");
            }
        }

        public static string DefaultBackupPath => Path.Combine(DefaultBackupDirectory, BackupFileName);
        public static string DefaultRecoveryPath => Path.Combine(DefaultBackupDirectory, RecoveryFileName);

        public static GameplayProgressOperationResult ExportDefaultBackup() =>
            Current.ExportBackup(DefaultBackupPath);

        public static GameplayProgressOperationResult ImportDefaultBackup() =>
            Current.ImportBackup(DefaultBackupPath, DefaultRecoveryPath);

        public static void UseForTests(GameplayProgressService service)
        {
            current = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static void ResetForTests()
        {
            current = new GameplayProgressService(new InMemoryGameplayProgressPersistence());
        }

        private static GameplayProgressService CreateDefault()
        {
#if UNITY_EDITOR
            return new GameplayProgressService(new InMemoryGameplayProgressPersistence());
#else
            string path = Path.Combine(Application.persistentDataPath, ProgressFileName);
            return new GameplayProgressService(new AtomicJsonFileGameplayProgressPersistence(path));
#endif
        }
    }

    [Serializable]
    internal sealed class PlayerProgressDocumentDto
    {
        public int schemaVersion;
        public string profileId;
        public double totalPracticeSeconds;
        public long completedSessionCount;
        public List<LessonBestResultDto> bestResults;
        public List<GameplaySessionResultDto> recentSessions;
    }

    [Serializable]
    internal sealed class LessonBestResultDto
    {
        public string lessonId;
        public double speedMultiplier;
        public double accuracy;
    }

    [Serializable]
    internal sealed class GameplaySessionResultDto
    {
        public string sessionId;
        public string completedAtUtc;
        public string kind;
        public string lessonId;
        public double speedMultiplier;
        public double activeSeconds;
        public int score;
        public double accuracy;
        public int maxCombo;
        public int perfectCount;
        public int goodCount;
        public int earlyCount;
        public int lateCount;
        public int missCount;
    }
}
