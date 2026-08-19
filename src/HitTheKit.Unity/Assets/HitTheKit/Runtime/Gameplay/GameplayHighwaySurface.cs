using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class GameplayHighwaySurface : VisualElement
    {
        public const float ImpactZoneHalfHeight = 9f;
        public const int NoteGlowPasses = 3;
        private IReadOnlyList<TimelineNote> notes = Array.Empty<TimelineNote>();
        private IReadOnlyList<GhostReplayHit> ghostHits = Array.Empty<GhostReplayHit>();
        private double songPositionSeconds;
        private double lookAheadSeconds = 4;
        private GameplayPresentationTheme theme;
        private DrumPad? pulsePad;
        private float pulseIntensity;

        public GameplayHighwaySurface()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public GameplayPresentationTheme Theme => theme;
        public IReadOnlyList<TimelineNote> Notes => notes;
        public IReadOnlyList<GhostReplayHit> GhostHits => ghostHits;
        public double SongPositionSeconds => songPositionSeconds;

        public void SetTheme(GameplayPresentationTheme value)
        {
            theme = value;
            MarkDirtyRepaint();
        }

        public void SetFrame(
            IReadOnlyList<TimelineNote> upcomingNotes,
            double positionSeconds,
            double lookAhead,
            DrumPad? highlightedPad,
            float highlightedIntensity,
            IReadOnlyList<GhostReplayHit> replayHits = null)
        {
            notes = upcomingNotes ?? Array.Empty<TimelineNote>();
            ghostHits = replayHits ?? Array.Empty<GhostReplayHit>();
            songPositionSeconds = IsFinite(positionSeconds) ? positionSeconds : 0;
            lookAheadSeconds = IsFinite(lookAhead) && lookAhead > 0 ? lookAhead : 4;
            pulsePad = highlightedPad;
            pulseIntensity = Mathf.Clamp01(highlightedIntensity);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 1 || bounds.height <= 1) return;

            Painter2D painter = context.painter2D;
            GameplayThemePalette palette = GameplayThemePalette.For(theme);
            GameplayEnvironmentProfile environment = GameplayEnvironmentProfile.For(theme);
            float horizonY = bounds.height * environment.HorizonRatio;
            float strikeY = bounds.height * environment.StrikeRatio;
            float topLeft = bounds.width * environment.TopLeftRatio;
            float topRight = bounds.width * environment.TopRightRatio;
            float bottomLeft = bounds.width * environment.BottomLeftRatio;
            float bottomRight = bounds.width * environment.BottomRightRatio;
            int laneCount = GameplayHighwayLanes.HighwayLaneCount;

            FillQuad(painter,
                new Vector2(topLeft, horizonY),
                new Vector2(topRight, horizonY),
                new Vector2(bottomRight, strikeY),
                new Vector2(bottomLeft, strikeY),
                palette.Surface);

            for (int lane = 0; lane <= laneCount; lane++)
            {
                float ratio = lane / (float)laneCount;
                Vector2 start = new Vector2(Mathf.Lerp(topLeft, topRight, ratio), horizonY);
                Vector2 end = new Vector2(Mathf.Lerp(bottomLeft, bottomRight, ratio), strikeY);
                StrokeLine(painter, start, end, palette.Grid, lane == 0 || lane == laneCount ? 2.2f : 1.2f);
            }

            FillQuad(painter,
                new Vector2(bottomLeft - 7, strikeY - ImpactZoneHalfHeight),
                new Vector2(bottomRight + 7, strikeY - ImpactZoneHalfHeight),
                new Vector2(bottomRight + 9, strikeY + ImpactZoneHalfHeight),
                new Vector2(bottomLeft - 9, strikeY + ImpactZoneHalfHeight),
                new Color(palette.Strike.r, palette.Strike.g, palette.Strike.b, 0.12f));
            StrokeLine(painter,
                new Vector2(bottomLeft - 8, strikeY - ImpactZoneHalfHeight),
                new Vector2(bottomRight + 8, strikeY - ImpactZoneHalfHeight),
                new Color(palette.Strike.r, palette.Strike.g, palette.Strike.b, 0.34f), 1.4f);
            StrokeLine(painter,
                new Vector2(bottomLeft - 8, strikeY + ImpactZoneHalfHeight),
                new Vector2(bottomRight + 8, strikeY + ImpactZoneHalfHeight),
                new Color(palette.Strike.r, palette.Strike.g, palette.Strike.b, 0.34f), 1.4f);

            for (int marker = 1; marker < 8; marker++)
            {
                float progress = marker / 8f;
                float eased = progress * progress;
                float y = Mathf.Lerp(horizonY, strikeY, eased);
                float left = Mathf.Lerp(topLeft, bottomLeft, eased);
                float right = Mathf.Lerp(topRight, bottomRight, eased);
                StrokeLine(painter, new Vector2(left, y), new Vector2(right, y),
                    new Color(palette.Grid.r, palette.Grid.g, palette.Grid.b, palette.Grid.a * 0.34f), 0.8f);
            }

            for (int index = 0; index < ghostHits.Count; index++)
            {
                GhostReplayHit hit = ghostHits[index];
                if (hit == null) continue;
                double delta = hit.TimeSeconds - songPositionSeconds;
                if (delta < -0.16 || delta > lookAheadSeconds) continue;
                DrawGhost(painter, hit.Pad, delta,
                    horizonY, strikeY, topLeft, topRight, bottomLeft, bottomRight);
            }

            for (int index = 0; index < notes.Count; index++)
            {
                TimelineNote note = notes[index];
                if (note == null) continue;
                double delta = note.EffectiveTimeSeconds - songPositionSeconds;
                if (delta < -0.16 || delta > lookAheadSeconds) continue;
                DrawNote(painter, environment.NoteShape, note.Note.Pad, delta,
                    horizonY, strikeY, topLeft, topRight, bottomLeft, bottomRight);
            }

            StrokeLine(painter,
                new Vector2(bottomLeft - 12, strikeY),
                new Vector2(bottomRight + 12, strikeY),
                new Color(palette.Strike.r, palette.Strike.g, palette.Strike.b, 0.18f), 12f);
            StrokeLine(painter,
                new Vector2(bottomLeft - 9, strikeY),
                new Vector2(bottomRight + 9, strikeY),
                new Color(palette.Strike.r, palette.Strike.g, palette.Strike.b, 0.45f), 7f);
            StrokeLine(painter,
                new Vector2(bottomLeft - 5, strikeY),
                new Vector2(bottomRight + 5, strikeY),
                pulseIntensity > 0.01f ? Color.Lerp(palette.Strike, Color.white, pulseIntensity) : palette.Strike,
                pulseIntensity > 0.01f ? 5.5f : 3.6f);
        }

        private void DrawGhost(
            Painter2D painter,
            DrumPad pad,
            double delta,
            float horizonY,
            float strikeY,
            float topLeft,
            float topRight,
            float bottomLeft,
            float bottomRight)
        {
            float normalized = 1f - Mathf.Clamp01((float)(delta / lookAheadSeconds));
            float eased = normalized * normalized;
            float y = Mathf.Lerp(horizonY, strikeY, eased);
            float x;
            if (pad == DrumPad.Kick)
            {
                x = Mathf.Lerp((topLeft + topRight) * 0.5f, (bottomLeft + bottomRight) * 0.5f, eased);
            }
            else
            {
                int laneIndex = GameplayHighwayLanes.HighwayIndex(pad);
                if (laneIndex < 0) return;
                float ratio = (laneIndex + 0.5f) / GameplayHighwayLanes.HighwayLaneCount;
                x = Mathf.Lerp(Mathf.Lerp(topLeft, topRight, ratio), Mathf.Lerp(bottomLeft, bottomRight, ratio), eased);
            }

            float radiusX = Mathf.Lerp(4f, pad == DrumPad.Kick ? 27f : 18f, eased);
            float radiusY = Mathf.Lerp(3f, 11f, eased);
            var center = new Vector2(x, y);
            Color outline = new Color(1f, 1f, 1f, 0.54f);
            StrokeRegularPolygon(painter, center, radiusX, radiusY, 10, outline, 1.8f);
            StrokeLine(painter,
                new Vector2(center.x - radiusX * 0.45f, center.y - radiusY * 0.45f),
                new Vector2(center.x + radiusX * 0.45f, center.y + radiusY * 0.45f), outline, 1.4f);
            StrokeLine(painter,
                new Vector2(center.x - radiusX * 0.45f, center.y + radiusY * 0.45f),
                new Vector2(center.x + radiusX * 0.45f, center.y - radiusY * 0.45f), outline, 1.4f);
        }

        private void DrawNote(
            Painter2D painter,
            GameplayNoteShape shape,
            DrumPad pad,
            double delta,
            float horizonY,
            float strikeY,
            float topLeft,
            float topRight,
            float bottomLeft,
            float bottomRight)
        {
            float normalized = 1f - Mathf.Clamp01((float)(delta / lookAheadSeconds));
            float eased = normalized * normalized;
            float y = Mathf.Lerp(horizonY, strikeY, eased);
            GameplayLaneDefinition lane = GameplayHighwayLanes.Find(pad);
            Color color = lane.Color;
            if (pulsePad == pad)
            {
                color = Color.Lerp(color, Color.white, pulseIntensity * 0.75f);
            }

            if (pad == DrumPad.Kick)
            {
                float width = Mathf.Lerp((topRight - topLeft) * 0.72f, (bottomRight - bottomLeft) * 0.72f, eased);
                float height = Mathf.Lerp(4f, 13f, eased);
                var center = new Vector2((topLeft + topRight) * 0.5f, y);
                float approach = Mathf.Clamp01((normalized - 0.58f) / 0.42f);
                if (approach > 0)
                    StrokeLine(painter, center, new Vector2((bottomLeft + bottomRight) * 0.5f, strikeY),
                        WithAlpha(color, 0.10f + approach * 0.24f), Mathf.Lerp(2f, 7f, approach));
                DrawKickGlow(painter, shape, center, width, height, color);
                DrawKickNote(painter, shape, center, width, height, color);
                return;
            }

            int laneIndex = GameplayHighwayLanes.HighwayIndex(pad);
            if (laneIndex < 0) return;
            float laneCount = GameplayHighwayLanes.HighwayLaneCount;
            float laneRatio = (laneIndex + 0.5f) / laneCount;
            float topX = Mathf.Lerp(topLeft, topRight, laneRatio);
            float bottomX = Mathf.Lerp(bottomLeft, bottomRight, laneRatio);
            float x = Mathf.Lerp(topX, bottomX, eased);
            float radiusX = Mathf.Lerp(5f, 22f, eased);
            float radiusY = Mathf.Lerp(3.5f, 13f, eased);
            var noteCenter = new Vector2(x, y);
            float bottomXForLane = Mathf.Lerp(bottomLeft, bottomRight, laneRatio);
            float laneApproach = Mathf.Clamp01((normalized - 0.58f) / 0.42f);
            if (laneApproach > 0)
                StrokeLine(painter, noteCenter, new Vector2(bottomXForLane, strikeY),
                    WithAlpha(color, 0.10f + laneApproach * 0.26f), Mathf.Lerp(1.5f, 5f, laneApproach));
            DrawLaneGlow(painter, shape, noteCenter, radiusX, radiusY, color);
            DrawLaneNote(painter, shape, noteCenter, radiusX, radiusY, color);
        }

        private static void DrawLaneGlow(
            Painter2D painter,
            GameplayNoteShape shape,
            Vector2 center,
            float radiusX,
            float radiusY,
            Color color)
        {
            for (int pass = NoteGlowPasses; pass >= 1; pass--)
            {
                float scale = 1f + pass * 0.26f;
                Color glow = WithAlpha(color, 0.045f + (NoteGlowPasses - pass) * 0.025f);
                switch (shape)
                {
                    case GameplayNoteShape.ArcadeDiamond:
                        FillDiamond(painter, center, radiusX * scale, radiusY * scale, glow);
                        break;
                    case GameplayNoteShape.ConcertDisc:
                        FillRegularPolygon(painter, center, radiusX * scale, radiusY * scale, 10, glow);
                        break;
                    case GameplayNoteShape.PrecisionOutline:
                        FillRegularPolygon(painter, center, radiusX * scale, radiusY * scale, 4, glow, Mathf.PI * 0.25f);
                        break;
                }
            }
        }

        private static void DrawLaneNote(
            Painter2D painter,
            GameplayNoteShape shape,
            Vector2 center,
            float radiusX,
            float radiusY,
            Color color)
        {
            switch (shape)
            {
                case GameplayNoteShape.ArcadeDiamond:
                    FillDiamond(painter, center, radiusX, radiusY, color);
                    FillDiamond(painter, center, radiusX * 0.45f, radiusY * 0.45f, Color.white);
                    break;
                case GameplayNoteShape.ConcertDisc:
                    FillRegularPolygon(painter, center, radiusX, radiusY, 10, color);
                    StrokeRegularPolygon(painter, center, radiusX * 0.80f, radiusY * 0.72f, 10, Color.white, 1.2f);
                    break;
                case GameplayNoteShape.PrecisionOutline:
                    FillRegularPolygon(painter, center, radiusX * 0.82f, radiusY * 0.82f, 4,
                        new Color(color.r, color.g, color.b, 0.28f), Mathf.PI * 0.25f);
                    StrokeRegularPolygon(painter, center, radiusX, radiusY, 4, color, 2.2f, Mathf.PI * 0.25f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        private static void DrawKickGlow(
            Painter2D painter,
            GameplayNoteShape shape,
            Vector2 center,
            float width,
            float height,
            Color color)
        {
            for (int pass = NoteGlowPasses; pass >= 1; pass--)
            {
                float scale = 1f + pass * 0.18f;
                Color glow = WithAlpha(color, 0.045f + (NoteGlowPasses - pass) * 0.025f);
                switch (shape)
                {
                    case GameplayNoteShape.ArcadeDiamond:
                        FillDiamond(painter, center, width * scale * 0.5f, height * scale, glow);
                        break;
                    case GameplayNoteShape.ConcertDisc:
                        FillRegularPolygon(painter, center, width * scale * 0.40f, height * scale * 1.15f, 10, glow);
                        break;
                    case GameplayNoteShape.PrecisionOutline:
                        FillQuad(painter,
                            new Vector2(center.x - width * scale * 0.5f, center.y - height * scale),
                            new Vector2(center.x + width * scale * 0.5f, center.y - height * scale),
                            new Vector2(center.x + width * scale * 0.5f, center.y + height * scale),
                            new Vector2(center.x - width * scale * 0.5f, center.y + height * scale),
                            glow);
                        break;
                }
            }
        }

        private static void DrawKickNote(
            Painter2D painter,
            GameplayNoteShape shape,
            Vector2 center,
            float width,
            float height,
            Color color)
        {
            switch (shape)
            {
                case GameplayNoteShape.ArcadeDiamond:
                    FillDiamond(painter, center, width * 0.5f, height, color);
                    break;
                case GameplayNoteShape.ConcertDisc:
                    FillRegularPolygon(painter, center, width * 0.40f, height * 1.15f, 10, color);
                    StrokeRegularPolygon(painter, center, width * 0.34f, height * 0.72f, 10, Color.white, 1.2f);
                    break;
                case GameplayNoteShape.PrecisionOutline:
                    FillQuad(painter,
                        new Vector2(center.x - width * 0.5f, center.y - height),
                        new Vector2(center.x + width * 0.5f, center.y - height),
                        new Vector2(center.x + width * 0.5f, center.y + height),
                        new Vector2(center.x - width * 0.5f, center.y + height),
                        new Color(color.r, color.g, color.b, 0.30f));
                    StrokeLine(painter,
                        new Vector2(center.x - width * 0.5f, center.y),
                        new Vector2(center.x + width * 0.5f, center.y), color, 2.5f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        private static void FillQuad(
            Painter2D painter,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.LineTo(c);
            painter.LineTo(d);
            painter.ClosePath();
            painter.Fill();
        }

        private static void FillDiamond(Painter2D painter, Vector2 center, float radiusX, float radiusY, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x, center.y - radiusY));
            painter.LineTo(new Vector2(center.x + radiusX, center.y));
            painter.LineTo(new Vector2(center.x, center.y + radiusY));
            painter.LineTo(new Vector2(center.x - radiusX, center.y));
            painter.ClosePath();
            painter.Fill();
        }

        private static void FillRegularPolygon(
            Painter2D painter,
            Vector2 center,
            float radiusX,
            float radiusY,
            int sides,
            Color color,
            float rotation = 0f)
        {
            painter.fillColor = color;
            BeginRegularPolygon(painter, center, radiusX, radiusY, sides, rotation);
            painter.Fill();
        }

        private static void StrokeRegularPolygon(
            Painter2D painter,
            Vector2 center,
            float radiusX,
            float radiusY,
            int sides,
            Color color,
            float width,
            float rotation = 0f)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            BeginRegularPolygon(painter, center, radiusX, radiusY, sides, rotation);
            painter.Stroke();
        }

        private static void BeginRegularPolygon(
            Painter2D painter,
            Vector2 center,
            float radiusX,
            float radiusY,
            int sides,
            float rotation)
        {
            painter.BeginPath();
            for (int index = 0; index < sides; index++)
            {
                float angle = rotation - Mathf.PI * 0.5f + Mathf.PI * 2f * index / sides;
                var point = new Vector2(
                    center.x + Mathf.Cos(angle) * radiusX,
                    center.y + Mathf.Sin(angle) * radiusY);
                if (index == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
        }

        private static void StrokeLine(Painter2D painter, Vector2 start, Vector2 end, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.Stroke();
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
