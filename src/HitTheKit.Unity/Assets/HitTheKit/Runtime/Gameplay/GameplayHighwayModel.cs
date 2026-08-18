using System;
using System.Collections.Generic;
using HitTheKit.Core;
using UnityEngine;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayPresentationTheme
    {
        ArcadeNeon,
        ConcertStage,
        PrecisionGrid
    }

    public enum GameplayNoteShape
    {
        ArcadeDiamond,
        ConcertDisc,
        PrecisionOutline
    }

    public readonly struct GameplayEnvironmentProfile
    {
        private GameplayEnvironmentProfile(
            string title,
            string subtitle,
            GameplayNoteShape noteShape,
            bool showsInstructionalKit,
            float horizonRatio,
            float strikeRatio,
            float topLeftRatio,
            float topRightRatio,
            float bottomLeftRatio,
            float bottomRightRatio)
        {
            Title = title;
            Subtitle = subtitle;
            NoteShape = noteShape;
            ShowsInstructionalKit = showsInstructionalKit;
            HorizonRatio = horizonRatio;
            StrikeRatio = strikeRatio;
            TopLeftRatio = topLeftRatio;
            TopRightRatio = topRightRatio;
            BottomLeftRatio = bottomLeftRatio;
            BottomRightRatio = bottomRightRatio;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public GameplayNoteShape NoteShape { get; }
        public bool ShowsInstructionalKit { get; }
        public float HorizonRatio { get; }
        public float StrikeRatio { get; }
        public float TopLeftRatio { get; }
        public float TopRightRatio { get; }
        public float BottomLeftRatio { get; }
        public float BottomRightRatio { get; }

        public static GameplayEnvironmentProfile For(GameplayPresentationTheme theme)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon:
                    return new GameplayEnvironmentProfile(
                        "ARCADE NEON", "HOLOGRAPHIC RUNWAY", GameplayNoteShape.ArcadeDiamond, false,
                        0.055f, 0.89f, 0.36f, 0.64f, 0.035f, 0.965f);
                case GameplayPresentationTheme.ConcertStage:
                    return new GameplayEnvironmentProfile(
                        "CONCERT STAGE", "LIVE DRUM ARENA", GameplayNoteShape.ConcertDisc, false,
                        0.075f, 0.72f, 0.41f, 0.59f, 0.15f, 0.85f);
                case GameplayPresentationTheme.PrecisionGrid:
                    return new GameplayEnvironmentProfile(
                        "PRECISION GRID", "TECHNICAL TRAINING CHAMBER", GameplayNoteShape.PrecisionOutline, true,
                        0.20f, 0.89f, 0.38f, 0.62f, 0.055f, 0.945f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme));
            }
        }
    }

    public sealed class GameplayLaneDefinition
    {
        internal GameplayLaneDefinition(
            DrumPad pad,
            string id,
            string label,
            string subtitle,
            string key,
            Color color)
        {
            Pad = pad;
            Id = id;
            Label = label;
            Subtitle = subtitle;
            Key = key;
            Color = color;
        }

        public DrumPad Pad { get; }
        public string Id { get; }
        public string Label { get; }
        public string Subtitle { get; }
        public string Key { get; }
        public Color Color { get; }
    }

    public static class GameplayHighwayLanes
    {
        private static readonly GameplayLaneDefinition[] definitions =
        {
            Lane(DrumPad.HiHat, "hi-hat", "HI-HAT", "CHARLESTON", "K", "62E87C"),
            Lane(DrumPad.Snare, "snare", "SNARE", "RULLANTE", "J", "B77BFF"),
            Lane(DrumPad.Tom1, "tom-1", "TOM 1", "ALTO", "G", "45C9FF"),
            Lane(DrumPad.Tom2, "tom-2", "TOM 2", "MEDIO", "H", "4D8DFF"),
            Lane(DrumPad.FloorTom, "floor-tom", "FLOOR TOM", "TIMPANO", "L", "FFC04D"),
            Lane(DrumPad.Crash, "crash", "CRASH", "PIATTO", "D", "FF685E"),
            Lane(DrumPad.Ride, "ride", "RIDE", "PIATTO", "S", "6C7CFF"),
            Lane(DrumPad.Kick, "kick", "KICK", "GRANCASSA", "F", "FF4F70")
        };

        private static readonly IReadOnlyList<GameplayLaneDefinition> readOnly =
            Array.AsReadOnly(definitions);

        public static IReadOnlyList<GameplayLaneDefinition> All => readOnly;
        public static int HighwayLaneCount => definitions.Length - 1;

        public static GameplayLaneDefinition Find(DrumPad pad)
        {
            for (int index = 0; index < definitions.Length; index++)
            {
                if (definitions[index].Pad == pad) return definitions[index];
            }

            throw new ArgumentOutOfRangeException(nameof(pad));
        }

        public static int HighwayIndex(DrumPad pad)
        {
            for (int index = 0; index < HighwayLaneCount; index++)
            {
                if (definitions[index].Pad == pad) return index;
            }

            return -1;
        }

        private static GameplayLaneDefinition Lane(
            DrumPad pad,
            string id,
            string label,
            string subtitle,
            string key,
            string htmlColor)
        {
            ColorUtility.TryParseHtmlString($"#{htmlColor}", out Color color);
            return new GameplayLaneDefinition(pad, id, label, subtitle, key, color);
        }
    }

    public readonly struct GameplayThemePalette
    {
        private GameplayThemePalette(
            Color surface,
            Color grid,
            Color strike,
            Color noteCore,
            Color kick,
            float surfaceOpacity)
        {
            Surface = surface;
            Grid = grid;
            Strike = strike;
            NoteCore = noteCore;
            Kick = kick;
            SurfaceOpacity = surfaceOpacity;
        }

        public Color Surface { get; }
        public Color Grid { get; }
        public Color Strike { get; }
        public Color NoteCore { get; }
        public Color Kick { get; }
        public float SurfaceOpacity { get; }

        public static GameplayThemePalette For(GameplayPresentationTheme theme)
        {
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon:
                    return new GameplayThemePalette(
                        Html("08101F", 0.86f), Html("65CFFF", 0.42f), Html("E9FCFF"),
                        Html("FFFFFF"), Html("C36BFF"), 0.86f);
                case GameplayPresentationTheme.ConcertStage:
                    return new GameplayThemePalette(
                        Html("100C0A", 0.76f), Html("FFB14A", 0.34f), Html("FFF1D1"),
                        Html("FFF7E6"), Html("FF9A38"), 0.76f);
                case GameplayPresentationTheme.PrecisionGrid:
                    return new GameplayThemePalette(
                        Html("06101B", 0.92f), Html("6DDCFF", 0.40f), Html("F1FDFF"),
                        Html("FFFFFF"), Html("FF654F"), 0.92f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(theme));
            }
        }

        private static Color Html(string value, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString($"#{value}", out Color color);
            color.a = alpha;
            return color;
        }
    }
}
