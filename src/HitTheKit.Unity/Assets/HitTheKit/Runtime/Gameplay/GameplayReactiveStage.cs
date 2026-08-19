using System;
using HitTheKit.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayStageEnergyBand
    {
        Calm,
        Building,
        Live,
        Peak,
        Recovery
    }

    public enum GameplayStagePattern
    {
        Steady,
        Rays,
        Chevrons,
        Warning
    }

    public readonly struct GameplayReactiveStageState
    {
        public GameplayReactiveStageState(
            GameplayStageEnergyBand band,
            GameplayStagePattern pattern,
            float intensity,
            DrumPad? accentPad,
            bool reducedMotion,
            bool highContrast,
            string label)
        {
            Band = band;
            Pattern = pattern;
            Intensity = intensity;
            AccentPad = accentPad;
            ReducedMotion = reducedMotion;
            HighContrast = highContrast;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public GameplayStageEnergyBand Band { get; }
        public GameplayStagePattern Pattern { get; }
        public float Intensity { get; }
        public DrumPad? AccentPad { get; }
        public bool ReducedMotion { get; }
        public bool HighContrast { get; }
        public string Label { get; }
    }

    public static class GameplayReactiveStageCalculator
    {
        public const float MaximumIntensity = 0.78f;
        public const float ReducedMotionMaximumIntensity = 0.22f;
        public const float MinimumPulseDurationSeconds = 0.18f;

        public static GameplayReactiveStageState Calculate(
            int combo,
            HitGrade? latestGrade,
            DrumPad? latestPad,
            float pulseIntensity,
            bool wrongInput,
            bool reducedMotion,
            bool highContrast)
        {
            if (combo < 0) throw new ArgumentOutOfRangeException(nameof(combo));

            float baseIntensity = combo >= 50 ? 0.62f
                : combo >= 25 ? 0.50f
                : combo >= 10 ? 0.36f
                : combo >= 3 ? 0.24f
                : 0.14f;
            float pulse = Mathf.Clamp01(pulseIntensity);
            bool recovery = pulse > 0f && (wrongInput || latestGrade == HitGrade.Miss);
            float intensity = recovery
                ? Mathf.Max(0.20f, baseIntensity * 0.62f)
                : baseIntensity + GradeBoost(latestGrade) * pulse;
            float cap = reducedMotion ? ReducedMotionMaximumIntensity : MaximumIntensity;
            intensity = Mathf.Clamp(intensity, 0f, cap);

            GameplayStageEnergyBand band = recovery ? GameplayStageEnergyBand.Recovery
                : combo >= 50 ? GameplayStageEnergyBand.Peak
                : combo >= 25 ? GameplayStageEnergyBand.Live
                : combo >= 3 ? GameplayStageEnergyBand.Building
                : GameplayStageEnergyBand.Calm;
            GameplayStagePattern pattern = recovery ? GameplayStagePattern.Warning
                : reducedMotion ? GameplayStagePattern.Steady
                : combo >= 25 ? GameplayStagePattern.Chevrons
                : combo >= 3 ? GameplayStagePattern.Rays
                : GameplayStagePattern.Steady;

            return new GameplayReactiveStageState(
                band, pattern, intensity, latestPad, reducedMotion, highContrast, LabelFor(band));
        }

        private static float GradeBoost(HitGrade? grade)
        {
            switch (grade)
            {
                case HitGrade.Perfect: return 0.16f;
                case HitGrade.Good: return 0.11f;
                case HitGrade.Early:
                case HitGrade.Late: return 0.06f;
                case HitGrade.Miss:
                case null: return 0f;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }
        }

        private static string LabelFor(GameplayStageEnergyBand band)
        {
            switch (band)
            {
                case GameplayStageEnergyBand.Calm: return "PALCO · CALMO";
                case GameplayStageEnergyBand.Building: return "PALCO · IN CRESCENDO";
                case GameplayStageEnergyBand.Live: return "PALCO · LIVE";
                case GameplayStageEnergyBand.Peak: return "PALCO · ON FIRE";
                case GameplayStageEnergyBand.Recovery: return "PALCO · RIPRENDI IL GROOVE";
                default: throw new ArgumentOutOfRangeException(nameof(band));
            }
        }
    }

    public sealed class GameplayReactiveStageSurface : VisualElement
    {
        private GameplayReactiveStageState state = GameplayReactiveStageCalculator.Calculate(
            0, null, null, 0, false, false, false);
        private GameplayPresentationTheme theme;

        public GameplayReactiveStageSurface()
        {
            name = "gameplay-reactive-stage-surface";
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public GameplayReactiveStageState State => state;

        public void SetTheme(GameplayPresentationTheme value)
        {
            theme = value;
            MarkDirtyRepaint();
        }

        public void SetState(GameplayReactiveStageState value)
        {
            state = value;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 1 || bounds.height <= 1) return;

            Painter2D painter = context.painter2D;
            GameplayThemePalette palette = GameplayThemePalette.For(theme);
            Color accent = state.AccentPad.HasValue
                ? GameplayHighwayLanes.Find(state.AccentPad.Value).Color
                : palette.Strike;
            float alpha = state.Intensity;
            float outline = state.HighContrast ? 3f : 1.4f;

            DrawBeam(painter, bounds, 0.04f, 0.34f, accent, alpha * 0.18f);
            DrawBeam(painter, bounds, 0.96f, 0.66f, accent, alpha * 0.18f);
            if (state.Pattern == GameplayStagePattern.Rays || state.Pattern == GameplayStagePattern.Chevrons)
            {
                DrawBeam(painter, bounds, 0.24f, 0.46f, palette.Strike, alpha * 0.12f);
                DrawBeam(painter, bounds, 0.76f, 0.54f, palette.Strike, alpha * 0.12f);
            }

            if (state.Pattern == GameplayStagePattern.Chevrons)
            {
                for (int row = 0; row < 3; row++)
                {
                    float y = bounds.height * (0.62f + row * 0.075f);
                    DrawChevron(painter, bounds.width * 0.12f, y, accent, alpha * 0.70f, outline);
                    DrawChevron(painter, bounds.width * 0.88f, y, accent, alpha * 0.70f, outline);
                }
            }
            else if (state.Pattern == GameplayStagePattern.Warning)
            {
                Color warning = new Color(1f, 0.34f, 0.28f, Mathf.Max(0.25f, alpha));
                StrokeLine(painter, new Vector2(18f, bounds.height * 0.54f),
                    new Vector2(18f, bounds.height * 0.86f), warning, warning.a, outline + 1f);
                StrokeLine(painter, new Vector2(bounds.width - 18f, bounds.height * 0.54f),
                    new Vector2(bounds.width - 18f, bounds.height * 0.86f), warning, warning.a, outline + 1f);
            }

            DrawCrowd(painter, bounds, accent, state.HighContrast ? 0.74f : 0.30f + alpha * 0.30f, outline);
        }

        private static void DrawChevron(Painter2D painter, float centerX, float y, Color color, float alpha, float width)
        {
            StrokeLine(painter, new Vector2(centerX - 42f, y), new Vector2(centerX, y - 16f), color, alpha, width);
            StrokeLine(painter, new Vector2(centerX, y - 16f), new Vector2(centerX + 42f, y), color, alpha, width);
        }

        private static void DrawBeam(Painter2D painter, Rect bounds, float originX, float targetX,
            Color color, float alpha)
        {
            painter.fillColor = WithAlpha(color, alpha);
            painter.BeginPath();
            painter.MoveTo(new Vector2(bounds.width * originX - 8f, 0));
            painter.LineTo(new Vector2(bounds.width * originX + 8f, 0));
            painter.LineTo(new Vector2(bounds.width * targetX + 90f, bounds.height));
            painter.LineTo(new Vector2(bounds.width * targetX - 90f, bounds.height));
            painter.ClosePath();
            painter.Fill();
        }

        private static void DrawCrowd(Painter2D painter, Rect bounds, Color accent, float alpha, float width)
        {
            float baseline = bounds.height - 8f;
            for (int index = 0; index < 18; index++)
            {
                float x = bounds.width * (index + 0.5f) / 18f;
                float height = 13f + (index % 4) * 4f;
                Color silhouette = index % 3 == 0
                    ? WithAlpha(accent, alpha * 0.50f)
                    : new Color(0.01f, 0.02f, 0.04f, alpha);
                StrokeLine(painter, new Vector2(x, baseline), new Vector2(x, baseline - height),
                    silhouette, silhouette.a, width + 3f);
            }
        }

        private static void StrokeLine(Painter2D painter, Vector2 from, Vector2 to, Color color, float alpha, float width)
        {
            painter.strokeColor = WithAlpha(color, alpha);
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }
}
