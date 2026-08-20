using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class GameplayTargetSymbol : VisualElement
    {
        public GameplayTargetSymbol(GameplayLaneSymbol symbol, Color color)
        {
            Symbol = symbol;
            Color = color;
            pickingMode = PickingMode.Ignore;
            AddToClassList("target-symbol");
            generateVisualContent += Draw;
        }

        public GameplayLaneSymbol Symbol { get; }
        public Color Color { get; }

        private void Draw(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 1 || bounds.height <= 1) return;

            Painter2D painter = context.painter2D;
            Vector2 center = bounds.center;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.42f;
            StrokePolygon(painter, center, radius, 16, WithAlpha(Color, 0.55f), 1.25f);

            switch (Symbol)
            {
                case GameplayLaneSymbol.Cross:
                    StrokeLine(painter, center + new Vector2(-radius * 0.42f, -radius * 0.42f),
                        center + new Vector2(radius * 0.42f, radius * 0.42f), Color, 2.2f);
                    StrokeLine(painter, center + new Vector2(-radius * 0.42f, radius * 0.42f),
                        center + new Vector2(radius * 0.42f, -radius * 0.42f), Color, 2.2f);
                    break;
                case GameplayLaneSymbol.Circle:
                    StrokePolygon(painter, center, radius * 0.48f, 16, Color, 2.1f);
                    break;
                case GameplayLaneSymbol.Square:
                    StrokePolygon(painter, center, radius * 0.55f, 4, Color, 2.1f, Mathf.PI * 0.25f);
                    break;
                case GameplayLaneSymbol.Diamond:
                    StrokePolygon(painter, center, radius * 0.62f, 4, Color, 2.1f);
                    break;
                case GameplayLaneSymbol.Triangle:
                    StrokePolygon(painter, center, radius * 0.65f, 3, Color, 2.1f);
                    break;
                case GameplayLaneSymbol.Capsule:
                    StrokeLine(painter, center + Vector2.left * radius * 0.45f,
                        center + Vector2.right * radius * 0.45f, Color, 3f);
                    break;
            }
        }

        private static void StrokePolygon(Painter2D painter, Vector2 center, float radius, int sides,
            Color color, float width, float rotation = 0)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            for (int index = 0; index < sides; index++)
            {
                float angle = rotation - Mathf.PI * 0.5f + Mathf.PI * 2f * index / sides;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (index == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            painter.Stroke();
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
