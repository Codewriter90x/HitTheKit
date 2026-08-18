using HitTheKit.Core;
using Xunit;

namespace HitTheKit.Core.Tests;

public sealed class PracticePerformanceAnalyzerTests
{
    [Fact]
    public void Reports_accuracy_and_timing_direction_per_pad()
    {
        var analyzer = new PracticePerformanceAnalyzer();
        analyzer.Record(DrumPad.Snare, HitGrade.Perfect);
        analyzer.Record(DrumPad.Snare, HitGrade.Early);
        analyzer.Record(DrumPad.Snare, HitGrade.Miss);

        PadPerformanceSnapshot value = analyzer.For(DrumPad.Snare);

        Assert.Equal(3, value.Resolved);
        Assert.Equal(2, value.Successful);
        Assert.Equal(66.667, value.Accuracy, 3);
        Assert.Equal(1, value.Early);
        Assert.Equal(1, value.Miss);
    }

    [Fact]
    public void Selects_the_pad_with_the_lowest_accuracy()
    {
        var analyzer = new PracticePerformanceAnalyzer();
        analyzer.Record(DrumPad.Kick, HitGrade.Perfect);
        analyzer.Record(DrumPad.Kick, HitGrade.Perfect);
        analyzer.Record(DrumPad.HiHat, HitGrade.Perfect);
        analyzer.Record(DrumPad.HiHat, HitGrade.Miss);

        PadPerformanceSnapshot weakest = Assert.IsType<PadPerformanceSnapshot>(analyzer.WeakestPad());
        Assert.Equal(DrumPad.HiHat, weakest.Pad);
    }

    [Fact]
    public void Reset_clears_aggregates()
    {
        var analyzer = new PracticePerformanceAnalyzer();
        analyzer.Record(DrumPad.Ride, HitGrade.Late);

        analyzer.Reset();

        Assert.Null(analyzer.WeakestPad());
        Assert.Equal(0, analyzer.LateCount);
    }
}
