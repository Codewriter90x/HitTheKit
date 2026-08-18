using HitTheKit.Unity.Audio;
using HitTheKit.Unity.Charts;
using HitTheKit.Unity.Gameplay;
using HitTheKit.Unity.Matching;
using HitTheKit.Unity.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Editor
{
    public static class GameplayHighwaySceneBuilder
    {
        private const string ScenePath = "Assets/HitTheKit/Scenes/GameplayPrototype.unity";
        private const string UxmlPath = "Assets/HitTheKit/UI/Gameplay/GameplayHighway.uxml";
        private const string PanelSettingsPath = "Assets/HitTheKit/UI/DeviceSetup/DeviceSetupPanelSettings.asset";
        private const string ArcadeBackgroundPath = "Assets/HitTheKit/UI/Gameplay/Backgrounds/arcade-neon-environment-v2.png";
        private const string ConcertBackgroundPath = "Assets/HitTheKit/UI/Gameplay/Backgrounds/concert-stage-environment-v2.png";
        private const string PrecisionBackgroundPath = "Assets/HitTheKit/UI/Gameplay/Backgrounds/precision-grid-environment-v2.png";
        private const string DemoChartPath = "Assets/HitTheKit/Fixtures/Charts/neon-circuit-demo-chart.json";
        private const string FirstPulseChartPath = "Assets/HitTheKit/Fixtures/Charts/lesson-01-first-pulse.json";
        private const string BackbeatChartPath = "Assets/HitTheKit/Fixtures/Charts/lesson-02-backbeat.json";
        private const string TimekeeperChartPath = "Assets/HitTheKit/Fixtures/Charts/lesson-03-timekeeper.json";
        private const string FirstGrooveChartPath = "Assets/HitTheKit/Fixtures/Charts/lesson-04-first-groove.json";
        private const string GenericProfilePath = "Assets/HitTheKit/Fixtures/DeviceProfiles/generic-gm-drums-v1.json";

        [MenuItem("HitTheKit/Gameplay/Rebuild Highway UI")]
        public static void Rebuild()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyExistingRoot();

            DspSongClockPrototype clock = Object.FindFirstObjectByType<DspSongClockPrototype>();
            ChartTimelinePrototype timeline = Object.FindFirstObjectByType<ChartTimelinePrototype>();
            HitMatchingPrototype matching = Object.FindFirstObjectByType<HitMatchingPrototype>();
            KeyboardDrumInput keyboard = Object.FindFirstObjectByType<KeyboardDrumInput>();
            if (clock == null || timeline == null || matching == null || keyboard == null)
            {
                throw new System.InvalidOperationException(
                    "GameplayPrototype requires clock, timeline, and matching before the highway UI can be built.");
            }

            Camera mainCamera = Object.FindFirstObjectByType<Camera>();
            if (mainCamera != null && mainCamera.GetComponent<AudioListener>() == null)
                mainCamera.gameObject.AddComponent<AudioListener>();

            var root = new GameObject("GameplayHighwayUI");
            UIDocument document = root.AddComponent<UIDocument>();
            document.panelSettings = RequireAsset<PanelSettings>(PanelSettingsPath);
            document.visualTreeAsset = RequireAsset<VisualTreeAsset>(UxmlPath);
            document.sortingOrder = 20;

            GameplayHighwayController controller = root.AddComponent<GameplayHighwayController>();
            GameplaySessionCoordinator coordinator = root.AddComponent<GameplaySessionCoordinator>();
            AudioSource feedbackSource = root.AddComponent<AudioSource>();
            feedbackSource.playOnAwake = false;
            feedbackSource.loop = false;
            feedbackSource.spatialBlend = 0;
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("document").objectReferenceValue = document;
            serialized.FindProperty("chartTimeline").objectReferenceValue = timeline;
            serialized.FindProperty("songClock").objectReferenceValue = clock;
            serialized.FindProperty("matching").objectReferenceValue = matching;
            serialized.FindProperty("sessionCoordinator").objectReferenceValue = coordinator;
            serialized.FindProperty("drumFeedbackSource").objectReferenceValue = feedbackSource;
            serialized.FindProperty("arcadeNeonBackground").objectReferenceValue = RequireAsset<Texture2D>(ArcadeBackgroundPath);
            serialized.FindProperty("concertStageBackground").objectReferenceValue = RequireAsset<Texture2D>(ConcertBackgroundPath);
            serialized.FindProperty("precisionGridBackground").objectReferenceValue = RequireAsset<Texture2D>(PrecisionBackgroundPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var coordinatorSerialized = new SerializedObject(coordinator);
            coordinatorSerialized.FindProperty("songClock").objectReferenceValue = clock;
            coordinatorSerialized.FindProperty("chartTimeline").objectReferenceValue = timeline;
            coordinatorSerialized.FindProperty("firstPulseChart").objectReferenceValue = RequireAsset<TextAsset>(FirstPulseChartPath);
            coordinatorSerialized.FindProperty("backbeatChart").objectReferenceValue = RequireAsset<TextAsset>(BackbeatChartPath);
            coordinatorSerialized.FindProperty("timekeeperChart").objectReferenceValue = RequireAsset<TextAsset>(TimekeeperChartPath);
            coordinatorSerialized.FindProperty("firstGrooveChart").objectReferenceValue = RequireAsset<TextAsset>(FirstGrooveChartPath);
            coordinatorSerialized.ApplyModifiedPropertiesWithoutUndo();

            var timelineSerialized = new SerializedObject(timeline);
            timelineSerialized.FindProperty("chartAsset").objectReferenceValue = RequireAsset<TextAsset>(DemoChartPath);
            timelineSerialized.FindProperty("lookAheadSeconds").doubleValue = 4.0;
            timelineSerialized.ApplyModifiedPropertiesWithoutUndo();

            var clockSerialized = new SerializedObject(clock);
            clockSerialized.FindProperty("bars").intValue = 8;
            clockSerialized.FindProperty("countInBeats").intValue = 4;
            clockSerialized.FindProperty("leadInSeconds").doubleValue = 2.0;
            clockSerialized.FindProperty("useDemoSong").boolValue = true;
            clockSerialized.ApplyModifiedPropertiesWithoutUndo();

            CoreMidiGameplayInput midi = keyboard.GetComponent<CoreMidiGameplayInput>();
            if (midi == null) midi = keyboard.gameObject.AddComponent<CoreMidiGameplayInput>();
            var midiSerialized = new SerializedObject(midi);
            midiSerialized.FindProperty("songClock").objectReferenceValue = clock;
            midiSerialized.FindProperty("genericProfile").objectReferenceValue = RequireAsset<TextAsset>(GenericProfilePath);
            midiSerialized.ApplyModifiedPropertiesWithoutUndo();

            serialized.FindProperty("midiInput").objectReferenceValue = midi;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CompositeDrumInput composite = keyboard.GetComponent<CompositeDrumInput>();
            if (composite == null) composite = keyboard.gameObject.AddComponent<CompositeDrumInput>();
            var compositeSerialized = new SerializedObject(composite);
            compositeSerialized.FindProperty("keyboardSource").objectReferenceValue = keyboard;
            compositeSerialized.FindProperty("midiSource").objectReferenceValue = midi;
            compositeSerialized.ApplyModifiedPropertiesWithoutUndo();

            var matchingSerialized = new SerializedObject(matching);
            matchingSerialized.FindProperty("drumInputSource").objectReferenceValue = composite;
            matchingSerialized.ApplyModifiedPropertiesWithoutUndo();

            SceneManager.MoveGameObjectToScene(root, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GameplayPrototype highway UI rebuilt.");
        }

        private static void DestroyExistingRoot()
        {
            GameObject existing = GameObject.Find("GameplayHighwayUI");
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new System.InvalidOperationException($"Required gameplay asset is missing: {path}");
            return asset;
        }
    }
}
