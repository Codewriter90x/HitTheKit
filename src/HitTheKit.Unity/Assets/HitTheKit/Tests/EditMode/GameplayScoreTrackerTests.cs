using HitTheKit.Core;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayScoreTrackerTests
    {
        [Test]
        public void Tracks_score_combo_accuracy_and_rank_from_canonical_match_results()
        {
            var tracker = new GameplayScoreTracker();
            var session = new HitMatchingSession(
                new[] { new ChartNote(1, DrumPad.Kick), new ChartNote(2, DrumPad.Snare) },
                new TimingWindows(0.04, 0.08, 0.14),
                0);
            session.HitResolved += tracker.Apply;

            session.ProcessInput(new DrumInputEvent(DrumPad.Kick, 100, 1), out _);
            session.ProcessMisses(2.2);

            GameplayScoreSnapshot snapshot = tracker.Snapshot;
            Assert.That(snapshot.Score, Is.EqualTo(1010));
            Assert.That(snapshot.Combo, Is.Zero);
            Assert.That(snapshot.MaxCombo, Is.EqualTo(1));
            Assert.That(snapshot.Accuracy, Is.EqualTo(50).Within(0.001));
            Assert.That(snapshot.Rank, Is.EqualTo("D"));
        }

        [Test]
        public void No_match_breaks_combo_without_counting_a_resolved_note()
        {
            var tracker = new GameplayScoreTracker();
            tracker.Apply(null);
            Assert.That(tracker.Snapshot.Resolved, Is.Zero);
            Assert.That(tracker.Snapshot.Accuracy, Is.EqualTo(100));
        }
    }
}
