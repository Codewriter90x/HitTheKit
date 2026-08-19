using System;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayPracticeLoopTests
    {
        [Test]
        public void Manual_loop_requires_A_before_B_and_restarts_at_inclusive_end()
        {
            var loop = new GameplayPracticeLoop();
            Assert.Throws<InvalidOperationException>(() => loop.SetEnd(4));

            loop.SetStart(2);
            loop.SetEnd(4);

            Assert.That(loop.Range.StartSeconds, Is.EqualTo(2));
            Assert.That(loop.Range.EndSeconds, Is.EqualTo(4));
            Assert.That(loop.ShouldRestart(3.999), Is.False);
            Assert.That(loop.ShouldRestart(4), Is.True);
        }

        [Test]
        public void Selecting_and_clearing_a_section_is_deterministic()
        {
            var loop = new GameplayPracticeLoop();
            var section = new GameplayPracticeRange(8, 16, "BATTUTE 5–8");

            loop.Select(section);
            Assert.That(loop.IsEnabled, Is.True);
            Assert.That(loop.Range, Is.SameAs(section));

            loop.Clear();
            Assert.That(loop.IsEnabled, Is.False);
            Assert.That(loop.PendingStartSeconds, Is.Null);
        }

        [Test]
        public void Sections_cover_song_once_and_keep_partial_final_group()
        {
            var sections = GameplayPracticeSections.Create(10, 4, 120);

            Assert.That(sections, Has.Count.EqualTo(3));
            Assert.That(sections[0].Label, Is.EqualTo("BATTUTE 1–4"));
            Assert.That(sections[0].StartSeconds, Is.Zero);
            Assert.That(sections[0].EndSeconds, Is.EqualTo(8));
            Assert.That(sections[1].StartSeconds, Is.EqualTo(sections[0].EndSeconds));
            Assert.That(sections[2].Label, Is.EqualTo("BATTUTE 9–10"));
            Assert.That(sections[2].EndSeconds, Is.EqualTo(20));
        }

        [Test]
        public void Sections_use_effective_BPM_without_frame_accumulation()
        {
            var sections = GameplayPracticeSections.Create(4, 3, 90);
            Assert.That(sections[0].DurationSeconds, Is.EqualTo(8).Within(0.000001));
        }
    }
}
