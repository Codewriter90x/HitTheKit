using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HitTheKit.Unity.Tests
{
    public sealed class UrpMigrationTests
    {
        private const string PipelineTypeName =
            "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";
        private const string PipelinePath =
            "Assets/HitTheKit/Rendering/HitTheKitUniversalRenderPipeline.asset";
        private const string MaterialPath =
            "Assets/HitTheKit/Visuals/Materials/PadBase.mat";

        [Test]
        public void GraphicsAndEveryQualityLevelUseCanonicalUrpAsset()
        {
            RenderPipelineAsset expected = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            Assert.That(expected, Is.Not.Null);
            Assert.That(expected.GetType().FullName, Is.EqualTo(PipelineTypeName));
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(expected));

            int originalQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    Assert.That(
                        QualitySettings.renderPipeline,
                        Is.SameAs(expected),
                        $"Quality level '{QualitySettings.names[index]}' does not use the canonical URP asset.");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, false);
            }
        }

        [Test]
        public void PadMaterialUsesUrpLitShader()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True);
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(new Color(0.1f, 0.11f, 0.13f, 1f)));
        }
    }
}
