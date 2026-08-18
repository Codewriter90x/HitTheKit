using System;
using HitTheKit.Core;
using UnityEngine;

namespace HitTheKit.Unity.Visuals
{
    public sealed class DrumPadVisual : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private DrumPad pad;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 0.35f, 0.08f, 1f);
        [SerializeField] private float maximumEmission = 1.5f;
        [SerializeField] private float inactiveScale = 1f;
        [SerializeField] private float activeScale = 1.08f;

        private MaterialPropertyBlock propertyBlock;
        private readonly PadHitFeedbackState feedback = new PadHitFeedbackState();
        private Vector3 baseLocalScale;
        private bool initialized;
        private PadVisualState timelineState;

        public DrumPad Pad => pad;
        public Renderer TargetRenderer => targetRenderer;
        public PadVisualState CurrentState { get; private set; }
        public bool IsHitFeedbackActive => feedback.IsActive;
        public Color HitFeedbackColor => feedback.Color;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (targetRenderer == null)
            {
                Debug.LogError("A target Renderer is required for a drum pad visual.", this);
                enabled = false;
                return;
            }

            ApplyState(PadVisualState.Inactive);
        }

        private void Update()
        {
            if (!feedback.IsActive) return;
            feedback.Advance(Time.unscaledDeltaTime);
            RenderCurrentState();
        }

        public void ApplyState(PadVisualState state)
        {
            if (float.IsNaN(state.Intensity) || float.IsInfinity(state.Intensity))
            {
                throw new ArgumentOutOfRangeException(nameof(state), "Visual intensity must be finite.");
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                throw new InvalidOperationException("A target Renderer is required for a drum pad visual.");
            }

            Initialize();

            float intensity = Mathf.Clamp01(state.Intensity);
            timelineState = new PadVisualState(state.NextNote, intensity);
            CurrentState = timelineState;
            RenderCurrentState();
        }

        public void ShowHitFeedback(Color color, double durationSeconds)
        {
            EnsureRenderer();
            Initialize();
            feedback.Begin(color, durationSeconds);
            RenderCurrentState();
        }

        private void RenderCurrentState()
        {
            float intensity = Mathf.Clamp01(timelineState.Intensity);
            Color color = feedback.IsActive
                ? feedback.Color
                : Color.Lerp(inactiveColor, activeColor, intensity);
            Color emission = feedback.IsActive
                ? feedback.Color * maximumEmission
                : activeColor * (maximumEmission * intensity);
            emission.a = 1f;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorProperty, color);
            propertyBlock.SetColor(BaseColorProperty, color);
            propertyBlock.SetColor(EmissionColorProperty, emission);
            targetRenderer.SetPropertyBlock(propertyBlock);

            float scale = Mathf.Lerp(inactiveScale, activeScale, intensity);
            transform.localScale = baseLocalScale * scale;
        }

        private void EnsureRenderer()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                throw new InvalidOperationException("A target Renderer is required for a drum pad visual.");
            }
        }

        private void Initialize()
        {
            if (initialized) return;

            propertyBlock = new MaterialPropertyBlock();
            baseLocalScale = transform.localScale;
            initialized = true;
        }
    }
}
