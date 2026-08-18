using System;
using HitTheKit.Core;
using HitTheKit.Unity.Input;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class DrumInputEventTests
    {
        [TestCase(-1)]
        [TestCase(128)]
        public void Rejects_velocity_outside_midi_range(int velocity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrumInputEvent(DrumPad.Kick, velocity, 0));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Rejects_non_finite_song_time(double songTime)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrumInputEvent(DrumPad.Kick, 100, songTime));
        }

        [Test]
        public void Rejects_unsupported_pad_value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrumInputEvent((DrumPad)999, 100, 0));
        }

        [TestCase(KeyCode.F, DrumPad.Kick)]
        [TestCase(KeyCode.J, DrumPad.Snare)]
        [TestCase(KeyCode.K, DrumPad.HiHat)]
        [TestCase(KeyCode.G, DrumPad.Tom1)]
        [TestCase(KeyCode.H, DrumPad.Tom2)]
        [TestCase(KeyCode.L, DrumPad.FloorTom)]
        [TestCase(KeyCode.D, DrumPad.Crash)]
        [TestCase(KeyCode.S, DrumPad.Ride)]
        public void Maps_full_gameplay_keyboard_layout(KeyCode key, DrumPad expected)
        {
            Assert.That(KeyboardDrumInput.TryMapKey(key, out DrumPad actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(KeyboardDrumInput.TryMapKey(key, out DrumPad repeated), Is.True);
            Assert.That(repeated, Is.EqualTo(expected));
        }

        [TestCase(KeyCode.Space)]
        [TestCase(KeyCode.A)]
        [TestCase(KeyCode.KeypadEnter)]
        public void Does_not_map_aliases(KeyCode key)
        {
            Assert.That(KeyboardDrumInput.TryMapKey(key, out _), Is.False);
        }

        [Test]
        public void Preserves_normalized_values()
        {
            var input = new DrumInputEvent(DrumPad.HiHat, 100, -0.25);
            Assert.That(input.Pad, Is.EqualTo(DrumPad.HiHat));
            Assert.That(input.Velocity, Is.EqualTo(100));
            Assert.That(input.SongTimeSeconds, Is.EqualTo(-0.25));
        }

        [Test]
        public void Replacing_song_time_preserves_pad_velocity_and_source()
        {
            var input = new DrumInputEvent(DrumPad.Ride, 87, 1.25, DrumInputSource.Midi);

            DrumInputEvent adjusted = input.WithSongTime(1.20);

            Assert.That(adjusted.Pad, Is.EqualTo(DrumPad.Ride));
            Assert.That(adjusted.Velocity, Is.EqualTo(87));
            Assert.That(adjusted.Source, Is.EqualTo(DrumInputSource.Midi));
            Assert.That(adjusted.SongTimeSeconds, Is.EqualTo(1.20));
        }
    }
}
