using System;
using HitTheKit.Unity.Audio;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class GeneratedClickTrackFactoryTests
    {
        [Test]
        public void Default_clip_has_expected_format_and_duration()
        {
            AudioClip clip = GeneratedClickTrackFactory.Create();
            try
            {
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.frequency, Is.EqualTo(48000));
                Assert.That(clip.length, Is.EqualTo(8).Within(1.0 / 48000));
                Assert.That(clip.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Generated_samples_are_deterministic_non_silent_and_bounded()
        {
            AudioClip first = GeneratedClickTrackFactory.Create(120, 1, 4, 8000);
            AudioClip second = GeneratedClickTrackFactory.Create(120, 1, 4, 8000);
            try
            {
                float[] firstSamples = ReadSamples(first);
                float[] secondSamples = ReadSamples(second);

                Assert.That(firstSamples, Is.EqualTo(secondSamples));
                Assert.That(Array.Exists(firstSamples, value => Math.Abs(value) > 0.001f), Is.True);
                Assert.That(Array.TrueForAll(firstSamples, value => value >= -1 && value <= 1), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void First_beat_is_more_prominent_than_regular_beat()
        {
            const int sampleRate = 8000;
            AudioClip clip = GeneratedClickTrackFactory.Create(120, 1, 4, sampleRate);
            try
            {
                float[] samples = ReadSamples(clip);
                float accentPeak = Peak(samples, 0, 200);
                float regularPeak = Peak(samples, sampleRate / 2, 200);

                Assert.That(accentPeak, Is.GreaterThan(regularPeak));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [TestCase(0, 1, 4, 8000)]
        [TestCase(double.NaN, 1, 4, 8000)]
        [TestCase(120, 0, 4, 8000)]
        [TestCase(120, 1, 0, 8000)]
        [TestCase(120, 1, 4, 0)]
        public void Invalid_parameters_are_rejected(double bpm, int bars, int beatsPerBar, int sampleRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GeneratedClickTrackFactory.Create(bpm, bars, beatsPerBar, sampleRate));
        }

        private static float[] ReadSamples(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True);
            return samples;
        }

        private static float Peak(float[] samples, int start, int count)
        {
            float peak = 0;
            for (int index = start; index < start + count; index++)
            {
                peak = Math.Max(peak, Math.Abs(samples[index]));
            }

            return peak;
        }
    }
}
