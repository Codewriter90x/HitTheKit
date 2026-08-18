using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HitTheKit.Unity.Gameplay
{
    public sealed class GameplayKitSurface : VisualElement
    {
        private const double SimultaneousToleranceSeconds = 0.025;
        private readonly Dictionary<DrumPad, GameplayKitPieceElement> pieces =
            new Dictionary<DrumPad, GameplayKitPieceElement>();
        private readonly GameplayKitTargetStateCalculator calculator =
            new GameplayKitTargetStateCalculator();
        private int guidanceMask = -1;

        public GameplayKitSurface()
        {
            name = "gameplay-kit-surface";
            pickingMode = PickingMode.Ignore;
            AddToClassList("gameplay-kit-surface");

            AddPiece(DrumPad.Crash, 4, 4, 25, 23, "kit-piece--cymbal");
            AddPiece(DrumPad.Ride, 71, 4, 25, 23, "kit-piece--cymbal");
            AddPiece(DrumPad.Tom1, 31, 25, 17, 25, "kit-piece--drum");
            AddPiece(DrumPad.Tom2, 52, 25, 17, 25, "kit-piece--drum");
            AddPiece(DrumPad.HiHat, 3, 38, 22, 23, "kit-piece--cymbal");
            AddPiece(DrumPad.Snare, 23, 50, 20, 28, "kit-piece--drum");
            AddPiece(DrumPad.FloorTom, 75, 47, 21, 31, "kit-piece--drum");
            AddPiece(DrumPad.Kick, 41, 49, 22, 44, "kit-piece--kick");
        }

        public int PieceCount => pieces.Count;
        public string GuidanceText { get; private set; } = "SEGUI I COLORI: LA NOTA E IL PEZZO SI ACCENDONO INSIEME";

        public bool TryGetTargetState(DrumPad pad, out GameplayKitTargetState state)
        {
            if (pieces.TryGetValue(pad, out GameplayKitPieceElement piece))
            {
                state = piece.CurrentState;
                return true;
            }

            state = default;
            return false;
        }

        public void SetTheme(GameplayPresentationTheme theme)
        {
            if (!Enum.IsDefined(typeof(GameplayPresentationTheme), theme))
                throw new ArgumentOutOfRangeException(nameof(theme));

            RemoveFromClassList("kit-theme--arcade-neon");
            RemoveFromClassList("kit-theme--concert-stage");
            RemoveFromClassList("kit-theme--precision-grid");
            switch (theme)
            {
                case GameplayPresentationTheme.ArcadeNeon:
                    AddToClassList("kit-theme--arcade-neon");
                    break;
                case GameplayPresentationTheme.ConcertStage:
                    AddToClassList("kit-theme--concert-stage");
                    break;
                case GameplayPresentationTheme.PrecisionGrid:
                    AddToClassList("kit-theme--precision-grid");
                    break;
            }
        }

        public void SetFrame(
            IReadOnlyList<TimelineNote> upcomingNotes,
            double songPositionSeconds,
            double preparationSeconds,
            DrumPad? pulsePad,
            float pulseIntensity)
        {
            double nearest = double.PositiveInfinity;
            int nextMask = 0;

            foreach (KeyValuePair<DrumPad, GameplayKitPieceElement> pair in pieces)
            {
                GameplayKitTargetState state = calculator.Calculate(
                    pair.Key,
                    upcomingNotes,
                    songPositionSeconds,
                    preparationSeconds,
                    pulsePad,
                    pulseIntensity);
                pair.Value.Apply(state);
                if (!state.IsUpcoming) continue;

                if (state.TimeUntilHitSeconds < nearest - SimultaneousToleranceSeconds)
                {
                    nearest = state.TimeUntilHitSeconds;
                    nextMask = 1 << (int)state.Pad;
                }
                else if (Math.Abs(state.TimeUntilHitSeconds - nearest) <= SimultaneousToleranceSeconds)
                {
                    nextMask |= 1 << (int)state.Pad;
                }
            }

            if (nextMask == guidanceMask) return;
            guidanceMask = nextMask;
            GuidanceText = BuildGuidance(nextMask);
        }

        private void AddPiece(
            DrumPad pad,
            float left,
            float top,
            float width,
            float height,
            string familyClass)
        {
            GameplayLaneDefinition lane = GameplayHighwayLanes.Find(pad);
            var piece = new GameplayKitPieceElement(lane, familyClass);
            piece.style.left = Length.Percent(left);
            piece.style.top = Length.Percent(top);
            piece.style.width = Length.Percent(width);
            piece.style.height = Length.Percent(height);
            Add(piece);
            pieces.Add(pad, piece);
        }

        private static string BuildGuidance(int mask)
        {
            if (mask == 0)
                return "SEGUI I COLORI: LA NOTA E IL PEZZO SI ACCENDONO INSIEME";

            string guidance = null;
            int count = 0;
            IReadOnlyList<GameplayLaneDefinition> lanes = GameplayHighwayLanes.All;
            for (int index = 0; index < lanes.Count; index++)
            {
                DrumPad pad = lanes[index].Pad;
                if ((mask & (1 << (int)pad)) == 0) continue;
                string instruction = GameplayKitZoneResolver.BeginnerInstruction(pad);
                guidance = guidance == null ? instruction : guidance + "  +  " + instruction;
                count++;
            }

            return count > 1 ? "INSIEME · " + guidance : guidance;
        }

        private sealed class GameplayKitPieceElement : VisualElement
        {
            private readonly GameplayLaneDefinition lane;
            private readonly VisualElement edgeZone;
            private readonly VisualElement bodyZone;
            private readonly VisualElement centerZone;
            private readonly VisualElement pedalZone;
            private readonly Label zoneLabel;

            public GameplayKitPieceElement(GameplayLaneDefinition definition, string familyClass)
            {
                lane = definition;
                name = "kit-piece-" + lane.Id;
                pickingMode = PickingMode.Ignore;
                AddToClassList("kit-piece");
                AddToClassList(familyClass);
                AddToClassList("lane--" + lane.Id);

                edgeZone = Zone("kit-zone kit-zone--edge");
                bodyZone = Zone("kit-zone kit-zone--body");
                centerZone = Zone("kit-zone kit-zone--center");
                pedalZone = Zone("kit-zone kit-zone--pedal");
                Add(edgeZone);
                Add(bodyZone);
                Add(centerZone);
                Add(pedalZone);

                var label = new Label(GameplayKitZoneResolver.BeginnerPieceName(lane.Pad));
                label.AddToClassList("kit-piece-label");
                Add(label);
                zoneLabel = new Label(GameplayKitZoneResolver.ZoneLabel(GameplayKitZoneResolver.DefaultFor(lane.Pad)));
                zoneLabel.AddToClassList("kit-zone-label");
                Add(zoneLabel);

                CurrentState = new GameplayKitTargetState(
                    lane.Pad,
                    GameplayKitZoneResolver.DefaultFor(lane.Pad),
                    false,
                    double.PositiveInfinity,
                    0,
                    false);
                Apply(CurrentState);
            }

            public GameplayKitTargetState CurrentState { get; private set; }

            public void Apply(GameplayKitTargetState state)
            {
                if (state.Pad != lane.Pad)
                    throw new ArgumentException("Target state pad does not match this kit piece.", nameof(state));

                CurrentState = state;
                Color dim = new Color(lane.Color.r, lane.Color.g, lane.Color.b, 0.12f);
                Color outline = new Color(lane.Color.r, lane.Color.g, lane.Color.b, 0.48f + (0.52f * state.Intensity));
                Color active = Color.Lerp(
                    new Color(lane.Color.r, lane.Color.g, lane.Color.b, 0.18f),
                    new Color(lane.Color.r, lane.Color.g, lane.Color.b, 0.96f),
                    state.Intensity);

                style.borderTopColor = outline;
                style.borderRightColor = outline;
                style.borderBottomColor = outline;
                style.borderLeftColor = outline;
                style.backgroundColor = state.Intensity > 0 ? dim : new Color(0.02f, 0.04f, 0.07f, 0.76f);
                style.opacity = 0.72f + (state.Intensity * 0.28f);

                SetZone(edgeZone, state.Zone == GameplayKitZone.Edge || state.Zone == GameplayKitZone.Rim, active);
                SetZone(bodyZone, state.Zone == GameplayKitZone.Bow || state.Zone == GameplayKitZone.Head, active);
                SetZone(centerZone, state.Zone == GameplayKitZone.Bell, active);
                SetZone(pedalZone, state.Zone == GameplayKitZone.Pedal, active);
                zoneLabel.style.color = state.Intensity > 0 ? lane.Color : new Color(0.65f, 0.72f, 0.80f, 0.78f);
                EnableInClassList("kit-piece--active", state.Intensity > 0.01f);
                EnableInClassList("kit-piece--hit", state.IsHitPulse);
            }

            private static VisualElement Zone(string classes)
            {
                var zone = new VisualElement { pickingMode = PickingMode.Ignore };
                string[] split = classes.Split(' ');
                for (int index = 0; index < split.Length; index++) zone.AddToClassList(split[index]);
                return zone;
            }

            private static void SetZone(VisualElement zone, bool selected, Color active)
            {
                zone.style.backgroundColor = selected ? active : Color.clear;
                zone.style.borderTopColor = selected ? active : Color.clear;
                zone.style.borderRightColor = selected ? active : Color.clear;
                zone.style.borderBottomColor = selected ? active : Color.clear;
                zone.style.borderLeftColor = selected ? active : Color.clear;
            }
        }
    }
}
