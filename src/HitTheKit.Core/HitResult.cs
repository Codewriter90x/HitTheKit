namespace HitTheKit.Core
{
    public sealed class HitResult
    {
        internal HitResult(
            HitGrade grade,
            ChartNote note,
            DrumHit? hit,
            double? deltaSeconds)
        {
            Grade = grade;
            Note = note;
            Hit = hit;
            DeltaSeconds = deltaSeconds;
        }

        public HitGrade Grade { get; }

        public ChartNote Note { get; }

        public DrumHit? Hit { get; }

        public double? DeltaSeconds { get; }
    }
}
