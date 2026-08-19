using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HitTheKit.Unity.DeviceSetup;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.MainMenu;
using HitTheKit.Unity.Input;
using HitTheKit.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Tests
{
    public sealed class MainMenuPlayModeTests
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
            GameplayLearningProgress.Reset();
        }

        [UnityTest]
        public IEnumerator Main_menu_scene_loads_stage_command_with_accessible_destinations()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);

            Assert.That(controller.IsViewBound, Is.True);
            Assert.That(controller.SelectedDestination, Is.EqualTo(MainMenuDestination.Play));
            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document.panelSettings.clearColor, Is.False,
                "The main-menu panel must preserve the camera-rendered 3D stage.");
            Assert.That(document.rootVisualElement.Q<VisualElement>("main-menu-background"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("menu-play").focusable, Is.True);
            Assert.That(document.rootVisualElement.Q<Button>("menu-learn").focusable, Is.True);
            Assert.That(document.rootVisualElement.Q<Button>("menu-setup").focusable, Is.True);
            Assert.That(controller.IsLearnOverlayVisible, Is.False);
            Assert.That(controller.IsSettingsOverlayVisible, Is.False);
            MainMenuStageEnvironment stage = controller.StageEnvironment;
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.IsReady, Is.True);
            Assert.That(stage.CrowdRowCount, Is.EqualTo(6));
            Assert.That(stage.AudienceRendererCount, Is.GreaterThanOrEqualTo(70),
                "The concert audience must use batched human anatomy/material groups, not capsule silhouettes.");
            Assert.That(stage.AudienceSkinRendererCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(stage.AudienceClothingRendererCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(stage.MovingSpotlightCount, Is.EqualTo(6));
            Assert.That(stage.ModelRendererCount, Is.GreaterThan(220),
                "The HD stage asset must retain its detailed drum hardware instead of falling back to the low-detail kit.");
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement
                .Q<VisualElement>("main-menu-background").resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                "The static background must remain a fallback when the 3D diorama is available.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Menu_selection_language_learn_and_settings_states_are_observable()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);

            controller.SelectDestination(MainMenuDestination.Learn, true);
            controller.ActivateSelected();
            Assert.That(controller.IsLearnOverlayVisible, Is.True);
            controller.CloseLearn();
            Assert.That(controller.IsLearnOverlayVisible, Is.False);

            controller.ToggleLanguage();
            Assert.That(controller.Language, Is.EqualTo(MainMenuLanguage.English));
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement
                .Q<Button>("menu-play").Q<Label>("choice-title").text, Is.EqualTo("PLAY"));
            Assert.That(controller.GetComponent<UIDocument>().rootVisualElement
                .Q<Button>("menu-learn").Q<Label>("choice-state").text, Is.EqualTo("SELECT"));

            controller.ToggleSettings();
            Assert.That(controller.IsSettingsOverlayVisible, Is.True);
            controller.ToggleSettings();
            Assert.That(controller.IsSettingsOverlayVisible, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Gameplay_theme_is_selected_in_settings_and_applied_to_the_next_session()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ToggleSettings();

            controller.SelectGameplayTheme(GameplayPresentationTheme.PrecisionGrid);

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(controller.SelectedGameplayTheme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(controller.StageEnvironment.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(root.Q<Button>("settings-theme-precision")
                .ClassListContains("theme-settings-button--selected"), Is.True);
            controller.ToggleSettings();
            controller.SelectDestination(MainMenuDestination.Play);
            controller.ActivateSelected();
            Assert.That(controller.IsSongLibraryVisible, Is.True);
            Assert.That(controller.StartSelectedSong(), Is.True);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(gameplay.CurrentSession.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(gameplay.Theme, Is.EqualTo(GameplayPresentationTheme.PrecisionGrid));
            Assert.That(gameplay.GetComponent<UIDocument>().rootVisualElement.Q<Button>("theme-precision"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Play_opens_a_folder_discovered_setlist_with_ready_and_incomplete_songs()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);

            controller.SelectDestination(MainMenuDestination.Play);
            controller.ActivateSelected();

            Assert.That(controller.IsSongLibraryVisible, Is.True);
            Assert.That(controller.SongLibrary, Is.Not.Null);
            Assert.That(controller.SongLibrary.Songs.Select(song => song.Id), Does.Contain("neon-circuit"));
            Assert.That(controller.SongLibrary.Songs.Select(song => song.Id), Does.Contain("local-song-example"));
            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<Button>("song-row-neon-circuit"), Is.Not.Null);
            Assert.That(root.Q<Button>("song-row-local-song-example"), Is.Not.Null);

            controller.SelectSong("neon-circuit");
            Assert.That(controller.SelectedSongId, Is.EqualTo("neon-circuit"));
            Assert.That(root.Q<Button>("song-play-button").enabledSelf, Is.True);

            controller.SelectSong("local-song-example");
            Assert.That(controller.SelectedSongId, Is.EqualTo("local-song-example"));
            Assert.That(root.Q<Label>("song-detail-title").text, Is.EqualTo("LOCAL SONG EXAMPLE"));
            Assert.That(root.Q<Label>("song-detail-artist").text, Is.EqualTo("YOUR LIBRARY"));
            Assert.That(root.Q<Button>("song-play-button").enabledSelf, Is.False);
            Assert.That(root.Q<Button>("song-record-button").enabledSelf, Is.False);
            Assert.That(root.Q<Button>("song-speed-sixty").enabledSelf, Is.False);
            Assert.That(root.Q<VisualElement>("song-difficulty-buttons").childCount, Is.Zero);
            Assert.That(controller.StartSelectedSong(), Is.False,
                "Metadata-only songs must remain visible without starting until authorized audio and chart files exist.");

            controller.SelectSong("neon-circuit");
            Assert.That(controller.SelectedSongId, Is.EqualTo("neon-circuit"));
            Assert.That(root.Q<Button>("song-play-button").enabledSelf, Is.True);
            Assert.That(root.Q<Button>("song-record-button").enabledSelf, Is.True);
            Assert.That(root.Q<VisualElement>("song-difficulty-buttons").Query<Button>().ToList(), Has.Count.EqualTo(1));
            Assert.That(root.Q<Button>("song-difficulty-easy"), Is.Not.Null);
            controller.SelectSongSpeed(0.6);
            Assert.That(controller.SelectedSongSpeed, Is.EqualTo(0.6));
            Assert.That(controller.SelectedSongDifficulty, Is.EqualTo("easy"));
            Assert.That(root.Q<Button>("song-speed-sixty")
                .ClassListContains("song-speed-button--selected"), Is.True);
            Assert.That(root.Q<Label>("song-effective-bpm").text, Does.Contain("72 BPM"));
            Assert.That(controller.StartSelectedSong(), Is.True);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay.CurrentSession.SongId, Is.EqualTo("neon-circuit"));
            Assert.That(gameplay.CurrentSession.SpeedMultiplier, Is.EqualTo(0.6));
            Assert.That(gameplay.CurrentSession.Difficulty, Is.EqualTo("easy"));
            Assert.That(gameplay.CurrentSession.CountInBeats * 60.0 / gameplay.CurrentSession.Bpm,
                Is.GreaterThanOrEqualTo(6.0));
            Assert.That(gameplay.CurrentSession.ReturnTarget, Is.EqualTo(GameplayReturnTarget.SongLibrary));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Chart_creator_launches_the_real_gameplay_session_for_the_selected_playable_song()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowSongLibrary();
            controller.SelectSong("neon-circuit");
            controller.SelectSongSpeed(0.6);

            Assert.That(controller.StartChartCreator(), Is.True);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(gameplay.CurrentSession.IsChartCreator, Is.True);
            Assert.That(gameplay.CurrentSession.SongId, Is.EqualTo("neon-circuit"));
            Assert.That(gameplay.CurrentSession.SpeedMultiplier, Is.EqualTo(0.6));
            Assert.That(gameplay.GetComponent<UIDocument>().rootVisualElement
                .Q<VisualElement>("chart-creator-results"), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Chart_creator_audio_import_opens_a_guided_metadata_form_without_exposing_the_local_path()
        {
            string audio = Path.Combine(Application.temporaryCachePath, "authoring-ui.wav");
            WriteSilentWave(audio);
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowSongLibrary();

            controller.BeginChartAuthoringAudio(audio);
            yield return null;

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(controller.IsChartAudioImportVisible, Is.True);
            Assert.That(root.Q<VisualElement>("chart-audio-import-overlay").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("chart-audio-file").text, Is.EqualTo("authoring-ui.wav"));
            Assert.That(root.Q<Label>("chart-audio-file").text, Does.Not.Contain(Application.temporaryCachePath));
            Assert.That(root.Q<TextField>("chart-audio-title").value, Is.EqualTo("authoring ui"));
            Assert.That(root.Q<TextField>("chart-audio-artist").value, Is.Empty);
            Assert.That(root.Q<TextField>("chart-audio-bpm").value, Is.Empty,
                "Unknown timing must not be silently replaced with a default BPM.");
            Assert.That(root.Q<TextField>("chart-audio-bars").value, Is.Empty);
            Assert.That(root.Q<TextField>("chart-audio-beats").value, Is.Empty);
            Assert.That(root.Q<Button>("chart-audio-start"), Is.Not.Null);
            Assert.That(root.Q<Button>("chart-audio-cancel"), Is.Not.Null);

            File.Delete(audio);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Audio_only_authoring_source_loads_the_real_dsp_audio_with_an_empty_chart()
        {
            string root = Path.Combine(Application.temporaryCachePath, "hitthekit-authoring-session-" +
                System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string source = Path.Combine(root, "source.wav");
            WriteSilentWave(source);
            SongLibraryEntry song = new ChartAuthoringAudioImporter().Import(
                new ChartAuthoringAudioRequest(source, "Authoring Song", "Local Artist", 120, 1, 4),
                Path.Combine(root, "library"),
                new System.DateTimeOffset(2026, 8, 19, 15, 0, 0, System.TimeSpan.Zero)).Song;
            GameplaySessionContext.SelectChartCreator(song, 1.0, "easy");

            AsyncOperation operation = SceneManager.LoadSceneAsync(MainMenuRoutes.GameplayScene, LoadSceneMode.Single);
            while (!operation.isDone) yield return null;
            var clock = Object.FindFirstObjectByType<HitTheKit.Unity.Audio.DspSongClockPrototype>();
            float deadline = Time.realtimeSinceStartup + 4f;
            while (clock.GeneratedClip == null && string.IsNullOrEmpty(clock.LoadError) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            var timeline = Object.FindFirstObjectByType<HitTheKit.Unity.Charts.ChartTimelinePrototype>();
            Assert.That(gameplay.CurrentSession.IsChartCreator, Is.True);
            Assert.That(gameplay.CurrentSession.Chart, Is.EqualTo(GameplaySessionChart.AuthoringEmpty));
            Assert.That(clock.ExternalAudioPath, Is.EqualTo(song.AudioPath));
            Assert.That(clock.LoadError, Is.Null.Or.Empty);
            Assert.That(clock.GeneratedClip, Is.Not.Null);
            Assert.That(timeline.Chart.Notes, Is.Empty);
            Assert.That(Object.FindFirstObjectByType<HitTheKit.Unity.Matching.HitMatchingPrototype>()
                .Session.Notes, Is.Empty);

            Directory.Delete(root, true);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Compact_song_library_keeps_rows_controls_and_actions_inside_their_panels()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowSongLibrary();

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            VisualElement app = root.Q<VisualElement>("main-menu-app");
            app.AddToClassList("main-menu--compact");
            app.AddToClassList("main-menu--short");
            yield return null;

            Button row = root.Q<Button>("song-row-neon-circuit");
            Assert.That(row, Is.Not.Null);
            Label index = row.Q<Label>(className: "song-row-index");
            Label title = row.Q<Label>(className: "song-row-title");
            Label artist = row.Q<Label>(className: "song-row-artist");
            Label metadata = row.Q<Label>(className: "song-row-meta");
            Label state = row.Q<Label>(className: "song-row-state");
            AssertContained(row, index, "song number");
            AssertContained(row, title, "song title");
            AssertContained(row, artist, "song artist");
            AssertContained(row, metadata, "song metadata");
            AssertContained(row, state, "song availability");
            Assert.That(title.worldBound.xMin, Is.GreaterThanOrEqualTo(index.worldBound.xMax - 1));
            Assert.That(state.worldBound.xMin, Is.GreaterThan(title.worldBound.xMin));

            VisualElement detail = root.Q<VisualElement>(className: "song-detail-card");
            ScrollView detailScroll = root.Q<ScrollView>("song-detail-scroll");
            Assert.That(detailScroll, Is.Not.Null);
            AssertContained(detail, root.Q<VisualElement>("song-difficulty-buttons"), "difficulty selector");
            AssertContained(detail, root.Q<Button>("song-speed-sixty"), "sixty-percent speed button");
            AssertContained(detail, root.Q<Button>("song-speed-ninety"), "ninety-percent speed button");
            AssertContained(detail, root.Q<Button>("song-speed-full"), "full-speed button");
            AssertContained(detail, root.Q<Button>("song-play-button"), "play button");
            Assert.That(root.Q<Button>("song-play-button").resolvedStyle.height, Is.GreaterThanOrEqualTo(40f));

            VisualElement panel = root.Q<VisualElement>(className: "song-library-panel");
            AssertContained(panel, root.Q<Button>("song-refresh-button"), "refresh button");
            AssertContained(panel, root.Q<Button>("song-folder-button"), "folder button");
            AssertContained(panel, root.Q<Button>("song-back-button"), "back button");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Full_screen_song_library_clears_stage_depth_and_keeps_launch_action_visible()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);

            Assert.That(controller.GetComponent<UIDocument>().panelSettings.clearDepthStencil, Is.True,
                "UI Toolkit clipping must start from a clean depth/stencil buffer above the 3D stage.");
            controller.ShowSongLibrary();
            yield return null;

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            VisualElement detail = root.Q<VisualElement>(className: "song-detail-card");
            Button play = root.Q<Button>("song-play-button");
            Assert.That(play.enabledSelf, Is.True);
            Assert.That(play.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(play.resolvedStyle.height, Is.GreaterThanOrEqualTo(40f));
            Assert.That(play.worldBound.width, Is.GreaterThan(180f));
            AssertContained(detail, play, "fixed song launch action");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Empty_song_library_keeps_the_menu_readable_and_launch_disabled()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowSongLibrary();

            string emptyRoot = Path.Combine(Application.temporaryCachePath, "hitthekit-empty-song-library");
            if (Directory.Exists(emptyRoot)) Directory.Delete(emptyRoot, true);
            Directory.CreateDirectory(emptyRoot);
            try
            {
                SongLibrarySnapshot emptyLibrary = new SongLibraryDiscovery().Discover(new[]
                {
                    new SongLibraryRoot(emptyRoot, SongLibraryOrigin.UserFolder)
                });
                typeof(MainMenuController).GetField("songLibrary", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, emptyLibrary);
                typeof(MainMenuController).GetField("selectedSongId", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(controller, null);
                typeof(MainMenuController).GetMethod("RenderSongLibrary", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller, null);

                VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(root.Q<Label>("song-library-count").text, Does.StartWith("0 "));
                Assert.That(root.Q<Label>("song-detail-title").text, Is.EqualTo("NESSUN BRANO"));
                Assert.That(root.Q<Label>("song-detail-readiness").text, Does.Contain("song.json"));
                Assert.That(root.Q<Button>("song-play-button").enabledSelf, Is.False);
                Assert.That(controller.StartSelectedSong(), Is.False);
                Assert.That(GameplaySessionContext.Current.Kind, Is.EqualTo(GameplaySessionKind.FreePlay));
            }
            finally
            {
                if (Directory.Exists(emptyRoot)) Directory.Delete(emptyRoot, true);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Learn_setlist_shows_progressive_unlock_accuracy_and_real_study_speed()
        {
            GameplayLearningProgress.RecordResult(GameplayLessonId.FirstPulse, 1.0, 91.5);
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowLearn();

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            ScrollView list = root.Q<ScrollView>("learn-list");
            Assert.That(list.Query<Button>(className: "lesson-row").ToList(), Has.Count.EqualTo(24));
            Assert.That(root.Q<Button>("learn-lesson-02").enabledSelf, Is.True);
            Assert.That(root.Q<Button>("learn-lesson-03").enabledSelf, Is.False);
            Assert.That(root.Q<Label>("learn-progress-accuracy").text, Does.Contain("91.5%"));

            controller.SelectLearningLesson(GameplayLessonId.Backbeat);
            controller.SelectStudySpeed(0.5);
            Assert.That(root.Q<Label>("learn-effective-bpm").text, Does.Contain("34 BPM"));
            Assert.That(root.Q<Button>("learn-speed-half").ClassListContains("study-speed-button--selected"), Is.True);
            Button start = root.Q<Button>("learn-start-button");
            yield return null;
            Assert.That(start.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(start.worldBound.width, Is.GreaterThan(120f));
            Assert.That(start.worldBound.height, Is.GreaterThan(40f));
            Assert.That(root.focusController.focusedElement, Is.Not.SameAs(start),
                "Opening Learn must not focus Start during the same Enter event and accidentally launch it.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Learn_and_settings_show_persisted_practice_summary_and_backup_controls()
        {
            var progress = new GameplayProgressService(new InMemoryGameplayProgressPersistence());
            progress.AddPracticeTime(7380);
            progress.RecordCompletedSession(new GameplaySessionResult(
                "44444444444444444444444444444444",
                "2026-08-08T10:00:00.0000000+00:00",
                GameplaySessionKind.FreePlay,
                null,
                1.0,
                60,
                1000,
                100,
                1,
                1,
                0,
                0,
                0,
                0));
            GameplayProgressRuntime.UseForTests(progress);
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ShowLearn();

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<Label>("learn-progress-time").text, Does.Contain("2h 03m"));
            controller.ToggleSettings();
            Assert.That(root.Q<Label>("settings-progress-summary").text, Does.Contain("2h 03m"));
            Assert.That(root.Q<Label>("settings-progress-summary").text, Does.Contain("1 SESSIONI"));
            Assert.That(root.Q<Button>("settings-export-progress"), Is.Not.Null);
            Assert.That(root.Q<Button>("settings-import-progress"), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Learn_is_a_full_section_and_double_kick_confirms_the_selected_destination()
        {
            PlayerPreferencesRuntime.Current.SetFirstRunCompleted(true);
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.SelectDestination(MainMenuDestination.Learn);

            Assert.That(controller.ProcessDrumInput(new DrumInputEvent(DrumPad.Kick, 100, 0), 3.0), Is.False);
            Assert.That(controller.ProcessDrumInput(new DrumInputEvent(DrumPad.Kick, 100, 0), 3.2), Is.True);
            Assert.That(controller.IsLearnOverlayVisible, Is.True);
            Assert.That(controller.ConfirmCurrentAction(), Is.False,
                "The input event that opens Learn must not also start the lesson in the same frame.");

            VisualElement root = controller.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q<VisualElement>("main-home").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<VisualElement>("learn-overlay").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<VisualElement>(className: "learn-rail"), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Main_menu_routes_to_device_setup_and_back()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.SelectDestination(MainMenuDestination.DeviceSetup);
            controller.ActivateSelected();
            yield return WaitForScene(MainMenuRoutes.DeviceSetupScene);

            DeviceSetupController setup = Object.FindFirstObjectByType<DeviceSetupController>();
            Assert.That(setup, Is.Not.Null);
            setup.Flow.Start();
            setup.View.Render(setup.Flow.Snapshot);
            Button persistentMenu = setup.GetComponent<UIDocument>().rootVisualElement.Q<Button>("main-menu-button");
            Assert.That(persistentMenu, Is.Not.Null);
            Assert.That(persistentMenu.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            setup.ReturnToMainMenu();
            yield return WaitForScene(MainMenuRoutes.MainMenuScene);
            Assert.That(Object.FindFirstObjectByType<MainMenuController>(), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Learn_lesson_selection_configures_a_real_beginner_gameplay_session()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.StartLesson(GameplayLessonId.FirstPulse);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(gameplay.SessionKind, Is.EqualTo(GameplaySessionKind.Lesson));
            Assert.That(gameplay.SessionTitle, Is.EqualTo("PRIMO BATTITO"));
            Assert.That(Object.FindFirstObjectByType<HitTheKit.Unity.Audio.DspSongClockPrototype>().Bpm, Is.EqualTo(64));
            Assert.That(Object.FindFirstObjectByType<HitTheKit.Unity.Charts.ChartTimelinePrototype>()
                .Chart.Notes.Select(note => note.Pad).Distinct(), Is.EquivalentTo(new[] { HitTheKit.Core.DrumPad.Kick }));
            GameplaySessionContext.Reset();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Slower_learning_launch_scales_song_and_chart_timing_together()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.StartLesson(GameplayLessonId.FirstPulse, 0.5);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            Assert.That(Object.FindFirstObjectByType<HitTheKit.Unity.Audio.DspSongClockPrototype>().Bpm, Is.EqualTo(32));
            var timeline = Object.FindFirstObjectByType<HitTheKit.Unity.Charts.ChartTimelinePrototype>();
            Assert.That(timeline.PlaybackSpeed, Is.EqualTo(0.5));
            Assert.That(timeline.Timeline.Notes.First().EffectiveTimeSeconds, Is.EqualTo(0.0).Within(0.001),
                "The lesson chart starts on beat one; the existing song-clock count-in owns preparation time.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Gameplay_returns_to_main_menu_without_persisting_scene_state()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.ActivateSelected();
            controller.SelectSong("neon-circuit");
            Assert.That(controller.StartSelectedSong(), Is.True);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay, Is.Not.Null);
            gameplay.ReturnToMainMenu();
            yield return WaitForScene(MainMenuRoutes.MainMenuScene);
            MainMenuController returned = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(returned.SelectedDestination, Is.EqualTo(MainMenuDestination.Play));
            Assert.That(returned.IsSongLibraryVisible, Is.True);
            Assert.That(returned.SelectedSongId, Is.EqualTo("neon-circuit"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Learning_gameplay_returns_to_the_same_learn_section_and_speed()
        {
            MainMenuController controller = null;
            yield return LoadMainMenu(value => controller = value);
            controller.StartLesson(GameplayLessonId.FirstPulse, 0.5);
            yield return WaitForScene(MainMenuRoutes.GameplayScene);

            GameplayHighwayController gameplay = Object.FindFirstObjectByType<GameplayHighwayController>();
            Assert.That(gameplay, Is.Not.Null);
            gameplay.ReturnToMainMenu();
            yield return WaitForScene(MainMenuRoutes.MainMenuScene);

            MainMenuController returned = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(returned.IsLearnOverlayVisible, Is.True);
            Assert.That(returned.SelectedDestination, Is.EqualTo(MainMenuDestination.Learn));
            Assert.That(returned.SelectedLesson, Is.EqualTo(GameplayLessonId.FirstPulse));
            Assert.That(returned.SelectedStudySpeed, Is.EqualTo(0.5));
            Assert.That(returned.GetComponent<UIDocument>().rootVisualElement.Q<Button>("learn-start-button").text,
                Does.Contain("32 BPM"));
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertContained(VisualElement container, VisualElement child, string description)
        {
            const float PixelTolerance = 4f;
            Assert.That(container, Is.Not.Null, $"Missing container for {description}.");
            Assert.That(child, Is.Not.Null, $"Missing {description}.");
            Rect outer = container.worldBound;
            Rect inner = child.worldBound;
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - PixelTolerance), description);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - PixelTolerance), description);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + PixelTolerance), description);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + PixelTolerance), description);
        }

        private static void WriteSilentWave(string path)
        {
            const int sampleRate = 8000;
            const int sampleCount = 8000;
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + sampleCount * 2);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(sampleCount * 2);
                for (int index = 0; index < sampleCount; index++) writer.Write((short)0);
            }
        }

        private static IEnumerator LoadMainMenu(System.Action<MainMenuController> ready)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(MainMenuRoutes.MainMenuScene, LoadSceneMode.Single);
            while (!operation.isDone) yield return null;
            float deadline = Time.realtimeSinceStartup + 3f;
            MainMenuController controller = null;
            while (controller == null && Time.realtimeSinceStartup < deadline)
            {
                controller = Object.FindFirstObjectByType<MainMenuController>();
                yield return null;
            }
            Assert.That(controller, Is.Not.Null);
            ready(controller);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            yield return null;
        }
    }
}
