using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public interface IGameplaySettingsPersistence
    {
        bool TryLoad(out string json);
        void Save(string json);
    }

    public sealed class InMemoryGameplaySettingsPersistence : IGameplaySettingsPersistence
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

    public sealed class AtomicJsonFileGameplaySettingsPersistence : IGameplaySettingsPersistence
    {
        private const long MaximumDocumentBytes = 64 * 1024;
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);
        private readonly string path;

        public AtomicJsonFileGameplaySettingsPersistence(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A settings path is required.", nameof(path));
            this.path = Path.GetFullPath(path);
        }

        public bool TryLoad(out string json)
        {
            if (!File.Exists(path))
            {
                json = null;
                return false;
            }

            var info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Settings document size is outside the supported range.");
            json = File.ReadAllText(path, Utf8NoBom);
            return true;
        }

        public void Save(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Settings JSON is required.", nameof(json));
            byte[] bytes = Utf8NoBom.GetBytes(json);
            if (bytes.Length > MaximumDocumentBytes)
                throw new InvalidOperationException("Settings document exceeds the maximum size.");

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Settings path requires a directory.");
            Directory.CreateDirectory(directory);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }

    public sealed class GameplaySettingsSnapshot
    {
        internal GameplaySettingsSnapshot(GameplayPresentationTheme theme)
        {
            Theme = theme;
        }

        public GameplayPresentationTheme Theme { get; }
    }

    public sealed class GameplaySettingsService
    {
        public const int SupportedSchemaVersion = 1;
        private readonly IGameplaySettingsPersistence persistence;
        private GameplayPresentationTheme theme = GameplayPresentationTheme.ArcadeNeon;

        public GameplaySettingsService(IGameplaySettingsPersistence persistence)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            try
            {
                if (persistence.TryLoad(out string json)) theme = Deserialize(json);
            }
            catch (Exception exception)
            {
                LastError = $"Saved settings could not be loaded: {exception.Message}";
            }
        }

        public GameplaySettingsSnapshot Snapshot => new GameplaySettingsSnapshot(theme);
        public GameplayPresentationTheme Theme => theme;
        public string LastError { get; private set; } = string.Empty;

        public void SelectTheme(GameplayPresentationTheme selectedTheme)
        {
            ValidateTheme(selectedTheme);
            string json = Serialize(selectedTheme);
            persistence.Save(json);
            theme = selectedTheme;
            LastError = string.Empty;
        }

        internal static string Serialize(GameplayPresentationTheme selectedTheme)
        {
            ValidateTheme(selectedTheme);
            return JsonUtility.ToJson(new GameplaySettingsDocument
            {
                schemaVersion = SupportedSchemaVersion,
                theme = selectedTheme.ToString()
            }, true) + "\n";
        }

        internal static GameplayPresentationTheme Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Settings JSON is empty.");
            if (!json.Contains("\"schemaVersion\"") || !json.Contains("\"theme\""))
                throw new InvalidOperationException("Settings JSON is missing required fields.");

            GameplaySettingsDocument document;
            try
            {
                document = JsonUtility.FromJson<GameplaySettingsDocument>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Settings JSON is malformed.", exception);
            }

            if (document == null) throw new InvalidOperationException("Settings JSON did not contain a document.");
            if (document.schemaVersion != SupportedSchemaVersion)
                throw new InvalidOperationException($"Unsupported settings schema version '{document.schemaVersion}'.");
            if (!Enum.TryParse(document.theme, false, out GameplayPresentationTheme parsed))
                throw new InvalidOperationException($"Unknown gameplay theme '{document.theme}'.");
            ValidateTheme(parsed);
            return parsed;
        }

        private static void ValidateTheme(GameplayPresentationTheme value)
        {
            if (!Enum.IsDefined(typeof(GameplayPresentationTheme), value))
                throw new ArgumentOutOfRangeException(nameof(value));
        }

        [Serializable]
        private sealed class GameplaySettingsDocument
        {
            public int schemaVersion;
            public string theme;
        }
    }

    public static class GameplaySettingsRuntime
    {
        private static GameplaySettingsService current;

        public static GameplaySettingsService Current => current ??= CreateDefault();
        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath,
            "HitTheKit",
            "gameplay-settings.json");

        public static void UseForTests(GameplaySettingsService service) =>
            current = service ?? throw new ArgumentNullException(nameof(service));

        public static void ResetForTests() => current = null;

        private static GameplaySettingsService CreateDefault() =>
            new GameplaySettingsService(new AtomicJsonFileGameplaySettingsPersistence(DefaultPath));
    }
}
