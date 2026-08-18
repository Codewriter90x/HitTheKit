using System;
using System.Reflection;
using HitTheKit.Unity.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace HitTheKit.Unity.Tests
{
    public sealed class DrumPadVisualTests
    {
        private static readonly Color InactiveColor = new Color(0.1f, 0.2f, 0.3f, 1f);
        private static readonly Color ActiveColor = new Color(0.9f, 0.6f, 0.2f, 1f);

        [Test]
        public void Default_state_is_inactive()
        {
            VisualFixture fixture = CreateFixture();
            try
            {
                Assert.That(fixture.Visual.CurrentState.IsActive, Is.False);
                Assert.That(fixture.Visual.CurrentState.Intensity, Is.Zero);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(-1f)]
        [TestCase(2f)]
        public void Applies_clamped_color_emission_and_scale(float requestedIntensity)
        {
            VisualFixture fixture = CreateFixture();
            try
            {
                fixture.Visual.ApplyState(new PadVisualState(null, requestedIntensity));

                float intensity = Mathf.Clamp01(requestedIntensity);
                var block = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(block);
                Color expectedColor = Color.Lerp(InactiveColor, ActiveColor, intensity);
                Color actualColor = block.GetColor(Shader.PropertyToID("_Color"));
                Color actualBaseColor = block.GetColor(Shader.PropertyToID("_BaseColor"));
                Color emission = block.GetColor(Shader.PropertyToID("_EmissionColor"));

                AssertColor(actualColor, expectedColor);
                AssertColor(actualBaseColor, expectedColor);
                AssertColor(emission, ActiveColor * (1.5f * intensity), ignoreAlpha: true);
                Assert.That(fixture.Visual.CurrentState.Intensity, Is.EqualTo(intensity));
                Assert.That(
                    fixture.GameObject.transform.localScale,
                    Is.EqualTo(Vector3.one * Mathf.Lerp(1f, 1.08f, intensity)));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Rejects_non_finite_intensity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PadVisualState(null, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PadVisualState(null, float.PositiveInfinity));
        }

        [Test]
        public void Rejects_missing_renderer_when_applying_state()
        {
            var gameObject = new GameObject("Missing renderer visual");
            try
            {
                DrumPadVisual visual = gameObject.AddComponent<DrumPadVisual>();
                Assert.Throws<InvalidOperationException>(() => visual.ApplyState(PadVisualState.Inactive));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Repeated_updates_use_property_block_without_instantiating_material()
        {
            VisualFixture fixture = CreateFixture();
            try
            {
                Material sharedBefore = fixture.Renderer.sharedMaterial;

                fixture.Visual.ApplyState(new PadVisualState(null, 0.25f));
                fixture.Visual.ApplyState(new PadVisualState(null, 0.75f));

                Assert.That(fixture.Renderer.sharedMaterial, Is.SameAs(sharedBefore));
                Assert.That(fixture.Renderer.HasPropertyBlock(), Is.True);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Repeated_hit_feedback_preserves_timeline_scale_and_shared_material()
        {
            VisualFixture fixture = CreateFixture();
            try
            {
                fixture.Visual.ApplyState(new PadVisualState(null, 0.5f));
                Vector3 expectedScale = Vector3.one * Mathf.Lerp(1f, 1.08f, 0.5f);
                Material sharedBefore = fixture.Renderer.sharedMaterial;

                for (int index = 0; index < 100; index++)
                {
                    fixture.Visual.ShowHitFeedback(Color.green, 0.12);
                }

                var block = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(block);
                AssertColor(block.GetColor(Shader.PropertyToID("_Color")), Color.green);
                Assert.That(fixture.GameObject.transform.localScale, Is.EqualTo(expectedScale));
                Assert.That(fixture.Visual.CurrentState.Intensity, Is.EqualTo(0.5f));
                Assert.That(fixture.Renderer.sharedMaterial, Is.SameAs(sharedBefore));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static VisualFixture CreateFixture()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var gameObject = new GameObject("Drum pad visual test");
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DrumPadVisual visual = gameObject.AddComponent<DrumPadVisual>();
            SetField(visual, "targetRenderer", renderer);
            SetField(visual, "inactiveColor", InactiveColor);
            SetField(visual, "activeColor", ActiveColor);
            SetField(visual, "maximumEmission", 1.5f);
            SetField(visual, "inactiveScale", 1f);
            SetField(visual, "activeScale", 1.08f);
            return new VisualFixture(gameObject, renderer, visual, material);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void AssertColor(Color actual, Color expected, bool ignoreAlpha = false)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001));
            if (!ignoreAlpha)
            {
                Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001));
            }
        }

        private sealed class VisualFixture : IDisposable
        {
            public VisualFixture(
                GameObject gameObject,
                MeshRenderer renderer,
                DrumPadVisual visual,
                Material material)
            {
                GameObject = gameObject;
                Renderer = renderer;
                Visual = visual;
                Material = material;
            }

            public GameObject GameObject { get; }
            public MeshRenderer Renderer { get; }
            public DrumPadVisual Visual { get; }
            public Material Material { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(GameObject);
                UnityEngine.Object.DestroyImmediate(Material);
            }
        }
    }
}
