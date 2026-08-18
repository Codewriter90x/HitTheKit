using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using HitTheKit.Unity.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HitTheKit.Unity.Tests
{
    public sealed class KeyboardHitMatchingPlayModeTests
    {
        [SetUp]
        public void SetUp() => PlayerPreferencesRuntime.UseForTests(
            new PlayerPreferencesService(new InMemoryGameplaySettingsPersistence()));

        [TearDown]
        public void TearDown() => PlayerPreferencesRuntime.ResetForTests();

        [UnityTest]
        public IEnumerator Synthetic_input_produces_perfect_snapshot_and_visual_feedback()
        {
            RuntimeFixture fixture = CreateFixture(Notes((0.2, "kick")));
            try
            {
                yield return null;
                Material sharedBefore = fixture.Kick.TargetRenderer.sharedMaterial;

                fixture.Input.Emit(new DrumInputEvent(DrumPad.Kick, 100, 0.2));

                Assert.That(fixture.Matching.Snapshot.PerfectCount, Is.EqualTo(1));
                Assert.That(fixture.Matching.Snapshot.ResolvedNoteCount, Is.EqualTo(1));
                Assert.That(fixture.Kick.IsHitFeedbackActive, Is.True);
                Assert.That(fixture.Snare.IsHitFeedbackActive, Is.False);
                Assert.That(fixture.Kick.TargetRenderer.sharedMaterial, Is.SameAs(sharedBefore));

                yield return WaitUntil(
                    () => !fixture.Kick.IsHitFeedbackActive,
                    2,
                    "Perfect feedback did not return to the timeline state.");
                Assert.That(fixture.Kick.CurrentState.Intensity, Is.Zero);
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Wrong_pad_is_no_match_and_later_correct_pad_resolves_note()
        {
            RuntimeFixture fixture = CreateFixture(Notes((0.2, "kick")));
            try
            {
                yield return null;
                fixture.Input.Emit(new DrumInputEvent(DrumPad.Snare, 100, 0.2));

                Assert.That(fixture.Matching.Snapshot.NoMatchCount, Is.EqualTo(1));
                Assert.That(fixture.Matching.Snapshot.ResolvedNoteCount, Is.Zero);
                Assert.That(fixture.Snare.IsHitFeedbackActive, Is.True);
                Assert.That(fixture.Kick.IsHitFeedbackActive, Is.False);

                fixture.Input.Emit(new DrumInputEvent(DrumPad.Kick, 100, 0.2));
                Assert.That(fixture.Matching.Snapshot.PerfectCount, Is.EqualTo(1));
                Assert.That(fixture.Matching.Snapshot.ResolvedNoteCount, Is.EqualTo(1));
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Saved_keyboard_offset_is_applied_once_before_matching()
        {
            PlayerPreferencesRuntime.Current.SetInputOffset(DrumInputSource.Keyboard, 0.040);
            RuntimeFixture fixture = CreateFixture(Notes((0.2, "kick")));
            try
            {
                yield return null;

                fixture.Input.Emit(new DrumInputEvent(DrumPad.Kick, 100, 0.240, DrumInputSource.Keyboard));

                Assert.That(fixture.Matching.Snapshot.PerfectCount, Is.EqualTo(1));
                Assert.That(fixture.Matching.Snapshot.LastInput.Value.SongTimeSeconds, Is.EqualTo(0.200).Within(0.000001));
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Automatic_miss_resolves_once_without_hit_feedback()
        {
            RuntimeFixture fixture = CreateFixture(Notes((0.05, "kick")));
            try
            {
                yield return null;
                yield return WaitUntil(
                    () => fixture.Matching.Snapshot != null && fixture.Matching.Snapshot.MissCount == 1,
                    3,
                    "Expired note was not marked Miss.");

                Assert.That(fixture.Matching.Snapshot.ResolvedNoteCount, Is.EqualTo(1));
                Assert.That(fixture.Kick.IsHitFeedbackActive, Is.False);
                yield return null;
                Assert.That(fixture.Matching.Snapshot.MissCount, Is.EqualTo(1));
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Feedback_presenter_maps_all_input_outcomes_to_the_pressed_pad()
        {
            RuntimeFixture fixture = CreateFixture(Notes(
                (1.0, "kick"),
                (2.0, "kick"),
                (3.0, "kick"),
                (4.0, "kick")));
            try
            {
                yield return null;
                AssertFeedback(fixture, DrumPad.Kick, 1.0, new Color(0.45f, 1f, 0.55f, 1f));
                AssertFeedback(fixture, DrumPad.Kick, 2.06, new Color(0.35f, 0.8f, 1f, 1f));
                AssertFeedback(fixture, DrumPad.Kick, 2.88, new Color(1f, 0.5f, 0.12f, 1f));
                AssertFeedback(fixture, DrumPad.Kick, 4.12, new Color(0.7f, 0.35f, 1f, 1f));
                AssertFeedback(fixture, DrumPad.Snare, 6.0, new Color(0.55f, 0.12f, 0.12f, 1f));
                Assert.That(fixture.HiHat.IsHitFeedbackActive, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayPrototype_contains_connected_keyboard_matching_and_feedback_components()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;

            KeyboardDrumInput[] inputs = UnityEngine.Object.FindObjectsByType<KeyboardDrumInput>();
            CompositeDrumInput[] compositeInputs = UnityEngine.Object.FindObjectsByType<CompositeDrumInput>();
            HitMatchingPrototype[] matching = UnityEngine.Object.FindObjectsByType<HitMatchingPrototype>();
            HitResultVisualPresenter[] feedback = UnityEngine.Object.FindObjectsByType<HitResultVisualPresenter>();

            Assert.That(inputs, Has.Length.EqualTo(1));
            Assert.That(compositeInputs, Has.Length.EqualTo(1));
            Assert.That(matching, Has.Length.EqualTo(1));
            Assert.That(feedback, Has.Length.EqualTo(1));
            Assert.That(matching[0].DrumInput, Is.SameAs(compositeInputs[0]));
            Assert.That(compositeInputs[0].KeyboardSource, Is.SameAs(inputs[0]));
            Assert.That(matching[0].ChartTimeline, Is.Not.Null);
            Assert.That(matching[0].SongClock, Is.SameAs(inputs[0].SongClock));
            Assert.That(matching[0].Session, Is.Not.Null);
            Assert.That(feedback[0].Matching, Is.SameAs(matching[0]));
            Assert.That(feedback[0].KickVisual, Is.Not.Null);
            Assert.That(feedback[0].SnareVisual, Is.Not.Null);
            Assert.That(feedback[0].HiHatVisual, Is.Not.Null);
            Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>(), Is.Empty);
            Component[] components = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .ToArray();
            Assert.That(components, Has.None.Null);
            Assert.That(components.Any(component =>
                component.GetType().FullName == "UnityEngine.EventSystems.EventSystem"), Is.False);
        }

        private static void AssertFeedback(
            RuntimeFixture fixture,
            DrumPad pad,
            double songTime,
            Color expected)
        {
            fixture.Input.Emit(new DrumInputEvent(pad, 100, songTime));
            DrumPadVisual visual = pad == DrumPad.Kick
                ? fixture.Kick
                : pad == DrumPad.Snare ? fixture.Snare : fixture.HiHat;
            var block = new MaterialPropertyBlock();
            visual.TargetRenderer.GetPropertyBlock(block);
            Color actual = block.GetColor(Shader.PropertyToID("_BaseColor"));
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001));
        }

        private static RuntimeFixture CreateFixture(string noteJson)
        {
            string json = $"{{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{{\"easy\":[{noteJson}]}}}}";
            var chartAsset = new TextAsset(json);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var root = new GameObject("Keyboard matching integration test");
            root.AddComponent<AudioListener>();
            var source = root.AddComponent<AudioSource>();
            source.mute = true;
            var clock = root.AddComponent<DspSongClockPrototype>();
            ConfigureClock(clock);
            var timeline = root.AddComponent<ChartTimelinePrototype>();
            ConfigureTimeline(timeline, chartAsset, clock);
            var input = root.AddComponent<ControlledDrumInput>();
            var matching = root.AddComponent<HitMatchingPrototype>();
            SetField(matching, "drumInputSource", input);
            SetField(matching, "chartTimeline", timeline);
            SetField(matching, "songClock", clock);

            DrumPadVisual kick = CreateVisual(root.transform, "Kick", DrumPad.Kick, material);
            DrumPadVisual snare = CreateVisual(root.transform, "Snare", DrumPad.Snare, material);
            DrumPadVisual hiHat = CreateVisual(root.transform, "HiHat", DrumPad.HiHat, material);
            var presenter = root.AddComponent<HitResultVisualPresenter>();
            SetField(presenter, "matching", matching);
            SetField(presenter, "kickVisual", kick);
            SetField(presenter, "snareVisual", snare);
            SetField(presenter, "hiHatVisual", hiHat);
            SetField(presenter, "feedbackDurationSeconds", 0.08d);

            return new RuntimeFixture(root, chartAsset, material, input, matching, kick, snare, hiHat);
        }

        private static DrumPadVisual CreateVisual(
            Transform parent,
            string name,
            DrumPad pad,
            Material material)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            var visual = gameObject.AddComponent<DrumPadVisual>();
            SetField(visual, "pad", pad);
            SetField(visual, "targetRenderer", renderer);
            return visual;
        }

        private static void ConfigureClock(DspSongClockPrototype clock)
        {
            SetField(clock, "leadInSeconds", 0.15d);
            SetField(clock, "bpm", 600d);
            SetField(clock, "bars", 1);
            SetField(clock, "beatsPerBar", 4);
            SetField(clock, "sampleRate", 8000);
            SetField(clock, "logLifecycle", false);
        }

        private static void ConfigureTimeline(
            ChartTimelinePrototype timeline,
            TextAsset chartAsset,
            DspSongClockPrototype clock)
        {
            SetField(timeline, "chartAsset", chartAsset);
            SetField(timeline, "difficulty", "easy");
            SetField(timeline, "songClock", clock);
            SetField(timeline, "lookAheadSeconds", 0.25d);
            SetField(timeline, "logLifecycle", false);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, failureMessage);
        }

        private static string Notes(params (double time, string pad)[] notes)
        {
            return string.Join(",", notes.Select(note =>
                $"{{\"time\":{note.time.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"\"pad\":\"{note.pad}\"}}"));
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class RuntimeFixture : IDisposable
        {
            private readonly GameObject root;
            private readonly TextAsset chartAsset;
            private readonly Material material;

            public RuntimeFixture(
                GameObject root,
                TextAsset chartAsset,
                Material material,
                ControlledDrumInput input,
                HitMatchingPrototype matching,
                DrumPadVisual kick,
                DrumPadVisual snare,
                DrumPadVisual hiHat)
            {
                this.root = root;
                this.chartAsset = chartAsset;
                this.material = material;
                Input = input;
                Matching = matching;
                Kick = kick;
                Snare = snare;
                HiHat = hiHat;
            }

            public ControlledDrumInput Input { get; }
            public HitMatchingPrototype Matching { get; }
            public DrumPadVisual Kick { get; }
            public DrumPadVisual Snare { get; }
            public DrumPadVisual HiHat { get; }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(chartAsset);
                UnityEngine.Object.Destroy(material);
            }
        }

        public sealed class ControlledDrumInput : MonoBehaviour, IDrumInput
        {
            public event Action<DrumInputEvent> HitReceived;
            public void Emit(DrumInputEvent input) => HitReceived?.Invoke(input);
        }
    }
}
