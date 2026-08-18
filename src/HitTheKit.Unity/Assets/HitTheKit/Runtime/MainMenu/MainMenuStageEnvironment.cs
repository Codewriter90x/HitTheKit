using System;
using System.Collections.Generic;
using HitTheKit.Unity.Gameplay;
using UnityEngine;

namespace HitTheKit.Unity.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuStageEnvironment : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Light keyLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Light[] movingSpotlights = Array.Empty<Light>();
        [SerializeField] private Light[] audienceLights = Array.Empty<Light>();
        [SerializeField] private ParticleSystem atmosphere;

        private readonly List<Transform> crowdRows = new List<Transform>();
        private readonly List<Transform> cymbalRoots = new List<Transform>();
        private readonly List<Renderer> stageRenderers = new List<Renderer>();
        private Quaternion[] crowdBaseRotations = Array.Empty<Quaternion>();
        private Quaternion[] spotlightBaseRotations = Array.Empty<Quaternion>();
        private float[] spotlightBaseIntensities = Array.Empty<float>();
        private int audienceRendererCount;
        private int audienceSkinRendererCount;
        private int audienceClothingRendererCount;
        private Vector3 cameraBasePosition;
        private Quaternion cameraBaseRotation;
        private MaterialPropertyBlock propertyBlock;
        private float selectionPulse;
        private int selectionPulseDirection = 1;
        private bool reducedMotion;
        private bool initialized;

        public bool IsReady => initialized && stageCamera != null && modelRoot != null;
        public int CrowdRowCount => crowdRows.Count;
        public int AudienceRendererCount => audienceRendererCount;
        public int AudienceSkinRendererCount => audienceSkinRendererCount;
        public int AudienceClothingRendererCount => audienceClothingRendererCount;
        public int MovingSpotlightCount => movingSpotlights?.Length ?? 0;
        public int ModelRendererCount => stageRenderers.Count;
        public GameplayPresentationTheme Theme { get; private set; } = GameplayPresentationTheme.ArcadeNeon;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            ApplyTheme(GameplaySettingsRuntime.Current.Theme);
        }

        private void Update()
        {
            if (!IsReady) return;
            float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            selectionPulse = Mathf.MoveTowards(selectionPulse, 0f, delta * 1.55f);
            AnimateEnvironment(Time.unscaledTime, reducedMotion ? 0f : 1f);
        }

        public void ApplyTheme(GameplayPresentationTheme theme)
        {
            if (!Enum.IsDefined(typeof(GameplayPresentationTheme), theme))
                throw new ArgumentOutOfRangeException(nameof(theme));
            Initialize();
            Theme = theme;
            MainMenuStagePalette palette = MainMenuStagePalette.For(theme);

            if (stageCamera != null) stageCamera.backgroundColor = palette.Background;
            ApplyLight(keyLight, palette.Key, palette.KeyIntensity);
            ApplyLight(rimLight, palette.Rim, palette.RimIntensity);
            ApplyLights(movingSpotlights, palette.Accent, palette.SpotIntensity);
            ApplyLights(audienceLights, palette.Audience, palette.AudienceIntensity);
            for (int index = 0; index < spotlightBaseIntensities.Length; index++)
                spotlightBaseIntensities[index] = movingSpotlights[index] == null ? 0f : movingSpotlights[index].intensity;

            for (int index = 0; index < stageRenderers.Count; index++)
            {
                Renderer renderer = stageRenderers[index];
                if (renderer == null) continue;
                Color baseColor = ColorFor(renderer.name, palette);
                Color emission = EmissionFor(renderer.name, palette);
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, baseColor);
                propertyBlock.SetColor(LegacyColorId, baseColor);
                propertyBlock.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(propertyBlock);
            }

            if (atmosphere != null)
            {
                ParticleSystem.MainModule main = atmosphere.main;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(palette.Atmosphere.r, palette.Atmosphere.g, palette.Atmosphere.b, 0.035f),
                    new Color(palette.Atmosphere.r, palette.Atmosphere.g, palette.Atmosphere.b, 0.16f));
                ParticleSystem.EmissionModule emission = atmosphere.emission;
                emission.rateOverTime = palette.AtmosphereRate;
            }
        }

        public void PulseDestination(MainMenuDestination destination)
        {
            if (!Enum.IsDefined(typeof(MainMenuDestination), destination))
                throw new ArgumentOutOfRangeException(nameof(destination));
            selectionPulseDirection = destination == MainMenuDestination.DeviceSetup ? -1 : 1;
            selectionPulse = destination == MainMenuDestination.Learn ? 0.72f : 1f;
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (value) RestoreAnimatedTransforms();
        }

        public void Initialize()
        {
            if (initialized) return;
            propertyBlock = new MaterialPropertyBlock();
            if (modelRoot != null)
            {
                Transform[] transforms = modelRoot.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    Transform candidate = transforms[index];
                    if (candidate.name.StartsWith("Audience_Row_", StringComparison.Ordinal)) crowdRows.Add(candidate);
                    if (candidate.name.StartsWith("Cymbal_", StringComparison.Ordinal) &&
                        candidate.name.EndsWith("_ROOT", StringComparison.Ordinal)) cymbalRoots.Add(candidate);
                }
                stageRenderers.AddRange(modelRoot.GetComponentsInChildren<Renderer>(true));
                for (int index = 0; index < stageRenderers.Count; index++)
                {
                    string rendererName = stageRenderers[index].name;
                    if (!rendererName.StartsWith("Audience_", StringComparison.Ordinal)) continue;
                    audienceRendererCount++;
                    if (rendererName.StartsWith("Audience_Skin", StringComparison.Ordinal)) audienceSkinRendererCount++;
                    if (rendererName.StartsWith("Audience_Clothes", StringComparison.Ordinal)) audienceClothingRendererCount++;
                }
            }

            crowdRows.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            cymbalRoots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            crowdBaseRotations = new Quaternion[crowdRows.Count];
            for (int index = 0; index < crowdRows.Count; index++) crowdBaseRotations[index] = crowdRows[index].localRotation;
            spotlightBaseRotations = new Quaternion[movingSpotlights?.Length ?? 0];
            spotlightBaseIntensities = new float[spotlightBaseRotations.Length];
            for (int index = 0; index < spotlightBaseRotations.Length; index++)
            {
                spotlightBaseRotations[index] = movingSpotlights[index] == null
                    ? Quaternion.identity
                    : movingSpotlights[index].transform.localRotation;
                spotlightBaseIntensities[index] = movingSpotlights[index] == null ? 0f : movingSpotlights[index].intensity;
            }
            if (stageCamera != null)
            {
                cameraBasePosition = stageCamera.transform.localPosition;
                cameraBaseRotation = stageCamera.transform.localRotation;
            }
            initialized = modelRoot != null && stageCamera != null;
        }

        private void AnimateEnvironment(float time, float motionScale)
        {
            float pulse = motionScale <= 0f ? 0f : selectionPulse * selectionPulse;
            for (int index = 0; index < spotlightBaseRotations.Length; index++)
            {
                Light spotlight = movingSpotlights[index];
                if (spotlight == null) continue;
                float phase = time * (0.20f + index * 0.011f) + index * 1.37f;
                float yaw = Mathf.Sin(phase) * (13f + index % 3 * 4f) * motionScale;
                float pitch = Mathf.Cos(phase * 0.73f) * 4.5f * motionScale;
                spotlight.transform.localRotation = spotlightBaseRotations[index] * Quaternion.Euler(pitch, yaw, 0f);
                spotlight.intensity = spotlightBaseIntensities[index] * (1f + pulse * 0.12f);
            }

            for (int index = 0; index < crowdRows.Count; index++)
            {
                float sway = Mathf.Sin(time * (0.52f + index * 0.025f) + index * 0.91f) *
                             (0.32f + index * 0.05f) * motionScale;
                crowdRows[index].localRotation = crowdBaseRotations[index] * Quaternion.Euler(0f, 0f, sway);
            }

            if (stageCamera != null)
            {
                float driftX = Mathf.Sin(time * 0.115f) * 0.075f * motionScale + pulse * 0.035f * selectionPulseDirection;
                float driftY = Mathf.Cos(time * 0.093f) * 0.035f * motionScale;
                stageCamera.transform.localPosition = cameraBasePosition + new Vector3(driftX, driftY, 0f);
                stageCamera.transform.localRotation = cameraBaseRotation * Quaternion.Euler(driftY * 0.45f, -driftX * 0.42f, 0f);
            }
        }

        private void RestoreAnimatedTransforms()
        {
            for (int index = 0; index < crowdRows.Count; index++) crowdRows[index].localRotation = crowdBaseRotations[index];
            for (int index = 0; index < spotlightBaseRotations.Length; index++)
                if (movingSpotlights[index] != null) movingSpotlights[index].transform.localRotation = spotlightBaseRotations[index];
            if (stageCamera != null)
            {
                stageCamera.transform.localPosition = cameraBasePosition;
                stageCamera.transform.localRotation = cameraBaseRotation;
            }
        }

        private static void ApplyLight(Light light, Color color, float intensity)
        {
            if (light == null) return;
            light.color = color;
            light.intensity = intensity;
        }

        private static void ApplyLights(IReadOnlyList<Light> lights, Color color, float intensity)
        {
            if (lights == null) return;
            for (int index = 0; index < lights.Count; index++) ApplyLight(lights[index], color, intensity);
        }

        private static Color ColorFor(string rendererName, MainMenuStagePalette palette)
        {
            if (rendererName.StartsWith("DrumShell_", StringComparison.Ordinal)) return palette.DrumShell;
            if (rendererName.StartsWith("DrumHead_", StringComparison.Ordinal)) return palette.DrumHead;
            if (rendererName.StartsWith("DrumInterior_", StringComparison.Ordinal)) return palette.DrumHead;
            if (rendererName.StartsWith("Hardware_", StringComparison.Ordinal)) return palette.Fixture;
            if (rendererName.StartsWith("Cymbal_", StringComparison.Ordinal)) return palette.Cymbal;
            if (rendererName.StartsWith("Chrome_", StringComparison.Ordinal)) return palette.Chrome;
            if (rendererName.StartsWith("Accent_", StringComparison.Ordinal)) return palette.Accent;
            if (rendererName.StartsWith("Audience_Skin0_", StringComparison.Ordinal)) return new Color(0.56f, 0.31f, 0.19f);
            if (rendererName.StartsWith("Audience_Skin1_", StringComparison.Ordinal)) return new Color(0.35f, 0.17f, 0.095f);
            if (rendererName.StartsWith("Audience_Skin2_", StringComparison.Ordinal)) return new Color(0.14f, 0.060f, 0.032f);
            if (rendererName.StartsWith("Audience_Skin3_", StringComparison.Ordinal)) return new Color(0.72f, 0.48f, 0.31f);
            if (rendererName.StartsWith("Audience_Hair0_", StringComparison.Ordinal)) return new Color(0.008f, 0.006f, 0.005f);
            if (rendererName.StartsWith("Audience_Hair1_", StringComparison.Ordinal)) return new Color(0.055f, 0.020f, 0.008f);
            if (rendererName.StartsWith("Audience_Hair2_", StringComparison.Ordinal)) return new Color(0.14f, 0.075f, 0.022f);
            if (rendererName.StartsWith("Audience_Shoes_", StringComparison.Ordinal)) return new Color(0.008f, 0.010f, 0.014f);
            if (rendererName.StartsWith("Audience_Detail_", StringComparison.Ordinal)) return new Color(0.003f, 0.004f, 0.006f);
            if (rendererName.StartsWith("Audience_Accent_", StringComparison.Ordinal)) return palette.Accent;
            if (rendererName.StartsWith("Audience_Clothes0_", StringComparison.Ordinal)) return palette.Crowd * 0.74f;
            if (rendererName.StartsWith("Audience_Clothes1_", StringComparison.Ordinal)) return Color.Lerp(palette.Crowd, palette.Rim, 0.12f);
            if (rendererName.StartsWith("Audience_Clothes2_", StringComparison.Ordinal)) return Color.Lerp(palette.Crowd, palette.Cymbal, 0.10f);
            if (rendererName.StartsWith("Audience_Clothes3_", StringComparison.Ordinal)) return Color.Lerp(palette.Crowd, palette.Accent, 0.10f);
            if (rendererName.StartsWith("Audience_", StringComparison.Ordinal)) return palette.Crowd;
            if (rendererName.StartsWith("Fixture_", StringComparison.Ordinal)) return palette.Fixture;
            return palette.Stage;
        }

        private static Color EmissionFor(string rendererName, MainMenuStagePalette palette)
        {
            if (rendererName.StartsWith("Accent_", StringComparison.Ordinal)) return palette.Accent * palette.EmissionMultiplier;
            if (rendererName.StartsWith("Fixture_", StringComparison.Ordinal)) return palette.Accent * 0.16f;
            if (rendererName.StartsWith("Audience_Accent_", StringComparison.Ordinal)) return palette.Accent * 2.4f;
            if (rendererName.StartsWith("Audience_", StringComparison.Ordinal)) return palette.Crowd * 0.08f;
            return Color.black;
        }
    }

    public readonly struct MainMenuStagePalette
    {
        private MainMenuStagePalette(
            Color background,
            Color stage,
            Color fixture,
            Color drumShell,
            Color drumHead,
            Color chrome,
            Color cymbal,
            Color crowd,
            Color accent,
            Color key,
            Color rim,
            Color audience,
            Color atmosphere,
            float keyIntensity,
            float rimIntensity,
            float spotIntensity,
            float audienceIntensity,
            float atmosphereRate,
            float emissionMultiplier)
        {
            Background = background;
            Stage = stage;
            Fixture = fixture;
            DrumShell = drumShell;
            DrumHead = drumHead;
            Chrome = chrome;
            Cymbal = cymbal;
            Crowd = crowd;
            Accent = accent;
            Key = key;
            Rim = rim;
            Audience = audience;
            Atmosphere = atmosphere;
            KeyIntensity = keyIntensity;
            RimIntensity = rimIntensity;
            SpotIntensity = spotIntensity;
            AudienceIntensity = audienceIntensity;
            AtmosphereRate = atmosphereRate;
            EmissionMultiplier = emissionMultiplier;
        }

        public Color Background { get; }
        public Color Stage { get; }
        public Color Fixture { get; }
        public Color DrumShell { get; }
        public Color DrumHead { get; }
        public Color Chrome { get; }
        public Color Cymbal { get; }
        public Color Crowd { get; }
        public Color Accent { get; }
        public Color Key { get; }
        public Color Rim { get; }
        public Color Audience { get; }
        public Color Atmosphere { get; }
        public float KeyIntensity { get; }
        public float RimIntensity { get; }
        public float SpotIntensity { get; }
        public float AudienceIntensity { get; }
        public float AtmosphereRate { get; }
        public float EmissionMultiplier { get; }

        public static MainMenuStagePalette For(GameplayPresentationTheme theme)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon:
                    return new MainMenuStagePalette(
                        new Color(0.001f, 0.006f, 0.018f),
                        new Color(0.035f, 0.075f, 0.12f),
                        new Color(0.028f, 0.072f, 0.13f),
                        new Color(0.022f, 0.052f, 0.075f),
                        new Color(0.17f, 0.20f, 0.23f),
                        new Color(0.62f, 0.70f, 0.77f),
                        new Color(0.52f, 0.25f, 0.045f),
                        new Color(0.020f, 0.052f, 0.085f),
                        new Color(0.02f, 0.66f, 1f),
                        new Color(0.72f, 0.86f, 1f),
                        new Color(0.66f, 0.16f, 0.48f),
                        new Color(0.06f, 0.34f, 0.62f),
                        new Color(0.04f, 0.43f, 0.78f),
                        2.1f, 2.2f, 92f, 16f, 18f, 4.2f);
                case GameplayPresentationTheme.ConcertStage:
                    return new MainMenuStagePalette(
                        new Color(0.012f, 0.006f, 0.003f),
                        new Color(0.11f, 0.052f, 0.025f),
                        new Color(0.13f, 0.065f, 0.028f),
                        new Color(0.105f, 0.028f, 0.018f),
                        new Color(0.22f, 0.17f, 0.14f),
                        new Color(0.70f, 0.62f, 0.53f),
                        new Color(0.58f, 0.30f, 0.055f),
                        new Color(0.075f, 0.038f, 0.022f),
                        new Color(1f, 0.28f, 0.035f),
                        new Color(1f, 0.72f, 0.50f),
                        new Color(0.82f, 0.19f, 0.055f),
                        new Color(0.75f, 0.20f, 0.035f),
                        new Color(0.72f, 0.20f, 0.035f),
                        2.5f, 2.7f, 108f, 18f, 21f, 4.5f);
                case GameplayPresentationTheme.PrecisionGrid:
                    return new MainMenuStagePalette(
                        new Color(0.001f, 0.008f, 0.012f),
                        new Color(0.028f, 0.092f, 0.095f),
                        new Color(0.032f, 0.105f, 0.11f),
                        new Color(0.022f, 0.080f, 0.070f),
                        new Color(0.15f, 0.21f, 0.20f),
                        new Color(0.54f, 0.71f, 0.68f),
                        new Color(0.50f, 0.34f, 0.075f),
                        new Color(0.020f, 0.075f, 0.072f),
                        new Color(0.10f, 1f, 0.72f),
                        new Color(0.70f, 1f, 0.90f),
                        new Color(0.08f, 0.56f, 0.47f),
                        new Color(0.06f, 0.42f, 0.38f),
                        new Color(0.05f, 0.56f, 0.48f),
                        1.9f, 2.0f, 82f, 14f, 13f, 3.5f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme));
            }
        }
    }
}
