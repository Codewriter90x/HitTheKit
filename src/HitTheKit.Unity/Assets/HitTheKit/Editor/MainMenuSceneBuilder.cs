using System;
using HitTheKit.Unity.MainMenu;
using HitTheKit.Unity.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/HitTheKit/Scenes/MainMenuPrototype.unity";
        private const string GameplayPath = "Assets/HitTheKit/Scenes/GameplayPrototype.unity";
        private const string DeviceSetupPath = "Assets/HitTheKit/Scenes/DeviceSetupPrototype.unity";
        private const string UxmlPath = "Assets/HitTheKit/UI/MainMenu/MainMenu.uxml";
        private const string BackgroundPath = "Assets/HitTheKit/UI/MainMenu/Backgrounds/stage-command.png";
        private const string StageModelPath = "Assets/HitTheKit/Visuals/Models/HitTheKit-MainMenuStage.fbx";
        private const string StageMaterialFolder = "Assets/HitTheKit/Visuals/Materials/MainMenuStage";
        private const string PanelTemplatePath = "Assets/HitTheKit/UI/DeviceSetup/DeviceSetupPanelSettings.asset";
        private const string MainMenuPanelPath = "Assets/HitTheKit/UI/MainMenu/MainMenuPanelSettings.asset";
        private const string GenericProfilePath = "Assets/HitTheKit/Fixtures/DeviceProfiles/generic-gm-drums-v1.json";

        [MenuItem("HitTheKit/Main Menu/Rebuild Stage Command")]
        public static void Build()
        {
            VisualTreeAsset visualTree = RequireAsset<VisualTreeAsset>(UxmlPath);
            Texture2D background = RequireAsset<Texture2D>(BackgroundPath);
            PanelSettings panelSettings = EnsureMainMenuPanelSettings(RequireAsset<PanelSettings>(PanelTemplatePath));
            GameObject stageModel = RequireAsset<GameObject>(StageModelPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.20f, 0.31f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.085f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.008f, 0.016f, 0.028f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.006f, 0.022f, 0.045f);
            RenderSettings.fogDensity = 0.011f;

            var stageRoot = new GameObject("Main Menu Stage Environment");
            MainMenuStageEnvironment stageEnvironment = stageRoot.AddComponent<MainMenuStageEnvironment>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(stageRoot.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.001f, 0.006f, 0.018f, 1f);
            camera.fieldOfView = 56f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 70f;
            camera.allowHDR = true;
            cameraObject.transform.localPosition = new Vector3(0f, 5.1f, 11.5f);
            cameraObject.transform.LookAt(new Vector3(2.8f, 1.30f, -4.8f));
            cameraObject.AddComponent<AudioListener>();

            GameObject modelRoot = (GameObject)PrefabUtility.InstantiatePrefab(stageModel, scene);
            modelRoot.name = "MainMenuStageModel";
            modelRoot.transform.SetParent(stageRoot.transform, false);
            modelRoot.transform.localPosition = Vector3.zero;
            AssignStageMaterials(modelRoot);
            Bounds stageBounds = CalculateRendererBounds(modelRoot);
            Renderer[] stageRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(camera);
            int activeRenderers = 0;
            int intersectingRenderers = 0;
            for (int index = 0; index < stageRenderers.Length; index++)
            {
                if (stageRenderers[index].enabled && stageRenderers[index].gameObject.activeInHierarchy) activeRenderers++;
                if (GeometryUtility.TestPlanesAABB(frustum, stageRenderers[index].bounds)) intersectingRenderers++;
            }
            Debug.Log($"Main-menu stage bounds center={stageBounds.center} size={stageBounds.size} " +
                      $"viewportCenter={camera.WorldToViewportPoint(stageBounds.center)} " +
                      $"activeRenderers={activeRenderers}/{stageRenderers.Length} " +
                      $"frustumRenderers={intersectingRenderers}");

            Light keyLight = CreateDirectionalLight(
                "Stage Key Light",
                stageRoot.transform,
                new Vector3(44f, -24f, 0f),
                new Color(0.25f, 0.76f, 1f),
                2.2f,
                LightShadows.Soft);
            Light rimLight = CreateDirectionalLight(
                "Stage Rim Light",
                stageRoot.transform,
                new Vector3(52f, 152f, 0f),
                new Color(0.84f, 0.11f, 0.67f),
                2.8f,
                LightShadows.None);
            Light[] movingSpotlights = CreateMovingSpotlights(stageRoot.transform);
            Light[] audienceLights = CreateAudienceLights(stageRoot.transform);
            ParticleSystem atmosphere = CreateAtmosphere(stageRoot.transform);

            var environmentSerialized = new SerializedObject(stageEnvironment);
            environmentSerialized.FindProperty("stageCamera").objectReferenceValue = camera;
            environmentSerialized.FindProperty("modelRoot").objectReferenceValue = modelRoot.transform;
            environmentSerialized.FindProperty("keyLight").objectReferenceValue = keyLight;
            environmentSerialized.FindProperty("rimLight").objectReferenceValue = rimLight;
            SetObjectArray(environmentSerialized.FindProperty("movingSpotlights"), movingSpotlights);
            SetObjectArray(environmentSerialized.FindProperty("audienceLights"), audienceLights);
            environmentSerialized.FindProperty("atmosphere").objectReferenceValue = atmosphere;
            environmentSerialized.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("EventSystem");

            var uiRoot = new GameObject("Main Menu UI");
            UIDocument document = uiRoot.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 20;
            MainMenuController controller = uiRoot.AddComponent<MainMenuController>();
            controller.Configure(panelSettings, background, stageEnvironment);
            CoreMidiGameplayInput midiInput = uiRoot.AddComponent<CoreMidiGameplayInput>();
            var midiSerialized = new SerializedObject(midiInput);
            midiSerialized.FindProperty("genericProfile").objectReferenceValue = RequireAsset<TextAsset>(GenericProfilePath);
            midiSerialized.ApplyModifiedPropertiesWithoutUndo();
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("menuMidiInput").objectReferenceValue = midiInput;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            SceneManager.MoveGameObjectToScene(uiRoot, scene);
            SceneManager.MoveGameObjectToScene(stageRoot, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save Main Menu scene.");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameplayPath, true),
                new EditorBuildSettingsScene(DeviceSetupPath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Main Menu Stage Command scene and build settings generated.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"Required main menu asset is missing: {path}");
            return asset;
        }

        private static void AssignStageMaterials(GameObject modelRoot)
        {
            EnsureFolder(StageMaterialFolder);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new InvalidOperationException("The URP Lit shader is required for the main-menu stage.");

            Material stage = CreateMaterial("Stage", lit, new Color(0.035f, 0.075f, 0.12f), 0.45f, 0.30f);
            Material fixture = CreateMaterial("Fixture", lit, new Color(0.025f, 0.035f, 0.055f), 0.72f, 0.46f);
            Material shell = CreateMaterial("DrumShell", lit, new Color(0.025f, 0.040f, 0.055f), 0.58f, 0.76f);
            Material drumHead = CreateMaterial("DrumHead", lit, new Color(0.17f, 0.19f, 0.22f), 0.03f, 0.54f);
            Material chrome = CreateMaterial("Chrome", lit, new Color(0.62f, 0.70f, 0.77f), 1.0f, 0.88f);
            Material cymbal = CreateMaterial("Cymbal", lit, new Color(0.52f, 0.25f, 0.045f), 0.92f, 0.62f);
            Material crowd = CreateMaterial("Crowd", lit, new Color(0.020f, 0.052f, 0.085f), 0.0f, 0.86f);
            Material[] audienceSkins =
            {
                CreateMaterial("AudienceSkin0", lit, new Color(0.56f, 0.31f, 0.19f), 0.0f, 0.28f),
                CreateMaterial("AudienceSkin1", lit, new Color(0.35f, 0.17f, 0.095f), 0.0f, 0.24f),
                CreateMaterial("AudienceSkin2", lit, new Color(0.14f, 0.060f, 0.032f), 0.0f, 0.20f),
                CreateMaterial("AudienceSkin3", lit, new Color(0.72f, 0.48f, 0.31f), 0.0f, 0.32f)
            };
            Material[] audienceClothes =
            {
                CreateMaterial("AudienceClothes0", lit, new Color(0.012f, 0.030f, 0.055f), 0.0f, 0.22f),
                CreateMaterial("AudienceClothes1", lit, new Color(0.055f, 0.012f, 0.055f), 0.0f, 0.24f),
                CreateMaterial("AudienceClothes2", lit, new Color(0.065f, 0.027f, 0.010f), 0.0f, 0.18f),
                CreateMaterial("AudienceClothes3", lit, new Color(0.018f, 0.055f, 0.045f), 0.0f, 0.20f)
            };
            Material[] audienceHair =
            {
                CreateMaterial("AudienceHair0", lit, new Color(0.008f, 0.006f, 0.005f), 0.0f, 0.14f),
                CreateMaterial("AudienceHair1", lit, new Color(0.055f, 0.020f, 0.008f), 0.0f, 0.18f),
                CreateMaterial("AudienceHair2", lit, new Color(0.14f, 0.075f, 0.022f), 0.0f, 0.22f)
            };
            Material audienceShoes = CreateMaterial("AudienceShoes", lit, new Color(0.008f, 0.010f, 0.014f), 0.0f, 0.34f);
            Material audienceDetail = CreateMaterial("AudienceDetail", lit, new Color(0.003f, 0.004f, 0.006f), 0.0f, 0.18f);
            Material accent = CreateMaterial("Accent", lit, new Color(0.02f, 0.66f, 1f), 0.42f, 0.17f, true);

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.sharedMaterial = MaterialFor(
                    renderer.name,
                    stage,
                    fixture,
                    shell,
                    drumHead,
                    chrome,
                    cymbal,
                    crowd,
                    audienceSkins,
                    audienceClothes,
                    audienceHair,
                    audienceShoes,
                    audienceDetail,
                    accent);
                renderer.shadowCastingMode = renderer.name.StartsWith("Audience_", StringComparison.Ordinal)
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = !renderer.name.StartsWith("Accent_", StringComparison.Ordinal);
            }
        }

        private static Material MaterialFor(
            string name,
            Material stage,
            Material fixture,
            Material shell,
            Material drumHead,
            Material chrome,
            Material cymbal,
            Material crowd,
            Material[] audienceSkins,
            Material[] audienceClothes,
            Material[] audienceHair,
            Material audienceShoes,
            Material audienceDetail,
            Material accent)
        {
            if (name.StartsWith("DrumShell_", StringComparison.Ordinal)) return shell;
            if (name.StartsWith("DrumHead_", StringComparison.Ordinal)) return drumHead;
            if (name.StartsWith("DrumInterior_", StringComparison.Ordinal)) return drumHead;
            if (name.StartsWith("Hardware_", StringComparison.Ordinal)) return fixture;
            if (name.StartsWith("Chrome_", StringComparison.Ordinal)) return chrome;
            if (name.StartsWith("Cymbal_", StringComparison.Ordinal)) return cymbal;
            for (int index = 0; index < audienceSkins.Length; index++)
                if (name.StartsWith($"Audience_Skin{index}_", StringComparison.Ordinal)) return audienceSkins[index];
            for (int index = 0; index < audienceClothes.Length; index++)
                if (name.StartsWith($"Audience_Clothes{index}_", StringComparison.Ordinal)) return audienceClothes[index];
            for (int index = 0; index < audienceHair.Length; index++)
                if (name.StartsWith($"Audience_Hair{index}_", StringComparison.Ordinal)) return audienceHair[index];
            if (name.StartsWith("Audience_Shoes_", StringComparison.Ordinal)) return audienceShoes;
            if (name.StartsWith("Audience_Detail_", StringComparison.Ordinal)) return audienceDetail;
            if (name.StartsWith("Audience_Accent_", StringComparison.Ordinal)) return accent;
            if (name.StartsWith("Audience_", StringComparison.Ordinal)) return crowd;
            if (name.StartsWith("Accent_", StringComparison.Ordinal)) return accent;
            if (name.StartsWith("Fixture_", StringComparison.Ordinal)) return fixture;
            return stage;
        }

        private static Material CreateMaterial(
            string name,
            Shader shader,
            Color color,
            float metallic,
            float smoothness,
            bool emission = false)
        {
            string path = $"{StageMaterialFolder}/{name}.mat";
            Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value == null)
            {
                value = new Material(shader) { name = $"MainMenuStage{name}" };
                AssetDatabase.CreateAsset(value, path);
            }
            value.shader = shader;
            value.SetColor("_BaseColor", color);
            value.SetFloat("_Metallic", metallic);
            value.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                value.EnableKeyword("_EMISSION");
                value.SetColor("_EmissionColor", color * 4.5f);
                value.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                value.DisableKeyword("_EMISSION");
                value.SetColor("_EmissionColor", Color.black);
                value.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(value);
            return value;
        }

        private static Light CreateDirectionalLight(
            string name,
            Transform parent,
            Vector3 euler,
            Color color,
            float intensity,
            LightShadows shadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localRotation = Quaternion.Euler(euler);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            return light;
        }

        private static Light[] CreateMovingSpotlights(Transform parent)
        {
            float[] positions = { -5.8f, -3.6f, -1.2f, 1.2f, 3.6f, 5.8f };
            var lights = new Light[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                var lightObject = new GameObject($"Moving Spotlight {index + 1:00}");
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.localPosition = new Vector3(positions[index], 6.2f, -2.2f);
                lightObject.transform.LookAt(new Vector3(positions[index] * 0.42f, 0.7f, -8.5f - index % 2 * 1.8f));
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(0.02f, 0.66f, 1f);
                light.intensity = 120f;
                light.range = 27f;
                light.spotAngle = 31f;
                light.innerSpotAngle = 18f;
                light.shadows = index == 1 || index == 4 ? LightShadows.Soft : LightShadows.None;
                lights[index] = light;
            }
            return lights;
        }

        private static Light[] CreateAudienceLights(Transform parent)
        {
            Vector3[] positions =
            {
                new Vector3(-5.5f, 1.7f, -7.0f),
                new Vector3(0f, 1.35f, -9.2f),
                new Vector3(5.5f, 1.7f, -7.0f)
            };
            var lights = new Light[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                var lightObject = new GameObject($"Audience Fill {index + 1:00}");
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.localPosition = positions[index];
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.06f, 0.34f, 0.62f);
                light.intensity = 18f;
                light.range = 8.5f;
                light.shadows = LightShadows.None;
                lights[index] = light;
            }
            return lights;
        }

        private static ParticleSystem CreateAtmosphere(Transform parent)
        {
            var atmosphereObject = new GameObject("Stage Atmosphere");
            atmosphereObject.transform.SetParent(parent, false);
            atmosphereObject.transform.localPosition = new Vector3(0f, 1.8f, -5.2f);
            ParticleSystem system = atmosphereObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.065f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 22f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 5f, 18f);
            ParticleSystemRenderer renderer = atmosphereObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = -2f;
            Material particleMaterial = CreateAtmosphereMaterial();
            renderer.sharedMaterial = particleMaterial;
            return system;
        }

        private static Material CreateAtmosphereMaterial()
        {
            const string path = StageMaterialFolder + "/Atmosphere.mat";
            Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) throw new InvalidOperationException("The URP particle shader is required for stage atmosphere.");
            if (value == null)
            {
                value = new Material(shader) { name = "MainMenuStageAtmosphere" };
                AssetDatabase.CreateAsset(value, path);
            }
            value.shader = shader;
            value.SetColor("_BaseColor", new Color(0.04f, 0.43f, 0.78f, 0.12f));
            EditorUtility.SetDirty(value);
            return value;
        }

        private static void SetObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static PanelSettings EnsureMainMenuPanelSettings(PanelSettings template)
        {
            PanelSettings value = AssetDatabase.LoadAssetAtPath<PanelSettings>(MainMenuPanelPath);
            if (value == null)
            {
                value = UnityEngine.Object.Instantiate(template);
                value.name = "MainMenuPanelSettings";
                AssetDatabase.CreateAsset(value, MainMenuPanelPath);
            }

            var serialized = new SerializedObject(value);
            serialized.FindProperty("m_ClearColor").boolValue = false;
            serialized.FindProperty("m_ClearDepthStencil").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(value);
            return value;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("The main-menu stage model contains no renderers.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}
