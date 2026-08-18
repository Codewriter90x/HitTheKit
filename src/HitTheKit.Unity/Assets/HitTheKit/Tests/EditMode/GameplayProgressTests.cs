using System;
using System.IO;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayProgressTests
    {
        [SetUp]
        public void SetUp() => GameplayProgressRuntime.ResetForTests();

        [TearDown]
        public void TearDown() => GameplayProgressRuntime.ResetForTests();

        [Test]
        public void Progress_round_trip_preserves_practice_time_results_and_lesson_bests()
        {
            var persistence = new InMemoryGameplayProgressPersistence();
            var service = new GameplayProgressService(persistence);
            service.AddPracticeTime(3661.5);
            GameplaySessionResult result = LessonResult("11111111111111111111111111111111");
            service.RecordCompletedSession(result);

            var reloaded = new GameplayProgressService(persistence);
            GameplayProgressSnapshot snapshot = reloaded.Snapshot;
            Assert.That(snapshot.TotalPracticeSeconds, Is.EqualTo(3661.5));
            Assert.That(snapshot.CompletedSessionCount, Is.EqualTo(1));
            Assert.That(snapshot.RecentSessions, Has.Count.EqualTo(1));
            Assert.That(reloaded.BestAccuracy(GameplayLessonId.FirstPulse, 1.0), Is.EqualTo(93.75));
            Assert.That(reloaded.ExportJson(), Is.EqualTo(service.ExportJson()), "Current-schema JSON must be deterministic.");
        }

        [Test]
        public void Import_replaces_progress_only_after_validation_and_writes_recovery_backup()
        {
            string root = Path.Combine(Path.GetTempPath(), "hitthekit-progress-" + Guid.NewGuid().ToString("N"));
            string backup = Path.Combine(root, "backup.json");
            string recovery = Path.Combine(root, "before-import.json");
            try
            {
                var source = new GameplayProgressService(new InMemoryGameplayProgressPersistence());
                source.AddPracticeTime(7200);
                source.RecordCompletedSession(LessonResult("22222222222222222222222222222222"));
                Assert.That(source.ExportBackup(backup).Succeeded, Is.True);

                var targetPersistence = new InMemoryGameplayProgressPersistence();
                var target = new GameplayProgressService(targetPersistence);
                target.AddPracticeTime(60);
                string originalProfile = target.Snapshot.ProfileId;
                GameplayProgressOperationResult imported = target.ImportBackup(backup, recovery);

                Assert.That(imported.Succeeded, Is.True);
                Assert.That(target.Snapshot.TotalPracticeSeconds, Is.EqualTo(7200));
                Assert.That(target.Snapshot.ProfileId, Is.EqualTo(source.Snapshot.ProfileId));
                Assert.That(new GameplayProgressService(
                    new AtomicJsonFileGameplayProgressPersistence(recovery)).Snapshot.ProfileId,
                    Is.EqualTo(originalProfile));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Atomic_file_persistence_overwrites_cleanly_and_leaves_no_temporary_file()
        {
            string root = Path.Combine(Path.GetTempPath(), "hitthekit-progress-atomic-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(root, "player-progress.json");
            try
            {
                var service = new GameplayProgressService(new AtomicJsonFileGameplayProgressPersistence(path));
                service.AddPracticeTime(15);
                service.AddPracticeTime(45);

                var reloaded = new GameplayProgressService(new AtomicJsonFileGameplayProgressPersistence(path));
                Assert.That(reloaded.Snapshot.TotalPracticeSeconds, Is.EqualTo(60));
                Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Import_commit_failure_restores_progress_and_previous_persistence_error_state()
        {
            string root = Path.Combine(Path.GetTempPath(), "hitthekit-progress-rollback-" + Guid.NewGuid().ToString("N"));
            string backup = Path.Combine(root, "backup.json");
            try
            {
                var source = new GameplayProgressService(new InMemoryGameplayProgressPersistence());
                source.AddPracticeTime(600);
                Assert.That(source.ExportBackup(backup).Succeeded, Is.True);

                var failingPersistence = new FailingSavePersistence();
                var target = new GameplayProgressService(failingPersistence);
                target.AddPracticeTime(30);
                string before = target.ExportJson();
                string previousError = target.LastError;

                GameplayProgressOperationResult result = target.ImportBackup(
                    backup, Path.Combine(root, "before-import.json"));

                Assert.That(result.Succeeded, Is.False);
                Assert.That(target.ExportJson(), Is.EqualTo(before));
                Assert.That(target.LastError, Is.EqualTo(previousError));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Invalid_import_is_fail_closed_and_does_not_mutate_current_progress()
        {
            string root = Path.Combine(Path.GetTempPath(), "hitthekit-progress-invalid-" + Guid.NewGuid().ToString("N"));
            string backup = Path.Combine(root, "backup.json");
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(backup,
                    "{\"schemaVersion\":99,\"profileId\":\"11111111111111111111111111111111\",\"totalPracticeSeconds\":0,\"completedSessionCount\":0,\"bestResults\":[],\"recentSessions\":[]}");
                var service = new GameplayProgressService(new InMemoryGameplayProgressPersistence());
                service.AddPracticeTime(125);
                string before = service.ExportJson();

                GameplayProgressOperationResult result = service.ImportBackup(backup, Path.Combine(root, "recovery.json"));

                Assert.That(result.Succeeded, Is.False);
                Assert.That(service.ExportJson(), Is.EqualTo(before));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void Loader_rejects_missing_required_collections_duplicate_sessions_and_inconsistent_accuracy()
        {
            var validPersistence = new InMemoryGameplayProgressPersistence();
            var service = new GameplayProgressService(validPersistence);
            service.RecordCompletedSession(LessonResult("33333333333333333333333333333333"));
            string valid = service.ExportJson();

            AssertLoadFails(valid.Replace("\"bestResults\": [", "\"removedBestResults\": ["));
            AssertLoadFails(valid.Replace("\"recentSessions\": [", "\"removedRecentSessions\": ["));
            AssertLoadFails(valid.Replace("\"accuracy\": 93.75", "\"accuracy\": 99.0"));

            service.RecordCompletedSession(new GameplaySessionResult(
                "55555555555555555555555555555555",
                "2026-08-08T10:01:00.0000000+00:00",
                GameplaySessionKind.FreePlay,
                null,
                1.0,
                10,
                1000,
                100,
                1,
                1,
                0,
                0,
                0,
                0));
            string duplicate = service.ExportJson().Replace(
                "55555555555555555555555555555555",
                "33333333333333333333333333333333");
            AssertLoadFails(duplicate);
        }

        [Test]
        public void Practice_timer_counts_only_focused_playing_time_and_caps_stalled_frames()
        {
            var timer = new GameplayPracticeTimer();

            Assert.That(timer.Tick(GameplayRunState.Countdown, true, 5), Is.False);
            Assert.That(timer.Tick(GameplayRunState.Paused, true, 5), Is.False);
            Assert.That(timer.Tick(GameplayRunState.Playing, false, 5), Is.False);
            Assert.That(timer.Tick(GameplayRunState.Playing, true, 10), Is.False);
            Assert.That(timer.PendingSeconds, Is.EqualTo(1), "A stalled frame must not add unattended time.");
            for (int index = 0; index < 14; index++) timer.Tick(GameplayRunState.Playing, true, 1);
            Assert.That(timer.PendingSeconds, Is.EqualTo(15));
            Assert.That(timer.Tick(GameplayRunState.Playing, true, 0.1), Is.True);
            Assert.That(timer.DrainPending(), Is.EqualTo(15.1).Within(0.0001));
            Assert.That(timer.PendingSeconds, Is.Zero);
            Assert.That(timer.CurrentAttemptSeconds, Is.EqualTo(15.1).Within(0.0001));
            timer.ResetAttempt();
            Assert.That(timer.CurrentAttemptSeconds, Is.Zero);
        }

        [Test]
        public void Existing_learning_progress_api_uses_the_persistent_service_boundary()
        {
            var persistence = new InMemoryGameplayProgressPersistence();
            GameplayProgressRuntime.UseForTests(new GameplayProgressService(persistence));

            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 88.5);

            Assert.That(new GameplayProgressService(persistence)
                .BestAccuracy(GameplayLessonId.FirstPulse, 1.0), Is.EqualTo(88.5));
            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.FirstPulse), Is.True);
        }

        [TestCase(0, "0m")]
        [TestCase(3599, "59m")]
        [TestCase(3600, "1h 00m")]
        [TestCase(7380, "2h 03m")]
        public void Practice_duration_has_a_compact_stable_format(double seconds, string expected)
        {
            Assert.That(GameplayDurationFormatter.Format(seconds), Is.EqualTo(expected));
        }

        private static GameplaySessionResult LessonResult(string id) => new GameplaySessionResult(
            id,
            "2026-08-08T10:00:00.0000000+00:00",
            GameplaySessionKind.Lesson,
            GameplayLessonId.FirstPulse,
            1.0,
            30,
            4000,
            93.75,
            4,
            3,
            1,
            0,
            0,
            0);

        private static void AssertLoadFails(string json)
        {
            var persistence = new InMemoryGameplayProgressPersistence();
            persistence.Save(json);
            var service = new GameplayProgressService(persistence);
            Assert.That(service.HasLoadError, Is.True);
            Assert.That(service.Snapshot.CompletedSessionCount, Is.Zero);
        }

        private sealed class FailingSavePersistence : IGameplayProgressPersistence
        {
            public bool TryLoad(out string json)
            {
                json = null;
                return false;
            }

            public void Save(string json) => throw new IOException("Synthetic save failure.");
        }
    }
}
