using HitTheKit.Core;
using HitTheKit.Unity.Input;
using HitTheKit.Unity.Matching;
using UnityEngine;

namespace HitTheKit.Unity.Visuals
{
    public sealed class HitResultVisualPresenter : MonoBehaviour
    {
        [SerializeField] private HitMatchingPrototype matching;
        [SerializeField] private DrumPadVisual kickVisual;
        [SerializeField] private DrumPadVisual snareVisual;
        [SerializeField] private DrumPadVisual hiHatVisual;
        [SerializeField] private double feedbackDurationSeconds = 0.12;

        private HitMatchingPrototype subscribedMatching;
        private bool invalidConfigurationLogged;

        public HitMatchingPrototype Matching => matching;
        public DrumPadVisual KickVisual => kickVisual;
        public DrumPadVisual SnareVisual => snareVisual;
        public DrumPadVisual HiHatVisual => hiHatVisual;
        public double FeedbackDurationSeconds => feedbackDurationSeconds;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            if (!HasValidReferences() || !IsFinite(feedbackDurationSeconds) || feedbackDurationSeconds <= 0)
            {
                LogInvalidConfigurationOnce();
                enabled = false;
                return;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleInputProcessed(DrumInputEvent input, HitResult result)
        {
            DrumPadVisual visual = GetVisual(input.Pad);
            if (visual == null) return;
            if (result != null && result.Grade == HitGrade.Miss) return;

            visual.ShowHitFeedback(GetColor(result), feedbackDurationSeconds);
        }

        private DrumPadVisual GetVisual(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return kickVisual;
                case DrumPad.Snare: return snareVisual;
                case DrumPad.HiHat: return hiHatVisual;
                default: return null;
            }
        }

        private static Color GetColor(HitResult result)
        {
            if (result == null) return new Color(0.55f, 0.12f, 0.12f, 1f);
            switch (result.Grade)
            {
                case HitGrade.Perfect: return new Color(0.45f, 1f, 0.55f, 1f);
                case HitGrade.Good: return new Color(0.35f, 0.8f, 1f, 1f);
                case HitGrade.Early: return new Color(1f, 0.5f, 0.12f, 1f);
                case HitGrade.Late: return new Color(0.7f, 0.35f, 1f, 1f);
                default: return new Color(0.55f, 0.12f, 0.12f, 1f);
            }
        }

        private bool HasValidReferences()
        {
            return matching != null &&
                   kickVisual != null && kickVisual.Pad == DrumPad.Kick &&
                   snareVisual != null && snareVisual.Pad == DrumPad.Snare &&
                   hiHatVisual != null && hiHatVisual.Pad == DrumPad.HiHat;
        }

        private void Subscribe()
        {
            if (matching == null || matching == subscribedMatching) return;
            Unsubscribe();
            matching.InputProcessed += HandleInputProcessed;
            subscribedMatching = matching;
        }

        private void Unsubscribe()
        {
            if (subscribedMatching == null) return;
            subscribedMatching.InputProcessed -= HandleInputProcessed;
            subscribedMatching = null;
        }

        private void LogInvalidConfigurationOnce()
        {
            if (invalidConfigurationLogged) return;
            invalidConfigurationLogged = true;
            Debug.LogError("Hit feedback requires matching and three correctly mapped pad visuals.", this);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
