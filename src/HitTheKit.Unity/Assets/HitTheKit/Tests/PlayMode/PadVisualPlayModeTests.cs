using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Matching;
using HitTheKit.Unity.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HitTheKit.Unity.Tests
{
    public sealed class PadVisualPlayModeTests
    {
        [SetUp]
        public void SetUp() => PlayerPreferencesRuntime.UseForTests(
            new PlayerPreferencesService(new InMemoryGameplaySettingsPersistence()));

        [TearDown]
        public void TearDown() => PlayerPreferencesRuntime.ResetForTests();

        [UnityTest]
        public IEnumerator Presenter_illuminates_only_chart_driven_pads_and_resets_elapsed_note()
        {
            RuntimeFixture fixture = CreateFixture(
                Notes((0.30, "kick"), (0.70, "snare")),
                lookAheadSeconds: 0.10);
            try
            {
                yield return null;

                Assert.That(fixture.Clock.PositionSeconds, Is.LessThan(0));
                Assert.That(fixture.Kick.CurrentState.IsActive, Is.False);
                Assert.That(fixture.Snare.CurrentState.IsActive, Is.False);
                Assert.That(fixture.HiHat.CurrentState.IsActive, Is.False);

                yield return WaitUntil(
                    () => fixture.Kick.CurrentState.Intensity > 0.05f,
                    3,
                    "Kick did not enter the look-ahead window.");
                float firstIntensity = fixture.Kick.CurrentState.Intensity;
                Assert.That(fixture.Snare.CurrentState.IsActive, Is.False);
                Assert.That(fixture.HiHat.CurrentState.IsActive, Is.False);

                yield return WaitUntil(
                    () => fixture.Kick.CurrentState.Intensity > firstIntensity + 0.10f,
                    3,
                    "Kick intensity did not increase toward the note.");

                yield return WaitUntil(
                    () => fixture.Clock.PositionSeconds > 0.31,
                    3,
                    "DSP position did not pass the kick note.");
                yield return null;
                Assert.That(fixture.Kick.CurrentState.IsActive, Is.False);
                Assert.That(fixture.Kick.CurrentState.Intensity, Is.Zero);

                yield return WaitUntil(
                    () => fixture.Snare.CurrentState.Intensity > 0.05f,
                    3,
                    "Snare did not become the next active visual.");
                Assert.That(fixture.Kick.CurrentState.IsActive, Is.False);
                Assert.That(fixture.HiHat.CurrentState.IsActive, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Presenter_supports_simultaneous_notes_on_distinct_pads()
        {
            RuntimeFixture fixture = CreateFixture(
                Notes((0.20, "kick"), (0.20, "snare")),
                lookAheadSeconds: 0.40);
            try
            {
                yield return null;
                yield return WaitUntil(
                    () => fixture.Kick.CurrentState.Intensity > 0.05f &&
                          fixture.Snare.CurrentState.Intensity > 0.05f,
                    3,
                    "Simultaneous pad visuals did not activate.");

                Assert.That(fixture.Kick.CurrentState.IsActive, Is.True);
                Assert.That(fixture.Snare.CurrentState.IsActive, Is.True);
                Assert.That(fixture.HiHat.CurrentState.IsActive, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayPrototype_contains_three_connected_chart_driven_pad_visuals()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            DrumPadVisual[] visuals = UnityEngine.Object.FindObjectsByType<DrumPadVisual>(FindObjectsSortMode.None);
            PadVisualTimelinePresenter[] presenters =
                UnityEngine.Object.FindObjectsByType<PadVisualTimelinePresenter>(FindObjectsSortMode.None);
            DspSongClockPrototype clock =
                UnityEngine.Object.FindFirstObjectByType<DspSongClockPrototype>();

            Assert.That(visuals, Has.Length.EqualTo(3));
            Assert.That(visuals.Select(visual => visual.Pad), Is.EquivalentTo(new[]
            {
                DrumPad.Kick,
                DrumPad.Snare,
                DrumPad.HiHat
            }));
            Assert.That(presenters, Has.Length.EqualTo(1));
            Assert.That(presenters[0].SongClock, Is.SameAs(clock));
            Assert.That(presenters[0].ChartTimeline, Is.Not.Null);
            Assert.That(presenters[0].KickVisual, Is.Not.Null);
            Assert.That(presenters[0].SnareVisual, Is.Not.Null);
            Assert.That(presenters[0].HiHatVisual, Is.Not.Null);

            clock.GetComponent<AudioSource>().mute = true;
            AssertSceneContainsOnlyExpectedComponents(SceneManager.GetActiveScene());

            yield return WaitUntil(
                () => visuals.Any(visual => visual.CurrentState.Intensity > 0.05f),
                3,
                "No scene pad became active from the chart timeline.");
        }

        private static RuntimeFixture CreateFixture(string noteJson, double lookAheadSeconds)
        {
            string json =
                $"{{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{{\"easy\":[{noteJson}]}}}}";
            var chartAsset = new TextAsset(json);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var root = new GameObject("Pad visual integration test");
            var source = root.AddComponent<AudioSource>();
            source.mute = true;
            var clock = root.AddComponent<DspSongClockPrototype>();
            ConfigureClock(clock);
            var timeline = root.AddComponent<ChartTimelinePrototype>();
            ConfigureTimeline(timeline, chartAsset, clock, lookAheadSeconds);

            DrumPadVisual kick = CreateVisual(root.transform, "Kick", DrumPad.Kick, material);
            DrumPadVisual snare = CreateVisual(root.transform, "Snare", DrumPad.Snare, material);
            DrumPadVisual hiHat = CreateVisual(root.transform, "HiHat", DrumPad.HiHat, material);
            var presenter = root.AddComponent<PadVisualTimelinePresenter>();
            SetField(presenter, "songClock", clock);
            SetField(presenter, "chartTimeline", timeline);
            SetField(presenter, "kickVisual", kick);
            SetField(presenter, "snareVisual", snare);
            SetField(presenter, "hiHatVisual", hiHat);

            return new RuntimeFixture(root, chartAsset, material, clock, kick, snare, hiHat);
        }

        private static DrumPadVisual CreateVisual(
            Transform parent,
            string name,
            DrumPad pad,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
            DrumPadVisual visual = gameObject.AddComponent<DrumPadVisual>();
            SetField(visual, "pad", pad);
            SetField(visual, "targetRenderer", gameObject.GetComponent<Renderer>());
            return visual;
        }

        private static void ConfigureClock(DspSongClockPrototype clock)
        {
            SetField(clock, "leadInSeconds", 0.20d);
            SetField(clock, "bpm", 600d);
            SetField(clock, "bars", 1);
            SetField(clock, "beatsPerBar", 4);
            SetField(clock, "sampleRate", 8000);
            SetField(clock, "logLifecycle", false);
        }

        private static void ConfigureTimeline(
            ChartTimelinePrototype timeline,
            TextAsset chartAsset,
            DspSongClockPrototype clock,
            double lookAheadSeconds)
        {
            SetField(timeline, "chartAsset", chartAsset);
            SetField(timeline, "difficulty", "easy");
            SetField(timeline, "songClock", clock);
            SetField(timeline, "lookAheadSeconds", lookAheadSeconds);
            SetField(timeline, "logLifecycle", false);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        private static void AssertSceneContainsOnlyExpectedComponents(Scene scene)
        {
            var expectedTypes = new HashSet<Type>
            {
                typeof(Transform),
                typeof(Camera),
                typeof(AudioListener),
                typeof(Light),
                typeof(AudioSource),
                typeof(DspSongClockPrototype),
                typeof(ChartTimelinePrototype),
                typeof(PadVisualTimelinePresenter),
                typeof(KeyboardDrumInput),
                typeof(CoreMidiGameplayInput),
                typeof(CompositeDrumInput),
                typeof(HitMatchingPrototype),
                typeof(HitResultVisualPresenter),
                typeof(GameplaySessionCoordinator),
                typeof(GameplayHighwayController),
                typeof(UnityEngine.UIElements.UIDocument),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(DrumPadVisual)
            };

            Component[] components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .ToArray();
            Assert.That(components, Has.None.Null);
            Assert.That(
                components.Select(component => component.GetType()).Where(type => !expectedTypes.Contains(type)),
                Is.Empty);
        }

        private static string Notes(params (double time, string pad)[] notes)
        {
            return string.Join(",", notes.Select(note =>
                $"{{\"time\":{note.time.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"\"pad\":\"{note.pad}\"}}"));
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
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
                DspSongClockPrototype clock,
                DrumPadVisual kick,
                DrumPadVisual snare,
                DrumPadVisual hiHat)
            {
                this.root = root;
                this.chartAsset = chartAsset;
                this.material = material;
                Clock = clock;
                Kick = kick;
                Snare = snare;
                HiHat = hiHat;
            }

            public DspSongClockPrototype Clock { get; }
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
    }
}
