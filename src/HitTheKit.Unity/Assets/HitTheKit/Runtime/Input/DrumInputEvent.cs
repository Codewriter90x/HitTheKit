using System;
using HitTheKit.Core;

namespace HitTheKit.Unity.Input
{
    public enum DrumInputSource
    {
        Keyboard = 0,
        Midi = 1,
        Test = 2
    }

    public readonly struct DrumInputEvent
    {
        public DrumInputEvent(
            DrumPad pad,
            int velocity,
            double songTimeSeconds,
            DrumInputSource source = DrumInputSource.Keyboard,
            DrumArticulation articulation = DrumArticulation.Default)
        {
            if (!Enum.IsDefined(typeof(DrumPad), pad))
            {
                throw new ArgumentOutOfRangeException(nameof(pad), "The drum pad is not supported.");
            }

            if (velocity < 0 || velocity > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(velocity), "Velocity must be between 0 and 127.");
            }

            if (double.IsNaN(songTimeSeconds) || double.IsInfinity(songTimeSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(songTimeSeconds), "Song time must be finite.");
            }

            if (!Enum.IsDefined(typeof(DrumInputSource), source))
            {
                throw new ArgumentOutOfRangeException(nameof(source), "The drum input source is not supported.");
            }

            DrumArticulationValidator.EnsureValid(pad, articulation);

            Pad = pad;
            Velocity = velocity;
            SongTimeSeconds = songTimeSeconds;
            Source = source;
            Articulation = articulation;
        }

        public DrumPad Pad { get; }
        public int Velocity { get; }
        public double SongTimeSeconds { get; }
        public DrumInputSource Source { get; }
        public DrumArticulation Articulation { get; }

        public DrumInputEvent WithSongTime(double songTimeSeconds) =>
            new DrumInputEvent(Pad, Velocity, songTimeSeconds, Source, Articulation);
    }
}
