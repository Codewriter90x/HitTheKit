using System;
using UnityEngine;

namespace HitTheKit.Unity.Audio
{
    public static class GeneratedClickTrackFactory
    {
        public const double DefaultBpm = 120;
        public const int DefaultBars = 4;
        public const int DefaultBeatsPerBar = 4;
        public const int DefaultSampleRate = 48000;

        public static AudioClip Create(
            double bpm = DefaultBpm,
            int bars = DefaultBars,
            int beatsPerBar = DefaultBeatsPerBar,
            int sampleRate = DefaultSampleRate)
        {
            ValidateParameters(bpm, bars, beatsPerBar, sampleRate);

            int beatCount = checked(bars * beatsPerBar);
            double durationSeconds = beatCount * 60.0 / bpm;
            int sampleCount = checked((int)Math.Ceiling(durationSeconds * sampleRate));
            var samples = new float[sampleCount];
            int clickSamples = Math.Max(1, (int)(sampleRate * 0.025));

            for (int beat = 0; beat < beatCount; beat++)
            {
                int startSample = (int)Math.Round(beat * 60.0 / bpm * sampleRate);
                bool isAccent = beat % beatsPerBar == 0;
                double frequency = isAccent ? 1600.0 : 1100.0;
                double amplitude = isAccent ? 0.55 : 0.32;

                for (int index = 0; index < clickSamples && startSample + index < samples.Length; index++)
                {
                    double age = index / (double)sampleRate;
                    double envelope = Math.Exp(-age * 150.0);
                    double wave = Math.Sin(2.0 * Math.PI * frequency * age);
                    samples[startSample + index] = (float)(amplitude * envelope * wave);
                }
            }

            AudioClip clip = AudioClip.Create(
                "Generated HitTheKit Click Track",
                sampleCount,
                channels: 1,
                frequency: sampleRate,
                stream: false);
            if (!clip.SetData(samples, 0))
            {
                UnityEngine.Object.DestroyImmediate(clip);
                throw new InvalidOperationException("Unity could not populate the generated click track.");
            }

            return clip;
        }

        private static void ValidateParameters(double bpm, int bars, int beatsPerBar, int sampleRate)
        {
            if (double.IsNaN(bpm) || double.IsInfinity(bpm) || bpm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be finite and positive.");
            }

            if (bars <= 0) throw new ArgumentOutOfRangeException(nameof(bars));
            if (beatsPerBar <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }
    }
}
