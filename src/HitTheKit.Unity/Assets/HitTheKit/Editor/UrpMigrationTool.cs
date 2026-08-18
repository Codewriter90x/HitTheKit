using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HitTheKit.Unity.EditorTools
{
    public static class UrpMigrationTool
    {
        private const string RenderingFolder = "Assets/HitTheKit/Rendering";
        private const string RendererPath = RenderingFolder + "/HitTheKitUniversalRenderer.asset";
        private const string PipelinePath = RenderingFolder + "/HitTheKitUniversalRenderPipeline.asset";
        private const string DefaultVolumePath = RenderingFolder + "/DefaultVolumeProfile.asset";
        private const string GlobalSettingsPath = RenderingFolder + "/UniversalRenderPipelineGlobalSettings.asset";
        private const string GeneratedDefaultVolumePath = "Assets/DefaultVolumeProfile.asset";
        private const string GeneratedGlobalSettingsPath = "Assets/UniversalRenderPipelineGlobalSettings.asset";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";
        private const string DefaultPostProcessDataPath =
            UrpPackagePath + "/Runtime/Data/PostProcessData.asset";
        private const string PadMaterialPath = "Assets/HitTheKit/Visuals/Materials/PadBase.mat";

        public static void Migrate()
        {
            EnsureFolder(RenderingFolder);

            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "HitTheKitUniversalRenderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            PopulateRendererResources(renderer);

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "HitTheKitUniversalRenderPipeline";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            AssignPipelineEverywhere(pipeline);
            ConvertPadMaterial();
            OrganizeGeneratedAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"HitTheKit URP migration complete: {PipelinePath}");
        }

        private static void PopulateRendererResources(UniversalRendererData renderer)
        {
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
            if (renderer.postProcessData == null)
            {
                throw new InvalidOperationException(
                    $"Required URP post-processing data was not found: {DefaultPostProcessDataPath}");
            }

            ResourceReloader.ReloadAllNullIn(renderer, UrpPackagePath);
            EditorUtility.SetDirty(renderer);
        }

        private static void AssignPipelineEverywhere(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;

            int originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        private static void ConvertPadMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PadMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Required pad material not found: {PadMaterialPath}");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader is unavailable.");
            }

            Color baseColor = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.GetColor("_Color");
            Color emissionColor = material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor")
                : Color.black;

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetColor("_EmissionColor", emissionColor);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
        }

        private static void OrganizeGeneratedAssets()
        {
            MoveGeneratedAsset(GeneratedDefaultVolumePath, DefaultVolumePath);
            MoveGeneratedAsset(GeneratedGlobalSettingsPath, GlobalSettingsPath);
        }

        private static void MoveGeneratedAsset(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null ||
                AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"Could not move generated URP asset from {sourcePath} to {destinationPath}: {error}");
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
