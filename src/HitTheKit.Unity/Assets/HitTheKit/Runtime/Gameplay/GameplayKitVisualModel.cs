using System;
using System.Collections.Generic;
using HitTheKit.Core;
using HitTheKit.Unity.Charts;

namespace HitTheKit.Unity.Gameplay
{
    public enum GameplayKitZone
    {
        Pedal,
        Head,
        Rim,
        Bow,
        Bell,
        Edge
    }

    public readonly struct GameplayKitTargetState
    {
        public GameplayKitTargetState(
            DrumPad pad,
            GameplayKitZone zone,
            bool isUpcoming,
            double timeUntilHitSeconds,
            float intensity,
            bool isHitPulse)
        {
            if (!Enum.IsDefined(typeof(DrumPad), pad))
                throw new ArgumentOutOfRangeException(nameof(pad));
            if (!Enum.IsDefined(typeof(GameplayKitZone), zone))
                throw new ArgumentOutOfRangeException(nameof(zone));
            if (float.IsNaN(intensity) || float.IsInfinity(intensity))
                throw new ArgumentOutOfRangeException(nameof(intensity));

            Pad = pad;
            Zone = zone;
            IsUpcoming = isUpcoming;
            TimeUntilHitSeconds = timeUntilHitSeconds;
            Intensity = Math.Max(0f, Math.Min(1f, intensity));
            IsHitPulse = isHitPulse;
        }

        public DrumPad Pad { get; }
        public GameplayKitZone Zone { get; }
        public bool IsUpcoming { get; }
        public double TimeUntilHitSeconds { get; }
        public float Intensity { get; }
        public bool IsHitPulse { get; }
    }

    public static class GameplayKitZoneResolver
    {
        public static string BeginnerPieceName(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return "GRANCASSA";
                case DrumPad.Snare: return "RULLANTE";
                case DrumPad.HiHat: return "CHARLESTON";
                case DrumPad.Tom1: return "TOM 1 · ALTO";
                case DrumPad.Tom2: return "TOM 2 · MEDIO";
                case DrumPad.FloorTom: return "TIMPANO";
                case DrumPad.Crash: return "CRASH";
                case DrumPad.Ride: return "RIDE";
                default:
                    throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        public static GameplayKitZone DefaultFor(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return GameplayKitZone.Pedal;
                case DrumPad.Snare:
                case DrumPad.Tom1:
                case DrumPad.Tom2:
                case DrumPad.FloorTom:
                    return GameplayKitZone.Head;
                case DrumPad.HiHat:
                case DrumPad.Ride:
                    return GameplayKitZone.Bow;
                case DrumPad.Crash:
                    return GameplayKitZone.Edge;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        public static GameplayKitZone For(DrumPad pad, DrumArticulation articulation)
        {
            DrumArticulationValidator.EnsureValid(pad, articulation);
            switch (articulation)
            {
                case DrumArticulation.Default: return DefaultFor(pad);
                case DrumArticulation.Head: return GameplayKitZone.Head;
                case DrumArticulation.Rim: return GameplayKitZone.Rim;
                case DrumArticulation.Bell: return GameplayKitZone.Bell;
                case DrumArticulation.Pedal: return GameplayKitZone.Pedal;
                case DrumArticulation.Edge:
                case DrumArticulation.Choke: return GameplayKitZone.Edge;
                case DrumArticulation.Bow:
                case DrumArticulation.Closed:
                case DrumArticulation.HalfOpen:
                case DrumArticulation.Open: return GameplayKitZone.Bow;
                default: throw new ArgumentOutOfRangeException(nameof(articulation));
            }
        }

        public static string BeginnerInstruction(DrumPad pad)
        {
            switch (pad)
            {
                case DrumPad.Kick: return "CASSA · PREMI IL PEDALE";
                case DrumPad.Snare: return "RULLANTE · COLPISCI LA PELLE";
                case DrumPad.HiHat: return "CHARLESTON · COLPISCI IL CORPO DEL PIATTO";
                case DrumPad.Tom1: return "TOM 1 · COLPISCI LA PELLE";
                case DrumPad.Tom2: return "TOM 2 · COLPISCI LA PELLE";
                case DrumPad.FloorTom: return "TIMPANO · COLPISCI LA PELLE";
                case DrumPad.Crash: return "CRASH · COLPISCI IL BORDO";
                case DrumPad.Ride: return "RIDE · COLPISCI IL CORPO";
                default:
                    throw new ArgumentOutOfRangeException(nameof(pad));
            }
        }

        public static string ZoneLabel(GameplayKitZone zone)
        {
            switch (zone)
            {
                case GameplayKitZone.Pedal: return "PEDALE";
                case GameplayKitZone.Head: return "PELLE";
                case GameplayKitZone.Rim: return "BORDO";
                case GameplayKitZone.Bow: return "CORPO";
                case GameplayKitZone.Bell: return "CAMPANA";
                case GameplayKitZone.Edge: return "BORDO";
                default:
                    throw new ArgumentOutOfRangeException(nameof(zone));
            }
        }
    }

    public sealed class GameplayKitTargetStateCalculator
    {
        public GameplayKitTargetState Calculate(
            DrumPad pad,
            IReadOnlyList<TimelineNote> upcomingNotes,
            double songPositionSeconds,
            double preparationSeconds,
            DrumPad? pulsePad,
            float pulseIntensity)
        {
            GameplayHighwayLanes.Find(pad);
            if (!IsFinite(songPositionSeconds))
                throw new ArgumentOutOfRangeException(nameof(songPositionSeconds));
            if (!IsFinite(preparationSeconds) || preparationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(preparationSeconds));
            if (float.IsNaN(pulseIntensity) || float.IsInfinity(pulseIntensity))
                throw new ArgumentOutOfRangeException(nameof(pulseIntensity));

            double nearest = double.PositiveInfinity;
            DrumArticulation nearestArticulation = DrumArticulation.Default;
            if (upcomingNotes != null)
            {
                for (int index = 0; index < upcomingNotes.Count; index++)
                {
                    TimelineNote note = upcomingNotes[index];
                    if (note == null || note.Note.Pad != pad) continue;
                    double delta = note.EffectiveTimeSeconds - songPositionSeconds;
                    if (delta < 0 || delta > preparationSeconds || delta >= nearest) continue;
                    nearest = delta;
                    nearestArticulation = note.Note.Articulation;
                }
            }

            bool isUpcoming = !double.IsPositiveInfinity(nearest);
            float approach = isUpcoming
                ? SmoothStep(1f - (float)(nearest / preparationSeconds))
                : 0f;
            bool isPulse = pulsePad.HasValue && pulsePad.Value == pad && pulseIntensity > 0f;
            float intensity = Math.Max(approach, isPulse ? Math.Max(0f, Math.Min(1f, pulseIntensity)) : 0f);

            return new GameplayKitTargetState(
                pad,
                GameplayKitZoneResolver.For(pad, nearestArticulation),
                isUpcoming,
                isUpcoming ? nearest : double.PositiveInfinity,
                intensity,
                isPulse);
        }

        private static float SmoothStep(float value)
        {
            float clamped = Math.Max(0f, Math.Min(1f, value));
            return clamped * clamped * (3f - (2f * clamped));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
