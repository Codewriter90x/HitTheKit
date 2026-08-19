using System;
using System.Linq;
using HitTheKit.Core;
using Xunit;

namespace HitTheKit.Core.Tests;

public sealed class PracticeErrorMapAnalyzerTests
{
    [Fact]
    public void Aggregates_weighted_accuracy_by_section_and_pad()
    {
        var analyzer = Analyzer();
        analyzer.Record(Result(1, DrumPad.Snare, HitGrade.Perfect));
        analyzer.Record(Result(2, DrumPad.Snare, HitGrade.Good));
        analyzer.Record(Result(5, DrumPad.Kick, HitGrade.Miss));

        PracticeErrorCell snare = analyzer.Snapshot().Single(cell => cell.Pad == DrumPad.Snare);
        PracticeErrorCell kick = analyzer.Snapshot().Single(cell => cell.Pad == DrumPad.Kick);
        Assert.Equal("BATTUTE 1–4", snare.Section.Label);
        Assert.Equal(87.5, snare.Accuracy, 3);
        Assert.Equal("BATTUTE 5–8", kick.Section.Label);
        Assert.Equal(0, kick.Accuracy);
        PracticeErrorCell weakest = Assert.IsType<PracticeErrorCell>(analyzer.Weakest());
        Assert.Equal(DrumPad.Kick, weakest.Pad);
        Assert.Equal(1, weakest.Section.Index);
    }

    [Fact]
    public void Uses_start_inclusive_end_exclusive_section_boundaries()
    {
        var analyzer = Analyzer();
        analyzer.Record(Result(4, DrumPad.HiHat, HitGrade.Late));

        Assert.Equal(1, analyzer.Snapshot().Single().Section.Index);
    }

    [Fact]
    public void Ignores_results_outside_defined_song_sections()
    {
        var analyzer = Analyzer();
        analyzer.Record(Result(9, DrumPad.Ride, HitGrade.Miss));
        Assert.Empty(analyzer.Snapshot());
    }

    [Fact]
    public void Snapshot_order_is_deterministic_and_reset_clears_cells()
    {
        var analyzer = Analyzer();
        analyzer.Record(Result(5, DrumPad.Snare, HitGrade.Miss));
        analyzer.Record(Result(1, DrumPad.Ride, HitGrade.Early));
        analyzer.Record(Result(1, DrumPad.Kick, HitGrade.Good));

        Assert.Equal(
            new[] { DrumPad.Kick, DrumPad.Ride, DrumPad.Snare },
            analyzer.Snapshot().Select(cell => cell.Pad));
        analyzer.Reset();
        Assert.Null(analyzer.Weakest());
    }

    [Fact]
    public void Rejects_gaps_and_unordered_section_indices()
    {
        Assert.Throws<ArgumentException>(() => new PracticeErrorMapAnalyzer(new[]
        {
            new PracticeSectionDefinition(0, "A", 0, 2),
            new PracticeSectionDefinition(1, "B", 3, 4)
        }));
        Assert.Throws<ArgumentException>(() => new PracticeErrorMapAnalyzer(new[]
        {
            new PracticeSectionDefinition(1, "A", 0, 2)
        }));
    }

    private static PracticeErrorMapAnalyzer Analyzer() => new PracticeErrorMapAnalyzer(new[]
    {
        new PracticeSectionDefinition(0, "BATTUTE 1–4", 0, 4),
        new PracticeSectionDefinition(1, "BATTUTE 5–8", 4, 8)
    });

    private static HitResult Result(double time, DrumPad pad, HitGrade grade)
    {
        var note = new ChartNote(time, pad);
        var matcher = new HitMatcher(new TimingWindows(0.03, 0.08, 0.18), 0);
        if (grade == HitGrade.Miss)
        {
            Assert.True(matcher.TryMarkMissed(note, time + 1, out HitResult? missed));
            return Assert.IsType<HitResult>(missed);
        }

        double delta = grade switch
        {
            HitGrade.Perfect => 0,
            HitGrade.Good => 0.05,
            HitGrade.Early => -0.1,
            HitGrade.Late => 0.1,
            _ => throw new ArgumentOutOfRangeException(nameof(grade))
        };
        Assert.True(matcher.TryMatch(new[] { note }, new DrumHit(pad, time + delta, 100), out HitResult? result));
        return Assert.IsType<HitResult>(result);
    }
}
