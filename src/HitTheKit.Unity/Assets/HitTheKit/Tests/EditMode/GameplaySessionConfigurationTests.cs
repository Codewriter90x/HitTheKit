using System;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplaySessionConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplaySettingsRuntime.UseForTests(
                new GameplaySettingsService(new InMemoryGameplaySettingsPersistence()));
            GameplaySessionContext.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameplaySessionContext.Reset();
            GameplaySettingsRuntime.ResetForTests();
        }

        [Test]
        public void Settings_persist_the_selected_theme_and_restore_it_in_a_new_service()
        {
            var persistence = new InMemoryGameplaySettingsPersistence();
            var first = new GameplaySettingsService(persistence);

            first.SelectTheme(GameplayPresentationTheme.PrecisionGrid);
            var restored = new GameplaySettingsService(persistence);

            Assert.That(restored.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(persistence.Content, Does.Contain("\"schemaVersion\": 1"));
            Assert.That(persistence.Content, Does.Contain("\"theme\": \"PrecisionGrid\""));
        }

        [Test]
        public void Session_context_captures_settings_for_the_next_session_without_mutating_the_active_one()
        {
            var settings = new GameplaySettingsService(new InMemoryGameplaySettingsPersistence());
            GameplaySettingsRuntime.UseForTests(settings);
            GameplaySessionContext.SelectFreePlay();
            GameplaySessionDefinition active = GameplaySessionContext.Current;

            settings.SelectTheme(GameplayPresentationTheme.ConcertStage);

            Assert.That(active.Theme, Is.EqualTo(GameplayPresentationTheme.ArcadeNeon));
            Assert.That(GameplaySessionContext.Current, Is.SameAs(active));
            GameplaySessionContext.SelectFreePlay();
            Assert.That(GameplaySessionContext.Current.Theme, Is.EqualTo(GameplayPresentationTheme.ConcertStage));
        }

        [Test]
        public void Free_play_and_lesson_are_complete_definitions_for_the_same_gameplay_scene()
        {
            GameplaySessionDefinition freePlay =
                GameplaySessionFactory.FreePlay(GameplayPresentationTheme.ArcadeNeon);
            GameplaySessionDefinition lesson = GameplaySessionFactory.Lesson(
                GameplayLessonId.Backbeat,
                0.5,
                GameplayPresentationTheme.ConcertStage);

            Assert.That(freePlay.Kind, Is.EqualTo(GameplaySessionKind.FreePlay));
            Assert.That(freePlay.Chart, Is.EqualTo(GameplaySessionChart.DemoSong));
            Assert.That(freePlay.ReturnTarget, Is.EqualTo(GameplayReturnTarget.MainMenu));
            Assert.That(freePlay.LessonId, Is.Null);
            Assert.That(lesson.Kind, Is.EqualTo(GameplaySessionKind.Lesson));
            Assert.That(lesson.Chart, Is.EqualTo(GameplaySessionChart.SchoolLesson));
            Assert.That(lesson.Bpm, Is.EqualTo(34));
            Assert.That(lesson.ChartPlaybackSpeed, Is.EqualTo(0.5));
            Assert.That(lesson.ReturnTarget, Is.EqualTo(GameplayReturnTarget.LearningPath));
            Assert.That(lesson.Theme, Is.EqualTo(GameplayPresentationTheme.ConcertStage));
        }

        [Test]
        public void Session_definition_rejects_inconsistent_lesson_metadata()
        {
            Assert.Throws<ArgumentException>(() => new GameplaySessionDefinition(
                GameplaySessionKind.FreePlay,
                GameplaySessionChart.DemoSong,
                "easy",
                1,
                120,
                8,
                4,
                4,
                true,
                GameplayPresentationTheme.ArcadeNeon,
                GameplayReturnTarget.MainMenu,
                "TITLE",
                "SUBTITLE",
                "METADATA",
                "KICKER",
                "RETURN",
                GameplayLessonId.FirstPulse));
        }

        [Test]
        public void Any_valid_definition_can_be_published_without_the_gameplay_scene_knowing_its_launcher()
        {
            GameplaySessionDefinition session =
                GameplaySessionFactory.FreePlay(GameplayPresentationTheme.PrecisionGrid);

            GameplaySessionContext.Select(session);

            Assert.That(GameplaySessionContext.Current, Is.SameAs(session));
            Assert.Throws<ArgumentNullException>(() => GameplaySessionContext.Select(null));
        }

        [Test]
        public void Invalid_saved_theme_falls_back_without_becoming_an_active_setting()
        {
            var persistence = new InMemoryGameplaySettingsPersistence();
            persistence.Save("{\"schemaVersion\":1,\"theme\":\"arcadeneon\"}");

            var settings = new GameplaySettingsService(persistence);

            Assert.That(settings.Theme, Is.EqualTo(GameplayPresentationTheme.ArcadeNeon));
            Assert.That(settings.LastError, Does.Contain("Unknown gameplay theme"));
        }
    }
}
