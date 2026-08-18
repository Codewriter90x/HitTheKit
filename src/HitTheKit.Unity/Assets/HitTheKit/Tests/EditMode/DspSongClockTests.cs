using System;
using HitTheKit.Unity.Audio;
using NUnit.Framework;

namespace HitTheKit.Unity.Tests
{
    public sealed class DspSongClockTests
    {
        [Test]
        public void Constructor_rejects_null_time_source()
        {
            Assert.Throws<ArgumentNullException>(() => new DspSongClock(null));
        }

        [Test]
        public void Clock_is_initially_unscheduled()
        {
            var clock = new DspSongClock(new FakeDspTimeSource());

            Assert.That(clock.IsScheduled, Is.False);
            Assert.That(clock.HasStarted, Is.False);
            Assert.That(clock.HasCompleted, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = clock.PositionSeconds);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Schedule_rejects_non_finite_start(double start)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateClock().Schedule(start, 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Schedule_rejects_invalid_duration(double duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateClock().Schedule(1, duration));
        }

        [Test]
        public void Position_follows_dsp_timeline_through_preroll_start_and_playback()
        {
            var time = new FakeDspTimeSource { Now = 9.5 };
            var clock = new DspSongClock(time);
            clock.Schedule(10, 2);

            Assert.That(clock.PositionSeconds, Is.EqualTo(-0.5).Within(0.000001));
            Assert.That(clock.HasStarted, Is.False);
            Assert.That(clock.HasCompleted, Is.False);

            time.Now = 10;
            Assert.That(clock.PositionSeconds, Is.Zero.Within(0.000001));
            Assert.That(clock.HasStarted, Is.True);
            Assert.That(clock.HasCompleted, Is.False);

            time.Now = 10.75;
            Assert.That(clock.PositionSeconds, Is.EqualTo(0.75).Within(0.000001));
        }

        [Test]
        public void Completion_is_inclusive_at_duration_boundary()
        {
            var time = new FakeDspTimeSource { Now = 11.999 };
            var clock = new DspSongClock(time);
            clock.Schedule(10, 2);

            Assert.That(clock.HasCompleted, Is.False);
            time.Now = 12;
            Assert.That(clock.HasCompleted, Is.True);
        }

        [Test]
        public void Second_schedule_requires_reset()
        {
            var clock = CreateClock();
            clock.Schedule(1, 2);

            Assert.Throws<InvalidOperationException>(() => clock.Schedule(3, 4));

            clock.Reset();
            clock.Schedule(3, 4);
            Assert.That(clock.StartDspTime, Is.EqualTo(3));
            Assert.That(clock.DurationSeconds, Is.EqualTo(4));
        }

        [Test]
        public void Position_is_monotonic_when_dsp_time_increases()
        {
            var time = new FakeDspTimeSource { Now = 2 };
            var clock = new DspSongClock(time);
            clock.Schedule(3, 4);
            double first = clock.PositionSeconds;

            time.Now = 3.25;
            double second = clock.PositionSeconds;
            time.Now = 5;
            double third = clock.PositionSeconds;

            Assert.That(second, Is.GreaterThan(first));
            Assert.That(third, Is.GreaterThan(second));
        }

        [Test]
        public void Pause_freezes_position_and_resume_preserves_elapsed_song_time()
        {
            var time = new FakeDspTimeSource { Now = 10.5 };
            var clock = new DspSongClock(time);
            clock.Schedule(10, 4);

            clock.Pause();
            time.Now = 12.5;
            Assert.That(clock.PositionSeconds, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(clock.IsPaused, Is.True);

            clock.Resume();
            Assert.That(clock.PositionSeconds, Is.EqualTo(0.5).Within(0.000001));
            time.Now = 13;
            Assert.That(clock.PositionSeconds, Is.EqualTo(1).Within(0.000001));
        }

        private static DspSongClock CreateClock() => new DspSongClock(new FakeDspTimeSource());

        private sealed class FakeDspTimeSource : IDspTimeSource
        {
            public double Now { get; set; }
        }
    }
}
