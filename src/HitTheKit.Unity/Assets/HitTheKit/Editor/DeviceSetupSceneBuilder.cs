using System;
using HitTheKit.Unity.DeviceSetup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.EditorTools
{
    public static class DeviceSetupSceneBuilder
    {
        private const string ScenePath = "Assets/HitTheKit/Scenes/DeviceSetupPrototype.unity";
        private const string UxmlPath = "Assets/HitTheKit/UI/DeviceSetup/DeviceSetup.uxml";
        private const string PanelPath = "Assets/HitTheKit/UI/DeviceSetup/DeviceSetupPanelSettings.asset";
        private const string MainMenuPath = "Assets/HitTheKit/Scenes/MainMenuPrototype.unity";
        private const string GameplayPath = "Assets/HitTheKit/Scenes/GameplayPrototype.unity";

        [MenuItem("HitTheKit/Build Device Setup Prototype")]
        public static void Build()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null) throw new InvalidOperationException("Device Setup UXML is missing or failed to import.");

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "DeviceSetupPanelSettings";
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1280, 720);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
                panelSettings.clearColor = true;
                panelSettings.colorClearValue = new Color(0.043f, 0.067f, 0.11f, 1f);
                AssetDatabase.CreateAsset(panelSettings, PanelPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.067f, 0.11f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var eventSystemObject = new GameObject("EventSystem");
            // UI Toolkit routes panel input without a uGUI EventSystem component. Keeping this
            // named scene root makes the input boundary explicit without adding the uGUI package.

            var uiRoot = new GameObject("Device Setup UI");
            UIDocument document = uiRoot.AddComponent<UIDocument>();
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 0;
            DeviceSetupController controller = uiRoot.AddComponent<DeviceSetupController>();
            controller.ConfigurePanelSettings(panelSettings);
            EditorUtility.SetDirty(controller);

            var simulatedRoot = new GameObject("Simulated Backends");
            simulatedRoot.AddComponent<SimulatedBackendRoot>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Failed to save Device Setup scene.");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GameplayPath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Device Setup prototype scene and build settings generated.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
