using System;
using System.Linq;
using HitTheKit.Unity.MainMenu;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using HitTheKit.Core;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class MainMenuTests
    {
        [Test]
        public void Defines_exactly_three_unique_destinations_and_stable_routes()
        {
            MainMenuDestination[] destinations = Enum.GetValues(typeof(MainMenuDestination))
                .Cast<MainMenuDestination>()
                .ToArray();
            Assert.That(destinations, Has.Length.EqualTo(3));
            Assert.That(destinations, Is.Unique);
            Assert.That(MainMenuRoutes.MainMenuScene, Is.EqualTo("MainMenuPrototype"));
            Assert.That(MainMenuRoutes.GameplayScene, Is.EqualTo("GameplayPrototype"));
            Assert.That(MainMenuRoutes.DeviceSetupScene, Is.EqualTo("DeviceSetupPrototype"));
        }

        [TestCase(MainMenuLanguage.Italian, "GIOCA", "IMPARA", "CONFIGURA BATTERIA")]
        [TestCase(MainMenuLanguage.English, "PLAY", "LEARN", "SET UP DRUMS")]
        public void Localized_content_covers_each_destination(
            MainMenuLanguage language,
            string play,
            string learn,
            string setup)
        {
            MainMenuContent content = MainMenuContent.For(language);
            Assert.That(content.Destination(MainMenuDestination.Play).Title, Is.EqualTo(play));
            Assert.That(content.Destination(MainMenuDestination.Learn).Title, Is.EqualTo(learn));
            Assert.That(content.Destination(MainMenuDestination.DeviceSetup).Title, Is.EqualTo(setup));
            Assert.That(content.Destination(MainMenuDestination.Play).Subtitle, Is.Not.Empty);
            Assert.That(content.Destination(MainMenuDestination.Learn).Subtitle, Is.Not.Empty);
            Assert.That(content.Destination(MainMenuDestination.DeviceSetup).Subtitle, Is.Not.Empty);
        }

        [Test]
        public void Unknown_language_and_destination_are_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MainMenuContent.For((MainMenuLanguage)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MainMenuContent.For(MainMenuLanguage.Italian).Destination((MainMenuDestination)99));
        }

        [Test]
        public void Learn_destination_exposes_a_school_curriculum_with_a_playable_first_semester()
        {
            Assert.That(GameplayLearningPath.All, Has.Count.EqualTo(24));
            Assert.That(GameplayLearningPath.All.Count(lesson => lesson.IsPlayable), Is.EqualTo(12));
            Assert.That(GameplayLearningPath.All.First().Focus, Is.EqualTo("KICK"));
            Assert.That(GameplayLearningPath.All[3].Focus, Is.EqualTo("FIRST GROOVE"));
            Assert.That(GameplayLearningPath.All.Select(lesson => lesson.Discipline),
                Does.Contain(GameplayLessonDiscipline.Technique));
            Assert.That(GameplayLearningPath.All.Select(lesson => lesson.Discipline),
                Does.Contain(GameplayLessonDiscipline.Reading));
        }

        [Test]
        public void Double_kick_confirm_requires_two_intentional_consecutive_kick_hits()
        {
            var gesture = new DoubleKickConfirmGesture();

            Assert.That(gesture.Register(DrumPad.Kick, 100, 10.0), Is.False);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 10.24), Is.True);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 11.0), Is.False, "A completed gesture must reset.");
            Assert.That(gesture.Register(DrumPad.Snare, 100, 11.1), Is.False, "A non-kick hit breaks the gesture.");
            Assert.That(gesture.Register(DrumPad.Kick, 100, 11.2), Is.False);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 11.8), Is.False, "A slow pair must not confirm.");
        }

        [Test]
        public void Double_kick_confirm_rejects_zero_velocity_and_duplicate_bounce()
        {
            var gesture = new DoubleKickConfirmGesture();

            Assert.That(gesture.Register(DrumPad.Kick, 0, 4.0), Is.False);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 5.0), Is.False);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 5.01), Is.False);
            Assert.That(gesture.Register(DrumPad.Kick, 100, 5.25), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => gesture.Register(DrumPad.Kick, 128, 6.0));
        }

        [Test]
        public void Stage_palettes_are_distinct_and_reject_unknown_themes()
        {
            MainMenuStagePalette arcade = MainMenuStagePalette.For(GameplayPresentationTheme.ArcadeNeon);
            MainMenuStagePalette concert = MainMenuStagePalette.For(GameplayPresentationTheme.ConcertStage);
            MainMenuStagePalette precision = MainMenuStagePalette.For(GameplayPresentationTheme.PrecisionGrid);

            Assert.That(arcade.Accent, Is.Not.EqualTo(concert.Accent));
            Assert.That(concert.Accent, Is.Not.EqualTo(precision.Accent));
            Assert.That(arcade.AtmosphereRate, Is.GreaterThan(0f));
            Assert.That(concert.SpotIntensity, Is.GreaterThan(arcade.SpotIntensity));
            Assert.Throws<ArgumentOutOfRangeException>(() => MainMenuStagePalette.For((GameplayPresentationTheme)99));
        }
    }
}
