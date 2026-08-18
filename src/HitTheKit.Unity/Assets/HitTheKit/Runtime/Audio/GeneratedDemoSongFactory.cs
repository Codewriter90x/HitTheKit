using System;
using UnityEngine;

namespace HitTheKit.Unity.Audio
{
    public static class GeneratedDemoSongFactory
    {
        public static AudioClip Create(double bpm, int bars, int beatsPerBar, int sampleRate)
        {
            if (bpm <= 0 || bars <= 0 || beatsPerBar <= 0 || sampleRate <= 0)
                throw new ArgumentOutOfRangeException("Demo song parameters must be positive.");

            double beatSeconds = 60.0 / bpm;
            double duration = bars * beatsPerBar * beatSeconds;
            int frames = Mathf.CeilToInt((float)(duration * sampleRate));
            var samples = new float[frames * 2];

            for (int bar = 0; bar < bars; bar++)
            {
                for (int beat = 0; beat < beatsPerBar; beat++)
                {
                    double time = (bar * beatsPerBar + beat) * beatSeconds;
                    double root = 55f * (bar % 4 == 3 ? 1.5f : 1f);
                    AddBass(samples, sampleRate, time, beatSeconds, (float)root);
                    AddPad(samples, sampleRate, time, beatSeconds * 1.8, root * 2, 0.055f);
                    if (beat % 2 == 0)
                        AddMelody(samples, sampleRate, time + beatSeconds * 0.25, beatSeconds * 0.62, root * (beat == 0 ? 4 : 5), 0.085f);
                }
            }

            AudioClip clip = AudioClip.Create("HitTheKit - Neon Circuit", frames, 2, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void AddPad(float[] output, int sampleRate, double start, double length, double frequency, float gain)
        {
            AddVoice(output, sampleRate, start, Math.Min(length, 0.9), (t, random) =>
                (float)((Math.Sin(2 * Math.PI * frequency * t) +
                         Math.Sin(2 * Math.PI * frequency * 1.5 * t) * 0.38) *
                        SmoothEnvelope(t, Math.Min(length, 0.9)) * gain));
        }

        private static void AddMelody(float[] output, int sampleRate, double start, double length, double frequency, float gain)
        {
            AddVoice(output, sampleRate, start, length, (t, random) =>
                (float)((Math.Sin(2 * Math.PI * frequency * t) * 0.8 +
                         Math.Sin(2 * Math.PI * frequency * 2 * t) * 0.2) *
                        SmoothEnvelope(t, length) * gain));
        }

        private static void AddBass(float[] output, int sampleRate, double start, double length, float frequency)
        {
            AddVoice(output, sampleRate, start, Math.Min(length * 0.88, 0.42), (t, random) =>
                (float)(Math.Sin(2 * Math.PI * frequency * t) * Math.Exp(-3.8 * t) * 0.16));
        }

        private static double SmoothEnvelope(double time, double duration)
        {
            double attack = Math.Min(1, time / 0.045);
            double release = Math.Min(1, Math.Max(0, duration - time) / 0.16);
            return attack * release;
        }

        private static void AddVoice(
            float[] output,
            int sampleRate,
            double start,
            double duration,
            Func<double, float, float> sample)
        {
            int first = Math.Max(0, (int)(start * sampleRate));
            int count = Math.Min((int)(duration * sampleRate), output.Length / 2 - first);
            uint noise = (uint)(first + 1);
            for (int index = 0; index < count; index++)
            {
                noise = noise * 1664525u + 1013904223u;
                float random = ((noise >> 8) / 8388607.5f) - 1f;
                float value = sample(index / (double)sampleRate, random);
                int frame = (first + index) * 2;
                output[frame] = Mathf.Clamp(output[frame] + value, -0.95f, 0.95f);
                output[frame + 1] = Mathf.Clamp(output[frame + 1] + value, -0.95f, 0.95f);
            }
        }
    }
}
