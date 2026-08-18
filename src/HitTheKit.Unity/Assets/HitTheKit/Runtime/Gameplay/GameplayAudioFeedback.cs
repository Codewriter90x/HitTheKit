using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Input;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public readonly struct GameplayAudioFeedbackDecision
    {
        public GameplayAudioFeedbackDecision(bool playDrum, bool playMistake)
        {
            PlayDrum = playDrum;
            PlayMistake = playMistake;
        }

        public bool PlayDrum { get; }
        public bool PlayMistake { get; }
    }

    public static class GameplayAudioFeedbackPolicy
    {
        public static GameplayAudioFeedbackDecision ForInput(DrumInputEvent input, HitResult result)
        {
            // A physical electronic kit already renders its own drum voice. Layering the
            // generated practice kit on top causes doubled hits and a white-noise hi-hat
            // (heard as a recurring "tsss"). Keyboard play still needs audible drums.
            bool playSyntheticFeedback = input.Source != DrumInputSource.Midi;
            return new GameplayAudioFeedbackDecision(
                playSyntheticFeedback,
                playSyntheticFeedback && result == null);
        }

        public static bool ShouldPlayMiss(HitResult result) => result != null && result.Grade == HitGrade.Miss;
    }

    public static class GeneratedDrumFeedbackFactory
    {
        public const int DefaultSampleRate = 24000;

        public static IReadOnlyDictionary<DrumPad, AudioClip> CreateKit(int sampleRate = DefaultSampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            var result = new Dictionary<DrumPad, AudioClip>();
            foreach (DrumPad pad in Enum.GetValues(typeof(DrumPad))) result.Add(pad, CreateHit(pad, sampleRate));
            return result;
        }

        public static AudioClip CreateMistake(int sampleRate = DefaultSampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            return CreateMono("HitTheKit - Muted miss", sampleRate, 0.09, (time, noise) =>
                ((float)Math.Sin(2 * Math.PI * 132 * time) * 0.34f +
                 (float)Math.Sin(2 * Math.PI * 264 * time) * 0.11f) *
                (float)Math.Exp(-48 * time));
        }

        private static AudioClip CreateHit(DrumPad pad, int sampleRate)
        {
            switch (pad)
            {
                case DrumPad.Kick:
                    return CreateMono("Kick", sampleRate, 0.22, (time, noise) =>
                        (float)(Math.Sin(2 * Math.PI * (94 - 60 * Math.Min(1, time / 0.22)) * time) * Math.Exp(-17 * time)) * 0.9f);
                case DrumPad.Snare:
                    return CreateMono("Snare", sampleRate, 0.18, (time, noise) =>
                        (noise * 0.72f + (float)Math.Sin(2 * Math.PI * 185 * time) * 0.28f) * (float)Math.Exp(-25 * time) * 0.72f);
                case DrumPad.HiHat:
                    return CreateMono("Hi-hat", sampleRate, 0.085, (time, noise) => noise * (float)Math.Exp(-62 * time) * 0.48f);
                case DrumPad.Crash:
                case DrumPad.Ride:
                    return CreateMono(pad.ToString(), sampleRate, 0.34, (time, noise) =>
                        (noise * 0.62f + (float)Math.Sin(2 * Math.PI * 730 * time) * 0.16f) * (float)Math.Exp(-8 * time) * 0.45f);
                case DrumPad.Tom1:
                case DrumPad.Tom2:
                case DrumPad.FloorTom:
                    double frequency = pad == DrumPad.Tom1 ? 176 : pad == DrumPad.Tom2 ? 142 : 104;
                    return CreateMono(pad.ToString(), sampleRate, 0.24, (time, noise) =>
                        ((float)Math.Sin(2 * Math.PI * frequency * time) * 0.8f + noise * 0.08f) * (float)Math.Exp(-14 * time));
                default:
                    throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        private static AudioClip CreateMono(
            string name,
            int sampleRate,
            double duration,
            Func<double, float, float> sample)
        {
            int frames = Math.Max(1, Mathf.CeilToInt((float)(duration * sampleRate)));
            var samples = new float[frames];
            uint noise = 0xC0FFEEu;
            for (int index = 0; index < frames; index++)
            {
                noise = noise * 1664525u + 1013904223u;
                float random = ((noise >> 8) / 8388607.5f) - 1f;
                samples[index] = Mathf.Clamp(sample(index / (double)sampleRate, random), -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("HitTheKit - " + name, frames, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
