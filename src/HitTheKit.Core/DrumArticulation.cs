using System;

namespace HitTheKit.Core
{
    public enum DrumArticulation
    {
        Default,
        Head,
        Rim,
        Bow,
        Edge,
        Bell,
        Closed,
        HalfOpen,
        Open,
        Pedal,
        Choke
    }

    public static class DrumArticulationValidator
    {
        public static bool IsValid(DrumPad pad, DrumArticulation articulation)
        {
            if (!Enum.IsDefined(typeof(DrumPad), pad) ||
                !Enum.IsDefined(typeof(DrumArticulation), articulation))
                return false;
            if (articulation == DrumArticulation.Default) return true;

            switch (pad)
            {
                case DrumPad.Kick:
                    return false;
                case DrumPad.Snare:
                case DrumPad.Tom1:
                case DrumPad.Tom2:
                case DrumPad.FloorTom:
                    return articulation == DrumArticulation.Head || articulation == DrumArticulation.Rim;
                case DrumPad.HiHat:
                    return articulation == DrumArticulation.Bow ||
                           articulation == DrumArticulation.Edge ||
                           articulation == DrumArticulation.Closed ||
                           articulation == DrumArticulation.HalfOpen ||
                           articulation == DrumArticulation.Open ||
                           articulation == DrumArticulation.Pedal ||
                           articulation == DrumArticulation.Choke;
                case DrumPad.Crash:
                    return articulation == DrumArticulation.Bow ||
                           articulation == DrumArticulation.Edge ||
                           articulation == DrumArticulation.Choke;
                case DrumPad.Ride:
                    return articulation == DrumArticulation.Bow ||
                           articulation == DrumArticulation.Edge ||
                           articulation == DrumArticulation.Bell ||
                           articulation == DrumArticulation.Choke;
                default:
                    return false;
            }
        }

        public static void EnsureValid(DrumPad pad, DrumArticulation articulation)
        {
            if (!IsValid(pad, articulation))
                throw new ArgumentException($"Articulation '{articulation}' is not valid for drum pad '{pad}'.");
        }
    }
}
