using System.Collections;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayHighwayPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplaySettingsRuntime.UseForTests(
                new GameplaySettingsService(new InMemoryGameplaySettingsPersistence()));
            PlayerPreferencesRuntime.UseForTests(
                new PlayerPreferencesService(new InMemoryGameplaySettingsPersistence()));
            GameplaySessionContext.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameplaySessionContext.Reset();
            GameplaySettingsRuntime.ResetForTests();
            PlayerPreferencesRuntime.ResetForTests();
        }

        [UnityTest]
        public IEnumerator Gameplay_scene_uses_the_session_theme_and_does_not_expose_an_in_game_switcher()
        {
            GameplaySessionContext.SelectFreePlay(GameplayPresentationTheme.ConcertStage);
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            GameplayHighwayController controller =
                Object.FindAnyObjectByType<GameplayHighwayController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsViewBound, Is.True);
            Assert.That(controller.TargetCount, Is.EqualTo(8));
            Assert.That(controller.Surface, Is.Not.Null);
            Assert.That(controller.KitSurface, Is.Not.Null);
            Assert.That(controller.KitSurface.PieceCount, Is.EqualTo(8));
            Assert.That(controller.IsInstructionalKitVisible, Is.False);
            Assert.That(controller.KitSurface.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            UIDocument document = controller.GetComponent<UIDocument>();
            VisualElement app = document.rootVisualElement.Q<VisualElement>("gameplay-app");
            Assert.That(app, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("target-kick"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("gameplay-kit-surface"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Label>("kit-guidance-label"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("results-overlay"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("pause-button"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("countdown-overlay"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<ChartWaveformView>("chart-waveform"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("chart-waveform-preview"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<TextField>("chart-note-velocity"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<DropdownField>("chart-note-articulation"), Is.Not.Null);
            Assert.That(controller.CountdownBeat, Is.InRange(1, 4));
            Assert.That(controller.Theme, Is.EqualTo(GameplayPresentationTheme.ConcertStage));
            Assert.That(app.ClassListContains("theme--concert-stage"), Is.True);
            Assert.That(controller.EnvironmentTitle, Is.EqualTo("CONCERT STAGE"));
            Assert.That(document.rootVisualElement.Q<Label>("environment-title").text, Is.EqualTo("CONCERT STAGE"));
            Assert.That(controller.ActiveBackground, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("theme-arcade"), Is.Null);
            Assert.That(document.rootVisualElement.Q<Button>("theme-concert"), Is.Null);
            Assert.That(document.rootVisualElement.Q<Button>("theme-precision"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Precision_grid_enables_the_colored_instructional_kit_from_settings()
        {
            GameplaySessionContext.SelectFreePlay(GameplayPresentationTheme.PrecisionGrid);
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            GameplayHighwayController controller =
                Object.FindAnyObjectByType<GameplayHighwayController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(controller.IsInstructionalKitVisible, Is.True);
            Assert.That(controller.KitSurface, Is.Not.Null);
            Assert.That(controller.KitSurface.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            UIDocument document = controller.GetComponent<UIDocument>();
            Label guidance = document.rootVisualElement.Q<Label>("kit-guidance-label");
            Assert.That(guidance, Is.Not.Null);
            Assert.That(guidance.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator Pause_and_restart_keep_audio_clock_and_run_state_coherent()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            GameplayHighwayController controller = Object.FindAnyObjectByType<GameplayHighwayController>();
            Assert.That(controller, Is.Not.Null);

            controller.TogglePause();
            Assert.That(controller.RunState, Is.EqualTo(GameplayRunState.Paused));
            Assert.That(Object.FindAnyObjectByType<HitTheKit.Unity.Audio.DspSongClockPrototype>().Clock.IsPaused, Is.True);

            controller.TogglePause();
            Assert.That(controller.RunState, Is.Not.EqualTo(GameplayRunState.Paused));
            controller.RestartRun();
            Assert.That(controller.RunState, Is.EqualTo(GameplayRunState.Countdown));
            Assert.That(controller.ScoreSnapshot.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Highway_receives_canonical_upcoming_notes_without_frame_clock_logic()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;

            GameplayHighwayController controller =
                Object.FindAnyObjectByType<GameplayHighwayController>();
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((controller == null || controller.Surface == null || controller.Surface.Notes.Count == 0) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (controller == null) controller = Object.FindAnyObjectByType<GameplayHighwayController>();
            }

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Surface.Notes, Is.Not.Empty);
            Assert.That(new[]
            {
                DrumPad.Kick, DrumPad.HiHat, DrumPad.Snare, DrumPad.Tom1,
                DrumPad.Tom2, DrumPad.FloorTom, DrumPad.Crash, DrumPad.Ride
            }, Does.Contain(controller.Surface.Notes[0].Note.Pad));

            TimelineNote first = controller.Surface.Notes[0];
            controller.KitSurface.SetFrame(
                controller.Surface.Notes,
                first.EffectiveTimeSeconds - 0.5,
                1.35,
                null,
                0);
            Assert.That(
                controller.KitSurface.TryGetTargetState(first.Note.Pad, out GameplayKitTargetState target),
                Is.True);
            Assert.That(target.IsUpcoming, Is.True);
            Assert.That(target.Intensity, Is.GreaterThan(0));
        }
    }
}
