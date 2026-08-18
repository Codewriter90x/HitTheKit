using System.Collections;
using System.IO;
using System.Reflection;
using HitTheKit.Unity.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HitTheKit.Unity.Tests
{
    public sealed class DspAudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator Dsp_time_is_finite_monotonic_and_advances()
        {
            double first = AudioSettings.dspTime;
            Assert.That(double.IsNaN(first) || double.IsInfinity(first), Is.False);

            float timeout = Time.realtimeSinceStartup + 5;
            double current = first;
            while (current <= first && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                double next = AudioSettings.dspTime;
                Assert.That(next, Is.GreaterThanOrEqualTo(current));
                current = next;
            }

            Assert.That(current, Is.GreaterThan(first));
        }

        [UnityTest]
        public IEnumerator Scheduled_audio_and_clock_share_the_dsp_timeline()
        {
            var gameObject = new GameObject("DSP scheduling test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            AudioClip clip = GeneratedClickTrackFactory.Create(600, 1, 1, 8000);
            try
            {
                var timeSource = new UnityDspTimeSource();
                var clock = new DspSongClock(timeSource);
                double start = timeSource.Now + 0.2;
                source.clip = clip;
                source.PlayScheduled(start);
                clock.Schedule(start, clip.length);

                Assert.That(clock.PositionSeconds, Is.LessThan(0));
                double previous = clock.PositionSeconds;
                float timeout = Time.realtimeSinceStartup + 5;
                while (!clock.HasStarted && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                    Assert.That(clock.PositionSeconds, Is.GreaterThanOrEqualTo(previous));
                    previous = clock.PositionSeconds;
                }

                Assert.That(clock.HasStarted, Is.True);
                Assert.That(clock.PositionSeconds, Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                Object.Destroy(gameObject);
                Object.Destroy(clip);
            }
        }

        [UnityTest]
        public IEnumerator Prototype_configures_source_and_schedules_generated_clip()
        {
            var gameObject = new GameObject("Prototype test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var prototype = gameObject.AddComponent<DspSongClockPrototype>();
            ConfigureShortFixture(prototype);
            double beforeStart = AudioSettings.dspTime;

            yield return null;

            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.loop, Is.False);
            Assert.That(source.spatialBlend, Is.Zero);
            Assert.That(prototype.GeneratedClip, Is.Not.Null);
            Assert.That(prototype.Clock.IsScheduled, Is.True);
            Assert.That(prototype.StartDspTime, Is.GreaterThan(beforeStart));

            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prototype_loads_a_detected_local_wave_file_before_scheduling()
        {
            string path = Path.Combine(Path.GetTempPath(), "hitthekit-song-" + System.Guid.NewGuid().ToString("N") + ".wav");
            WriteSilentWave(path, 8000, 800);
            var gameObject = new GameObject("External song test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var prototype = gameObject.AddComponent<DspSongClockPrototype>();
            prototype.Configure(600, 1, 1, 1, false, path);

            try
            {
                float deadline = Time.realtimeSinceStartup + 5;
                while (prototype.GeneratedClip == null && string.IsNullOrEmpty(prototype.LoadError) &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(prototype.LoadError, Is.Null.Or.Empty);
                Assert.That(prototype.ExternalAudioPath, Is.EqualTo(path));
                Assert.That(prototype.GeneratedClip, Is.Not.Null);
                Assert.That(prototype.Clock, Is.Not.Null);
                Assert.That(prototype.Clock.IsScheduled, Is.True);
                Assert.That(source.clip, Is.SameAs(prototype.GeneratedClip));
            }
            finally
            {
                Object.Destroy(gameObject);
                if (File.Exists(path)) File.Delete(path);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator External_audio_speed_changes_source_pitch_and_clock_duration_together()
        {
            string path = Path.Combine(Path.GetTempPath(), "hitthekit-slow-song-" + System.Guid.NewGuid().ToString("N") + ".wav");
            WriteSilentWave(path, 8000, 8000);
            var gameObject = new GameObject("Slow external song test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var prototype = gameObject.AddComponent<DspSongClockPrototype>();
            prototype.Configure(60, 1, 1, 4, false, path, 0.5);

            try
            {
                float deadline = Time.realtimeSinceStartup + 5;
                while (prototype.GeneratedClip == null && Time.realtimeSinceStartup < deadline) yield return null;

                Assert.That(prototype.GeneratedClip, Is.Not.Null);
                Assert.That(prototype.AudioPlaybackSpeed, Is.EqualTo(0.5));
                Assert.That(source.pitch, Is.EqualTo(0.5f));
                Assert.That(prototype.Clock.DurationSeconds,
                    Is.EqualTo(prototype.GeneratedClip.length / 0.5).Within(0.001));
            }
            finally
            {
                Object.Destroy(gameObject);
                if (File.Exists(path)) File.Delete(path);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Destroying_controller_stops_source_and_releases_generated_clip()
        {
            const double safetyMarginSeconds = 0.15;
            const float timeoutSeconds = 3;
            var gameObject = new GameObject("Prototype cleanup test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var prototype = gameObject.AddComponent<DspSongClockPrototype>();
            SetField(prototype, "leadInSeconds", 0.5d);
            SetField(prototype, "bpm", 120d);
            SetField(prototype, "bars", 1);
            SetField(prototype, "beatsPerBar", 1);
            SetField(prototype, "sampleRate", 8000);
            SetField(prototype, "logLifecycle", false);

            try
            {
                yield return null;
                double originalStartDspTime = prototype.StartDspTime;
                AudioClip generated = prototype.GeneratedClip;

                Assert.That(prototype.Clock.IsScheduled, Is.True);
                Assert.That(originalStartDspTime, Is.GreaterThan(AudioSettings.dspTime));
                Assert.That(source.clip, Is.SameAs(generated));
                Assert.That(source.isPlaying, Is.True);

                Object.Destroy(prototype);
                yield return null;

                Assert.That(source, Is.Not.Null);
                Assert.That(source.clip, Is.Null);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(generated == null, Is.True);

                float timeout = Time.realtimeSinceStartup + timeoutSeconds;
                while (AudioSettings.dspTime <= originalStartDspTime + safetyMarginSeconds &&
                       Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(
                    AudioSettings.dspTime,
                    Is.GreaterThan(originalStartDspTime + safetyMarginSeconds));
                Assert.That(source, Is.Not.Null);
                Assert.That(source.clip, Is.Null);
                Assert.That(source.isPlaying, Is.False);
            }
            finally
            {
                Object.Destroy(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator GameplayPrototype_scene_runs_clock_to_completion()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            DspSongClockPrototype prototype = Object.FindFirstObjectByType<DspSongClockPrototype>();
            Assert.That(prototype, Is.Not.Null);
            prototype.GetComponent<AudioSource>().mute = true;

            Assert.That(prototype.GeneratedClip, Is.Not.Null);
            Assert.That(prototype.Clock.IsScheduled, Is.True);

            float timeout = Time.realtimeSinceStartup + 20;
            while (!prototype.HasCompleted && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(prototype.HasCompleted, Is.True);
        }

        private static void ConfigureShortFixture(DspSongClockPrototype prototype)
        {
            SetField(prototype, "leadInSeconds", 0.1d);
            SetField(prototype, "bpm", 600d);
            SetField(prototype, "bars", 1);
            SetField(prototype, "beatsPerBar", 1);
            SetField(prototype, "sampleRate", 8000);
            SetField(prototype, "logLifecycle", false);
        }

        private static void SetField<T>(DspSongClockPrototype prototype, string name, T value)
        {
            typeof(DspSongClockPrototype)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(prototype, value);
        }

        private static void WriteSilentWave(string path, int sampleRate, int sampleCount)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            int dataBytes = sampleCount * channels * (bitsPerSample / 8);
            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8));
                writer.Write((short)(channels * (bitsPerSample / 8)));
                writer.Write(bitsPerSample);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(dataBytes);
                writer.Write(new byte[dataBytes]);
            }
        }
    }
}
