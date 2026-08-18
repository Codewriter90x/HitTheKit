using System;
using System.Collections.Generic;
using System.Linq;
using HitTheKit.Core;
using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class GameplayLearningPathTests
    {
        private static readonly string[] LessonChartPaths =
        {
            "Assets/HitTheKit/Fixtures/Charts/lesson-01-first-pulse.json",
            "Assets/HitTheKit/Fixtures/Charts/lesson-02-backbeat.json",
            "Assets/HitTheKit/Fixtures/Charts/lesson-03-timekeeper.json",
            "Assets/HitTheKit/Fixtures/Charts/lesson-04-first-groove.json"
        };

        [SetUp]
        public void SetUp()
        {
            GameplaySettingsRuntime.UseForTests(
                new GameplaySettingsService(new InMemoryGameplaySettingsPersistence()));
            GameplaySessionContext.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            GameplaySessionContext.Reset();
            GameplaySettingsRuntime.ResetForTests();
            GameplayLearningProgress.Reset();
        }

        [Test]
        public void Learning_path_has_six_school_modules_and_a_playable_first_semester()
        {
            Assert.That(GameplayLearningPath.All, Has.Count.EqualTo(24));
            Assert.That(GameplayLearningPath.All.Select(lesson => lesson.Number), Is.EqualTo(Enumerable.Range(1, 24)));
            Assert.That(GameplayLearningPath.All.Select(lesson => lesson.Id), Is.Unique);
            Assert.That(GameplayLearningPath.All.Take(12).All(lesson => lesson.IsPlayable), Is.True);
            Assert.That(GameplayLearningPath.All.Skip(12).All(lesson => !lesson.IsPlayable), Is.True);
            Assert.That(GameplayLearningPath.All.Select(lesson => lesson.ModuleNumber).Distinct(),
                Is.EqualTo(Enumerable.Range(1, 6)));
            Assert.That(GameplayLearningPath.All.Where(lesson => lesson.IsModuleAssessment)
                .Select(lesson => lesson.Number), Is.EqualTo(new[] { 4, 8, 12, 16, 20, 24 }));
            Assert.That(GameplayLearningProgress.PlayableCount, Is.EqualTo(12));
            Assert.That(GameplayLearningPath.All.All(lesson => lesson.PracticeMinutes > 0), Is.True);
            Assert.That(GameplayLearningPath.All.All(lesson => !string.IsNullOrWhiteSpace(lesson.ExercisePattern)), Is.True);
            Assert.That(GameplayLearningPath.All.All(lesson => !string.IsNullOrWhiteSpace(lesson.ItalianObjective)), Is.True);
            Assert.That(GameplayLearningPath.All.All(lesson => !string.IsNullOrWhiteSpace(lesson.EnglishObjective)), Is.True);
        }

        [Test]
        public void Progress_unlocks_the_next_lesson_only_after_eighty_percent_at_full_speed()
        {
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.FirstPulse), Is.True);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.Backbeat), Is.False);

            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 0.5, 99);
            Assert.That(GameplayLearningProgress.BestAccuracy(GameplayLessonId.FirstPulse, 0.5), Is.EqualTo(99));
            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.FirstPulse), Is.False);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.Backbeat), Is.False);

            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 79.9);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.Backbeat), Is.False);

            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 80);
            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.FirstPulse), Is.True);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.Backbeat), Is.True);
        }

        [Test]
        public void Module_assessment_unlocks_the_next_playable_module_but_not_future_syllabus()
        {
            foreach (GameplayLessonDefinition lesson in GameplayLearningPath.All.Take(4))
                GameplayLearningProgress.RecordResult(lesson.Id, 1.0, 80);

            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.FirstGroove), Is.True);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.HandCoordination), Is.True);

            foreach (GameplayLessonDefinition lesson in GameplayLearningPath.All.Skip(4).Take(8))
                GameplayLearningProgress.RecordResult(lesson.Id, 1.0, 90);

            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.Groove16th), Is.True);
            Assert.That(GameplayLearningProgress.IsUnlocked(GameplayLessonId.GhostNotesBase), Is.False,
                "Visible second-semester syllabus must not become playable before its grading capabilities exist.");
        }

        [Test]
        public void Session_context_preserves_the_selected_study_speed_and_rejects_preview_lessons()
        {
            GameplaySessionContext.SelectLesson(GameplayLessonId.FirstPulse, 0.75);
            Assert.That(GameplaySessionContext.Current.SpeedMultiplier, Is.EqualTo(0.75));
            Assert.Throws<InvalidOperationException>(() =>
                GameplaySessionContext.SelectLesson(GameplayLessonId.GhostNotesBase, 1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GameplaySessionContext.SelectLesson(GameplayLessonId.FirstPulse, 0.6));
        }

        [Test]
        public void Every_playable_school_lesson_builds_a_valid_deterministic_chart_inside_its_duration()
        {
            var loader = new ChartLoader();
            foreach (GameplayLessonDefinition lesson in GameplayLearningPath.All.Where(item => item.IsPlayable))
            {
                string first = GameplayLessonChartBuilder.BuildJson(lesson.Id);
                string second = GameplayLessonChartBuilder.BuildJson(lesson.Id);
                LoadedChart chart = loader.Load(first, "easy");
                double duration = lesson.Bars * 4 * 60.0 / lesson.Bpm;

                Assert.That(second, Is.EqualTo(first), lesson.Id.ToString());
                Assert.That(chart.Notes, Is.Not.Empty, lesson.Id.ToString());
                Assert.That(chart.Notes.All(note => note.TimeSeconds >= 0 && note.TimeSeconds < duration),
                    Is.True, lesson.Id.ToString());
            }
        }

        [Test]
        public void Technical_charts_make_sticking_order_observable_without_claiming_hand_detection()
        {
            var loader = new ChartLoader();
            LoadedChart singles = loader.Load(GameplayLessonChartBuilder.BuildJson(GameplayLessonId.HandCoordination), "easy");
            LoadedChart doubles = loader.Load(GameplayLessonChartBuilder.BuildJson(GameplayLessonId.HandDoubles), "easy");
            LoadedChart paradiddle = loader.Load(GameplayLessonChartBuilder.BuildJson(GameplayLessonId.Paradiddle), "easy");

            Assert.That(singles.Notes.Select(note => note.Pad).Distinct(), Is.EquivalentTo(new[] { DrumPad.Snare }));
            Assert.That(doubles.Notes.Select(note => note.Pad).Distinct(),
                Is.EquivalentTo(new[] { DrumPad.Snare, DrumPad.Tom1 }));
            Assert.That(paradiddle.Notes.Take(8).Select(note => note.Pad), Is.EqualTo(new[]
            {
                DrumPad.Snare, DrumPad.Tom1, DrumPad.Snare, DrumPad.Snare,
                DrumPad.Tom1, DrumPad.Snare, DrumPad.Tom1, DrumPad.Tom1
            }));
        }

        [Test]
        public void Pass_and_mastery_are_distinct_full_speed_states()
        {
            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 85);
            Assert.That(GameplayLearningProgress.IsCompleted(GameplayLessonId.FirstPulse), Is.True);
            Assert.That(GameplayLearningProgress.IsMastered(GameplayLessonId.FirstPulse), Is.False);

            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 92);
            Assert.That(GameplayLearningProgress.IsMastered(GameplayLessonId.FirstPulse), Is.True);
        }

        [Test]
        public void Timeline_and_matching_notes_scale_together_for_slow_practice()
        {
            const string json = "{\"version\":1,\"offsetSeconds\":0.5,\"difficulties\":{\"easy\":[{\"time\":1.5,\"pad\":\"kick\"}]}}";
            LoadedChart chart = new ChartLoader().Load(json, "easy");
            var timeline = new ChartTimeline(chart, 0.5);
            Assert.That(timeline.Notes.Single().EffectiveTimeSeconds, Is.EqualTo(4.0));
        }

        [Test]
        public void Lesson_charts_add_pieces_gradually_instead_of_starting_with_the_full_kit()
        {
            var loader = new ChartLoader();
            var padSets = new List<HashSet<DrumPad>>();
            foreach (string path in LessonChartPaths)
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                Assert.That(asset, Is.Not.Null, path);
                LoadedChart chart = loader.Load(asset.text, "easy");
                padSets.Add(chart.Notes.Select(note => note.Pad).ToHashSet());
            }

            Assert.That(padSets[0], Is.EquivalentTo(new[] { DrumPad.Kick }));
            Assert.That(padSets[1], Is.EquivalentTo(new[] { DrumPad.Kick, DrumPad.Snare }));
            Assert.That(padSets[2], Is.EquivalentTo(new[] { DrumPad.Kick, DrumPad.Snare, DrumPad.HiHat }));
            Assert.That(padSets[3], Does.Contain(DrumPad.Kick));
            Assert.That(padSets[3], Does.Contain(DrumPad.Snare));
            Assert.That(padSets[3], Does.Contain(DrumPad.HiHat));
        }

        [TestCase(-2.00, 120, 4, 4)]
        [TestCase(-1.51, 120, 4, 4)]
        [TestCase(-1.49, 120, 4, 3)]
        [TestCase(-0.51, 120, 4, 2)]
        [TestCase(-0.01, 120, 4, 1)]
        [TestCase(0.00, 120, 4, 0)]
        public void Count_in_reports_musical_beats_not_wall_clock_seconds(
            double position,
            double bpm,
            int beats,
            int expected)
        {
            Assert.That(GameplayCountIn.RemainingBeat(position, bpm, beats), Is.EqualTo(expected));
        }

        [Test]
        public void Session_context_keeps_free_play_and_lessons_explicit()
        {
            GameplaySessionContext.SelectLesson(GameplayLessonId.Backbeat);
            Assert.That(GameplaySessionContext.Current.Kind, Is.EqualTo(GameplaySessionKind.Lesson));
            Assert.That(GameplaySessionContext.Current.LessonId, Is.EqualTo(GameplayLessonId.Backbeat));
            Assert.That(GameplaySessionContext.Current.ReturnTarget, Is.EqualTo(GameplayReturnTarget.LearningPath));

            GameplaySessionContext.SelectFreePlay();
            Assert.That(GameplaySessionContext.Current.Kind, Is.EqualTo(GameplaySessionKind.FreePlay));
            Assert.That(GameplaySessionContext.Current.LessonId, Is.Null);
            Assert.That(GameplaySessionContext.Current.ReturnTarget, Is.EqualTo(GameplayReturnTarget.MainMenu));
        }

        [Test]
        public void Audio_feedback_plays_drums_only_for_player_input_and_uses_a_distinct_miss_cue()
        {
            var input = new DrumInputEvent(DrumPad.Kick, 100, 1);
            var matching = new HitMatchingSession(
                new[] { new ChartNote(1, DrumPad.Kick), new ChartNote(2, DrumPad.Snare) },
                new TimingWindows(0.04, 0.08, 0.14),
                0);
            Assert.That(matching.ProcessInput(input, out HitResult matchedResult), Is.True);
            HitResult miss = null;
            matching.HitResolved += result => { if (result.Grade == HitGrade.Miss) miss = result; };
            matching.ProcessMisses(2.2);

            GameplayAudioFeedbackDecision matched = GameplayAudioFeedbackPolicy.ForInput(input, matchedResult);
            GameplayAudioFeedbackDecision wrong = GameplayAudioFeedbackPolicy.ForInput(input, null);
            GameplayAudioFeedbackDecision midi = GameplayAudioFeedbackPolicy.ForInput(
                new DrumInputEvent(DrumPad.Kick, 100, 1, DrumInputSource.Midi),
                matchedResult);
            GameplayAudioFeedbackDecision wrongMidi = GameplayAudioFeedbackPolicy.ForInput(
                new DrumInputEvent(DrumPad.Snare, 100, 1, DrumInputSource.Midi),
                null);

            Assert.That(matched.PlayDrum, Is.True);
            Assert.That(matched.PlayMistake, Is.False);
            Assert.That(wrong.PlayDrum, Is.True);
            Assert.That(wrong.PlayMistake, Is.True);
            Assert.That(midi.PlayDrum, Is.False);
            Assert.That(midi.PlayMistake, Is.False);
            Assert.That(wrongMidi.PlayDrum, Is.False);
            Assert.That(wrongMidi.PlayMistake, Is.False,
                "A wrong MIDI hit must not trigger synthetic audio over the physical drum kit.");
            Assert.That(miss, Is.Not.Null);
            Assert.That(GameplayAudioFeedbackPolicy.ShouldPlayMiss(miss), Is.True);
        }

        [Test]
        public void Generated_player_drum_and_mistake_clips_are_deterministic_bounded_and_separate()
        {
            IReadOnlyDictionary<DrumPad, AudioClip> first = GeneratedDrumFeedbackFactory.CreateKit(8000);
            IReadOnlyDictionary<DrumPad, AudioClip> second = GeneratedDrumFeedbackFactory.CreateKit(8000);
            AudioClip mistake = GeneratedDrumFeedbackFactory.CreateMistake(8000);
            try
            {
                Assert.That(first, Has.Count.EqualTo(Enum.GetValues(typeof(DrumPad)).Length));
                float[] firstKick = Samples(first[DrumPad.Kick]);
                float[] secondKick = Samples(second[DrumPad.Kick]);
                float[] miss = Samples(mistake);
                Assert.That(firstKick, Is.EqualTo(secondKick));
                Assert.That(firstKick, Is.Not.EqualTo(miss));
                Assert.That(firstKick.Any(sample => Math.Abs(sample) > 0.01f), Is.True);
                Assert.That(firstKick.All(sample => sample >= -1 && sample <= 1), Is.True);
            }
            finally
            {
                foreach (AudioClip clip in first.Values) UnityEngine.Object.DestroyImmediate(clip);
                foreach (AudioClip clip in second.Values) UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(mistake);
            }
        }

        private static float[] Samples(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True);
            return samples;
        }
    }
}
