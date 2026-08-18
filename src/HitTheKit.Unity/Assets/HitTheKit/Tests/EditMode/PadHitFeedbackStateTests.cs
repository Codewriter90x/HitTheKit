using System;
using HitTheKit.Unity.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class PadHitFeedbackStateTests
    {
        [Test]
        public void Feedback_is_active_until_duration_elapses()
        {
            var feedback = new PadHitFeedbackState();
            feedback.Begin(Color.green, 0.12);

            feedback.Advance(0.05);
            Assert.That(feedback.IsActive, Is.True);
            Assert.That(feedback.Color, Is.EqualTo(Color.green));
            feedback.Advance(0.07);
            Assert.That(feedback.IsActive, Is.False);
            Assert.That(feedback.RemainingSeconds, Is.Zero);
        }

        [Test]
        public void Consecutive_feedback_replaces_color_and_restarts_duration()
        {
            var feedback = new PadHitFeedbackState();
            feedback.Begin(Color.green, 0.1);
            feedback.Advance(0.08);
            feedback.Begin(Color.magenta, 0.15);

            Assert.That(feedback.Color, Is.EqualTo(Color.magenta));
            Assert.That(feedback.RemainingSeconds, Is.EqualTo(0.15).Within(0.000001));
        }

        [TestCase(0)]
        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        public void Rejects_invalid_duration(double duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PadHitFeedbackState().Begin(Color.white, duration));
        }

        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        public void Rejects_invalid_elapsed_time(double elapsed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PadHitFeedbackState().Advance(elapsed));
        }
    }
}
