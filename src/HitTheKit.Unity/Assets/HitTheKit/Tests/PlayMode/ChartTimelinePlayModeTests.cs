using System.Collections;
using System.Reflection;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HitTheKit.Unity.Tests
{
    public sealed class ChartTimelinePlayModeTests
    {
        private const string TestChartJson =
            "{\"version\":1,\"offsetSeconds\":0,\"difficulties\":{\"easy\":[" +
            "{\"time\":0.05,\"pad\":\"kick\"}," +
            "{\"time\":0.30,\"pad\":\"snare\"}]}}";

        [UnityTest]
        public IEnumerator Controller_loads_serialized_fixture_and_builds_timeline()
        {
            var chartAsset = new TextAsset(TestChartJson);
            var gameObject = new GameObject("Chart timeline controller test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var clock = gameObject.AddComponent<DspSongClockPrototype>();
            ConfigureShortClock(clock);
            var controller = gameObject.AddComponent<ChartTimelinePrototype>();
            ConfigureTimeline(controller, chartAsset, clock, 0.25);

            try
            {
                yield return null;

                Assert.That(controller.ChartAsset, Is.SameAs(chartAsset));
                Assert.That(controller.Difficulty, Is.EqualTo("easy"));
                Assert.That(controller.Chart, Is.Not.Null);
                Assert.That(controller.Timeline, Is.Not.Null);
                Assert.That(controller.Timeline.Notes, Has.Count.EqualTo(2));
            }
            finally
            {
                Object.Destroy(gameObject);
                Object.Destroy(chartAsset);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Timeline_queries_follow_dsp_position_through_preroll_and_elapsed_note()
        {
            var chartAsset = new TextAsset(TestChartJson);
            var gameObject = new GameObject("Chart DSP integration test");
            var source = gameObject.AddComponent<AudioSource>();
            source.mute = true;
            var clock = gameObject.AddComponent<DspSongClockPrototype>();
            ConfigureShortClock(clock);
            var controller = gameObject.AddComponent<ChartTimelinePrototype>();
            ConfigureTimeline(controller, chartAsset, clock, 0.1);

            try
            {
                yield return null;
                Assert.That(clock.PositionSeconds, Is.LessThan(0));
                int preRollElapsed = controller.ElapsedNoteCount;

                float timeout = Time.realtimeSinceStartup + 5;
                while (controller.ElapsedNoteCount == preRollElapsed &&
                       Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(controller.ElapsedNoteCount, Is.GreaterThan(preRollElapsed));
                Assert.That(clock.PositionSeconds, Is.GreaterThan(0.05));
            }
            finally
            {
                Object.Destroy(gameObject);
                Object.Destroy(chartAsset);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayPrototype_contains_connected_chart_timeline_that_advances()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("GameplayPrototype", LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            DspSongClockPrototype[] clocks = Object.FindObjectsByType<DspSongClockPrototype>(FindObjectsSortMode.None);
            ChartTimelinePrototype[] timelines = Object.FindObjectsByType<ChartTimelinePrototype>(FindObjectsSortMode.None);

            Assert.That(clocks, Has.Length.EqualTo(1));
            Assert.That(timelines, Has.Length.EqualTo(1));
            clocks[0].GetComponent<AudioSource>().mute = true;

            ChartTimelinePrototype controller = timelines[0];
            Assert.That(controller.SongClock, Is.SameAs(clocks[0]));
            Assert.That(controller.ChartAsset, Is.Not.Null);
            Assert.That(controller.Difficulty, Is.EqualTo("easy"));
            Assert.That(controller.Chart, Is.Not.Null);
            Assert.That(controller.Timeline.Notes, Has.Count.EqualTo(55));

            float timeout = Time.realtimeSinceStartup + 4;
            while (controller.ElapsedNoteCount == 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(controller.ElapsedNoteCount, Is.GreaterThan(0));
            Assert.That(clocks[0].PositionSeconds, Is.GreaterThan(0.5));
        }

        private static void ConfigureShortClock(DspSongClockPrototype clock)
        {
            SetField(clock, "leadInSeconds", 0.2d);
            SetField(clock, "bpm", 600d);
            SetField(clock, "bars", 1);
            SetField(clock, "beatsPerBar", 4);
            SetField(clock, "sampleRate", 8000);
            SetField(clock, "logLifecycle", false);
        }

        private static void ConfigureTimeline(
            ChartTimelinePrototype timeline,
            TextAsset asset,
            DspSongClockPrototype clock,
            double lookAhead)
        {
            SetField(timeline, "chartAsset", asset);
            SetField(timeline, "difficulty", "easy");
            SetField(timeline, "songClock", clock);
            SetField(timeline, "lookAheadSeconds", lookAhead);
            SetField(timeline, "logLifecycle", false);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
