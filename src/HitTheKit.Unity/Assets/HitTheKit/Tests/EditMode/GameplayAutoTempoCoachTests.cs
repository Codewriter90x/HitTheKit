using HitTheKit.Unity.Gameplay;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayAutoTempoCoachTests
    {
        [Test]
        public void Passing_song_attempt_advances_to_the_next_supported_step()
        {
            GameplayAutoTempoRecommendation result = GameplayAutoTempoCoach.Evaluate(
                SongSession(0.5, 60),
                91,
                2,
                1,
                100);

            Assert.That(result.Status, Is.EqualTo(GameplayAutoTempoStatus.Advance));
            Assert.That(result.CurrentSpeed, Is.EqualTo(0.5));
            Assert.That(result.NextSpeed, Is.EqualTo(0.6));
            Assert.That(result.CanAdvance, Is.True);
        }

        [TestCase(84.99, 0, 0)]
        [TestCase(95, 11, 0)]
        [TestCase(95, 0, 11)]
        public void Coach_repeats_when_quality_guardrails_are_not_met(
            double accuracy,
            int misses,
            int noMatches)
        {
            GameplayAutoTempoRecommendation result = GameplayAutoTempoCoach.Evaluate(
                SongSession(0.75, 90),
                accuracy,
                misses,
                noMatches,
                100);

            Assert.That(result.Status, Is.EqualTo(GameplayAutoTempoStatus.Repeat));
            Assert.That(result.NextSpeed, Is.EqualTo(0.75));
            Assert.That(result.CanAdvance, Is.False);
        }

        [Test]
        public void Full_speed_pass_is_mastered_without_an_invalid_next_step()
        {
            GameplayAutoTempoRecommendation result = GameplayAutoTempoCoach.Evaluate(
                SongSession(1, 120), 100, 0, 0, 16);

            Assert.That(result.Status, Is.EqualTo(GameplayAutoTempoStatus.Mastered));
            Assert.That(result.NextSpeed, Is.EqualTo(1));
        }

        [Test]
        public void Lessons_use_the_existing_study_speed_progression()
        {
            GameplaySessionDefinition lesson = GameplaySessionFactory.Lesson(
                GameplayLessonId.FirstPulse,
                0.5,
                GameplayPresentationTheme.PrecisionGrid);

            GameplayAutoTempoRecommendation result = GameplayAutoTempoCoach.Evaluate(
                lesson, 90, 0, 0, 16);

            Assert.That(result.NextSpeed, Is.EqualTo(0.75));
        }

        [Test]
        public void Unscoped_free_play_does_not_receive_a_false_tempo_recommendation()
        {
            GameplayAutoTempoRecommendation result = GameplayAutoTempoCoach.Evaluate(
                GameplaySessionFactory.FreePlay(GameplayPresentationTheme.ArcadeNeon),
                100,
                0,
                0,
                16);

            Assert.That(result.Status, Is.EqualTo(GameplayAutoTempoStatus.Unavailable));
            Assert.That(result.CanAdvance, Is.False);
        }

        [Test]
        public void Rebuilt_song_session_preserves_identity_and_original_tempo_math()
        {
            GameplaySessionDefinition original = SongSession(0.5, 60);

            GameplaySessionDefinition next = GameplaySessionFactory.AtSpeed(original, 0.6);

            Assert.That(next.SongId, Is.EqualTo(original.SongId));
            Assert.That(next.Difficulty, Is.EqualTo(original.Difficulty));
            Assert.That(next.Chart, Is.EqualTo(original.Chart));
            Assert.That(next.ChartPlaybackSpeed, Is.EqualTo(0.6));
            Assert.That(next.Bpm, Is.EqualTo(72).Within(0.000001));
            Assert.That(next.Metadata, Does.Contain("72 BPM"));
            Assert.That(next.Metadata, Does.Contain("0.6×"));
            Assert.That(next.CountInBeats, Is.EqualTo(GameplaySessionFactory.SongCountInBeats(72)));
        }

        [Test]
        public void Chart_creator_is_not_coached_and_rebuild_preserves_authoring_mode()
        {
            GameplaySessionDefinition authoring = ChartCreatorSession(0.5, 60);

            GameplayAutoTempoRecommendation recommendation = GameplayAutoTempoCoach.Evaluate(
                authoring, 100, 0, 0, 16);
            GameplaySessionDefinition rebuilt = GameplaySessionFactory.AtSpeed(authoring, 0.6);

            Assert.That(recommendation.Status, Is.EqualTo(GameplayAutoTempoStatus.Unavailable));
            Assert.That(recommendation.CanAdvance, Is.False);
            Assert.That(rebuilt.IsChartCreator, Is.True);
            Assert.That(rebuilt.SpeedMultiplier, Is.EqualTo(0.6));
        }

        private static GameplaySessionDefinition SongSession(double speed, double effectiveBpm) =>
            new GameplaySessionDefinition(
                GameplaySessionKind.FreePlay,
                GameplaySessionChart.DemoSong,
                "easy",
                speed,
                effectiveBpm,
                8,
                4,
                GameplaySessionFactory.SongCountInBeats(effectiveBpm),
                true,
                GameplayPresentationTheme.ArcadeNeon,
                GameplayReturnTarget.SongLibrary,
                "TEST SONG",
                "HITTHEKIT",
                $"HITTHEKIT · EASY · {effectiveBpm:0.#} BPM · {speed:0.##}×",
                "SETLIST",
                "TORNA AI BRANI",
                null,
                "test-song");

        private static GameplaySessionDefinition ChartCreatorSession(double speed, double effectiveBpm) =>
            new GameplaySessionDefinition(
                GameplaySessionKind.FreePlay,
                GameplaySessionChart.DemoSong,
                "easy",
                speed,
                effectiveBpm,
                8,
                4,
                GameplaySessionFactory.SongCountInBeats(effectiveBpm),
                true,
                GameplayPresentationTheme.ArcadeNeon,
                GameplayReturnTarget.SongLibrary,
                "TEST SONG",
                "HITTHEKIT",
                $"HITTHEKIT · EASY · {effectiveBpm:0.#} BPM · {speed:0.##}×",
                "CHART CREATOR",
                "TORNA AI BRANI",
                null,
                "test-song",
                null,
                null,
                true);
    }
}
