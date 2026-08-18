using System;
using HitTheKit.Core;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class PlayerPreferencesTests
    {
        [Test]
        public void Persists_timing_audio_accessibility_display_and_language_preferences()
        {
            var persistence = new InMemoryGameplaySettingsPersistence();
            var preferences = new PlayerPreferencesService(persistence);

            preferences.SetInputOffset(DrumInputSource.Keyboard, 0.035);
            preferences.SetInputOffset(DrumInputSource.Midi, -0.012);
            preferences.SetAudioMuted(true);
            preferences.SetMasterVolume(0.6f);
            preferences.SetReducedMotion(true);
            preferences.SetHighContrast(true);
            preferences.SetMetronomeEnabled(true);
            preferences.SetLanguage(PlayerLanguage.English);
            preferences.SetDisplay(false, 1280, 720);
            preferences.SetFirstRunCompleted(true);
            preferences.SelectMidiDevice("coremidi.42");

            PlayerPreferencesSnapshot restored = new PlayerPreferencesService(persistence).Snapshot;
            Assert.That(restored.KeyboardOffsetSeconds, Is.EqualTo(0.035).Within(0.000001));
            Assert.That(restored.MidiOffsetSeconds, Is.EqualTo(-0.012).Within(0.000001));
            Assert.That(restored.AudioMuted, Is.True);
            Assert.That(restored.MasterVolume, Is.EqualTo(0.6f));
            Assert.That(restored.ReducedMotion, Is.True);
            Assert.That(restored.HighContrast, Is.True);
            Assert.That(restored.MetronomeEnabled, Is.True);
            Assert.That(restored.Language, Is.EqualTo(PlayerLanguage.English));
            Assert.That(restored.Fullscreen, Is.False);
            Assert.That(restored.WindowWidth, Is.EqualTo(1280));
            Assert.That(restored.WindowHeight, Is.EqualTo(720));
            Assert.That(restored.FirstRunCompleted, Is.True);
            Assert.That(restored.SelectedMidiDeviceId, Is.EqualTo("coremidi.42"));
        }

        [Test]
        public void Keyboard_bindings_are_persisted_and_must_be_unique()
        {
            var persistence = new InMemoryGameplaySettingsPersistence();
            var preferences = new PlayerPreferencesService(persistence);

            preferences.SetKeyBinding(DrumPad.Kick, KeyCode.Space);

            Assert.That(new PlayerPreferencesService(persistence).Snapshot.KickKey, Is.EqualTo(KeyCode.Space));
            Assert.Throws<InvalidOperationException>(() => preferences.SetKeyBinding(DrumPad.Snare, KeyCode.Space));
        }

        [Test]
        public void Invalid_saved_preferences_fall_back_to_safe_defaults()
        {
            var persistence = new InMemoryGameplaySettingsPersistence();
            persistence.Save("{\"schemaVersion\":1,\"masterVolume\":9}");

            var preferences = new PlayerPreferencesService(persistence);

            Assert.That(preferences.Snapshot.MasterVolume, Is.EqualTo(1f));
            Assert.That(preferences.Snapshot.KickKey, Is.EqualTo(KeyCode.F));
            Assert.That(preferences.LastError, Is.Not.Empty);
        }

        [Test]
        public void Monotonic_mapper_preserves_event_age_instead_of_using_poll_time()
        {
            var mapper = new MonotonicMidiTimestampMapper();
            Assert.That(mapper.Map(10.000, 10.000, 0.000), Is.EqualTo(0).Within(0.000001));

            double mapped = mapper.Map(10.020, 10.050, 0.050);

            Assert.That(mapped, Is.EqualTo(0.020).Within(0.000001));
        }

        [Test]
        public void Monotonic_mapper_rejects_non_finite_clock_values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MonotonicMidiTimestampMapper().Map(double.NaN, 1, 1));
        }
    }
}
