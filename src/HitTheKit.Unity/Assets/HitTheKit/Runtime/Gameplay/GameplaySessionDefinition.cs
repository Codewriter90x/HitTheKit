using System;
using System.Collections.Generic;

namespace HitTheKit.Unity.Gameplay
{
    public static class GameplaySongSpeeds
    {
        // Keep the original practice speeds loadable for existing sessions while exposing
        // the finer 60-100% learning progression in the song-selection UI.
        private static readonly double[] values =
        {
            0.25,
            0.5,
            0.6,
            0.7,
            0.75,
            0.8,
            0.9,
            1.0
        };
        private static readonly IReadOnlyList<double> readOnlyValues = Array.AsReadOnly(values);

        public static IReadOnlyList<double> All => readOnlyValues;

        public static bool IsSupported(double value)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (Math.Abs(values[index] - value) < 0.0001) return true;
            }
            return false;
        }
    }

    public enum GameplaySessionChart
    {
        DemoSong,
        FirstPulse,
        Backbeat,
        Timekeeper,
        FirstGroove,
        SchoolLesson,
        ExternalFile
    }

    public enum GameplayReturnTarget
    {
        MainMenu,
        LearningPath,
        SongLibrary
    }

    public sealed class GameplaySessionDefinition
    {
        public GameplaySessionDefinition(
            GameplaySessionKind kind,
            GameplaySessionChart chart,
            string difficulty,
            double chartPlaybackSpeed,
            double bpm,
            int bars,
            int beatsPerBar,
            int countInBeats,
            bool useGeneratedSong,
            GameplayPresentationTheme theme,
            GameplayReturnTarget returnTarget,
            string title,
            string subtitle,
            string metadata,
            string kicker,
            string returnButtonLabel,
            GameplayLessonId? lessonId = null,
            string songId = null,
            string chartFilePath = null,
            string audioFilePath = null)
        {
            if (!Enum.IsDefined(typeof(GameplaySessionKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(GameplaySessionChart), chart)) throw new ArgumentOutOfRangeException(nameof(chart));
            if (!Enum.IsDefined(typeof(GameplayPresentationTheme), theme)) throw new ArgumentOutOfRangeException(nameof(theme));
            if (!Enum.IsDefined(typeof(GameplayReturnTarget), returnTarget)) throw new ArgumentOutOfRangeException(nameof(returnTarget));
            if (string.IsNullOrWhiteSpace(difficulty)) throw new ArgumentException("Difficulty is required.", nameof(difficulty));
            if (!IsFinite(chartPlaybackSpeed) || chartPlaybackSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(chartPlaybackSpeed));
            if (!IsFinite(bpm) || bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));
            if (bars <= 0) throw new ArgumentOutOfRangeException(nameof(bars));
            if (beatsPerBar <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));
            if (countInBeats <= 0) throw new ArgumentOutOfRangeException(nameof(countInBeats));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(subtitle)) throw new ArgumentException("Subtitle is required.", nameof(subtitle));
            if (string.IsNullOrWhiteSpace(metadata)) throw new ArgumentException("Metadata is required.", nameof(metadata));
            if (string.IsNullOrWhiteSpace(kicker)) throw new ArgumentException("Kicker is required.", nameof(kicker));
            if (string.IsNullOrWhiteSpace(returnButtonLabel))
                throw new ArgumentException("Return button label is required.", nameof(returnButtonLabel));

            if (kind == GameplaySessionKind.Lesson)
            {
                if (!lessonId.HasValue || !GameplayLearningPath.Find(lessonId.Value).IsPlayable)
                    throw new ArgumentException("A lesson session requires a playable lesson.", nameof(lessonId));
                if (!GameplayStudySpeeds.IsSupported(chartPlaybackSpeed))
                    throw new ArgumentOutOfRangeException(nameof(chartPlaybackSpeed));
            }
            else if (lessonId.HasValue)
            {
                throw new ArgumentException("A non-lesson session cannot reference a lesson.", nameof(lessonId));
            }

            if (chart == GameplaySessionChart.ExternalFile)
            {
                if (kind != GameplaySessionKind.FreePlay)
                    throw new ArgumentException("External songs must use a free-play session.", nameof(kind));
                if (string.IsNullOrWhiteSpace(songId)) throw new ArgumentException("External songs require a song ID.", nameof(songId));
                if (string.IsNullOrWhiteSpace(chartFilePath))
                    throw new ArgumentException("External songs require a chart file.", nameof(chartFilePath));
                if (string.IsNullOrWhiteSpace(audioFilePath))
                    throw new ArgumentException("External songs require an audio file.", nameof(audioFilePath));
            }
            else if (!string.IsNullOrEmpty(chartFilePath) || !string.IsNullOrEmpty(audioFilePath))
            {
                throw new ArgumentException("Only external songs can reference chart or audio files.");
            }

            if (!string.IsNullOrWhiteSpace(songId) && !GameplaySongSpeeds.IsSupported(chartPlaybackSpeed))
                throw new ArgumentOutOfRangeException(nameof(chartPlaybackSpeed));

            Kind = kind;
            Chart = chart;
            Difficulty = difficulty;
            ChartPlaybackSpeed = chartPlaybackSpeed;
            Bpm = bpm;
            Bars = bars;
            BeatsPerBar = beatsPerBar;
            CountInBeats = countInBeats;
            UseGeneratedSong = useGeneratedSong;
            Theme = theme;
            ReturnTarget = returnTarget;
            Title = title;
            Subtitle = subtitle;
            Metadata = metadata;
            Kicker = kicker;
            ReturnButtonLabel = returnButtonLabel;
            LessonId = lessonId;
            SongId = songId;
            ChartFilePath = chartFilePath;
            AudioFilePath = audioFilePath;
        }

        public GameplaySessionKind Kind { get; }
        public GameplaySessionChart Chart { get; }
        public string Difficulty { get; }
        public double ChartPlaybackSpeed { get; }
        public double SpeedMultiplier => ChartPlaybackSpeed;
        public double Bpm { get; }
        public int Bars { get; }
        public int BeatsPerBar { get; }
        public int CountInBeats { get; }
        public bool UseGeneratedSong { get; }
        public GameplayPresentationTheme Theme { get; }
        public GameplayReturnTarget ReturnTarget { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Metadata { get; }
        public string Kicker { get; }
        public string ReturnButtonLabel { get; }
        public GameplayLessonId? LessonId { get; }
        public string SongId { get; }
        public string ChartFilePath { get; }
        public string AudioFilePath { get; }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public static class GameplaySessionFactory
    {
        private const int BeatsPerBar = 4;
        private const int CountInBeats = 4;
        public const double MinimumSongCountInSeconds = 6.0;

        public static GameplaySessionDefinition FreePlay(GameplayPresentationTheme theme) =>
            new GameplaySessionDefinition(
                GameplaySessionKind.FreePlay,
                GameplaySessionChart.DemoSong,
                "easy",
                1.0,
                120,
                8,
                BeatsPerBar,
                CountInBeats,
                true,
                theme,
                GameplayReturnTarget.MainMenu,
                "NEON CIRCUIT",
                "DEMO STAGE",
                "DEMO STAGE · EASY · 120 BPM",
                "MODALITÀ GIOCO · LA BATTERIA SUONA SOLO QUANDO LA COLPISCI",
                "MENU PRINCIPALE");

        public static GameplaySessionDefinition Song(
            SongLibraryEntry song,
            GameplayPresentationTheme theme,
            double speedMultiplier = 1.0,
            string difficulty = null)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            if (!song.IsPlayable) throw new InvalidOperationException($"Song '{song.Id}' is not playable.");
            if (!song.Bpm.HasValue || !song.Bars.HasValue || !song.BeatsPerBar.HasValue)
                throw new InvalidOperationException($"Song '{song.Id}' has no verified timing metadata.");
            if (!GameplaySongSpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            string selectedDifficulty = string.IsNullOrWhiteSpace(difficulty)
                ? song.AvailableDifficulties.Count > 0 ? song.AvailableDifficulties[0] : null
                : difficulty;
            if (string.IsNullOrWhiteSpace(selectedDifficulty) ||
                !ContainsDifficulty(song.AvailableDifficulties, selectedDifficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty), "The selected difficulty is not available for this song.");

            bool generated = song.UsesGeneratedBacking;
            double effectiveBpm = song.Bpm.Value * speedMultiplier;
            int countInBeats = SongCountInBeats(effectiveBpm);
            string difficultyHint = string.IsNullOrWhiteSpace(song.DifficultyHint)
                ? "unrated"
                : song.DifficultyHint;
            return new GameplaySessionDefinition(
                GameplaySessionKind.FreePlay,
                generated ? GameplaySessionChart.DemoSong : GameplaySessionChart.ExternalFile,
                selectedDifficulty,
                speedMultiplier,
                effectiveBpm,
                song.Bars.Value,
                song.BeatsPerBar.Value,
                countInBeats,
                generated,
                theme,
                GameplayReturnTarget.SongLibrary,
                song.Title.ToUpperInvariant(),
                song.Artist.ToUpperInvariant(),
                $"{song.Artist.ToUpperInvariant()} · {selectedDifficulty.ToUpperInvariant()} · " +
                $"{difficultyHint.ToUpperInvariant()} · {effectiveBpm:0.#} BPM · {speedMultiplier:0.##}×",
                "SETLIST · ASCOLTA LA BASE, SUONA TU LA BATTERIA",
                "TORNA AI BRANI",
                null,
                song.Id,
                song.ChartPath,
                song.AudioPath);
        }

        public static int SongCountInBeats(double effectiveBpm)
        {
            if (double.IsNaN(effectiveBpm) || double.IsInfinity(effectiveBpm) || effectiveBpm <= 0)
                throw new ArgumentOutOfRangeException(nameof(effectiveBpm));
            return Math.Max(CountInBeats, (int)Math.Ceiling(MinimumSongCountInSeconds * effectiveBpm / 60.0));
        }

        private static bool ContainsDifficulty(IReadOnlyList<string> values, string selected)
        {
            if (values == null) return false;
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], selected, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static GameplaySessionDefinition Lesson(
            GameplayLessonId id,
            double speedMultiplier,
            GameplayPresentationTheme theme)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(id);
            if (!lesson.IsPlayable) throw new InvalidOperationException($"Lesson '{id}' is not available yet.");
            if (!GameplayStudySpeeds.IsSupported(speedMultiplier))
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier));

            return new GameplaySessionDefinition(
                GameplaySessionKind.Lesson,
                ChartFor(id),
                "easy",
                speedMultiplier,
                lesson.Bpm * speedMultiplier,
                lesson.Bars,
                BeatsPerBar,
                CountInBeats,
                true,
                theme,
                GameplayReturnTarget.LearningPath,
                lesson.ItalianTitle.ToUpperInvariant(),
                lesson.ItalianDescription,
                $"LEZIONE {lesson.Number}/{GameplayLearningPath.All.Count} · {lesson.Focus} · " +
                $"{lesson.Bpm * speedMultiplier:0} BPM · {speedMultiplier:0.##}×",
                "PERCORSO IMPARA · ASCOLTA LA BASE, SUONA TU LA BATTERIA",
                "TORNA A IMPARA",
                id);
        }

        private static GameplaySessionChart ChartFor(GameplayLessonId id)
        {
            GameplayLessonDefinition lesson = GameplayLearningPath.Find(id);
            if (!lesson.IsPlayable) throw new ArgumentOutOfRangeException(nameof(id));
            return GameplaySessionChart.SchoolLesson;
        }
    }

    public static class GameplaySessionContext
    {
        private static GameplaySessionDefinition current =
            GameplaySessionFactory.FreePlay(GameplayPresentationTheme.ArcadeNeon);

        public static GameplaySessionDefinition Current => current;

        public static void Select(GameplaySessionDefinition session) =>
            current = session ?? throw new ArgumentNullException(nameof(session));

        public static void SelectFreePlay() =>
            SelectFreePlay(GameplaySettingsRuntime.Current.Theme);

        public static void SelectFreePlay(GameplayPresentationTheme theme) =>
            Select(GameplaySessionFactory.FreePlay(theme));

        public static void SelectSong(
            SongLibraryEntry song,
            double speedMultiplier = 1.0,
            string difficulty = null) =>
            Select(GameplaySessionFactory.Song(
                song,
                GameplaySettingsRuntime.Current.Theme,
                speedMultiplier,
                difficulty));

        public static void SelectLesson(GameplayLessonId id, double speedMultiplier = 1.0) =>
            SelectLesson(id, speedMultiplier, GameplaySettingsRuntime.Current.Theme);

        public static void SelectLesson(
            GameplayLessonId id,
            double speedMultiplier,
            GameplayPresentationTheme theme) =>
            Select(GameplaySessionFactory.Lesson(id, speedMultiplier, theme));

        public static void Reset() =>
            Select(GameplaySessionFactory.FreePlay(GameplayPresentationTheme.ArcadeNeon));
    }
}
