using System;
using HitTheKit.Core;
using Xunit;

namespace HitTheKit.Core.Tests;

public sealed class GuidedLatencySoundCheckTests
{
    [Fact]
    public void Records_only_selected_source_and_recommends_median_offset()
    {
        var check = new GuidedLatencySoundCheck(8);
        check.Begin(GuidedSoundCheckInput.Midi, 10);

        for (int index = 0; index < 8; index++)
        {
            Assert.False(check.TryRecord(GuidedSoundCheckInput.Keyboard, 10 + index * 0.5 + 0.02));
            Assert.True(check.TryRecord(GuidedSoundCheckInput.Midi, 10 + index * 0.5 + 0.02));
        }

        GuidedSoundCheckSnapshot snapshot = check.Snapshot;
        Assert.Equal(GuidedSoundCheckState.Complete, snapshot.State);
        Assert.Equal(8, snapshot.AcceptedCount);
        Assert.True(snapshot.CanApplyRecommendation);
        Assert.Equal(0.03, check.RecommendOffsetSeconds(0.01), 6);
    }

    [Fact]
    public void Misses_expired_targets_without_using_outliers()
    {
        var check = new GuidedLatencySoundCheck(8);
        check.Begin(GuidedSoundCheckInput.Keyboard, 5);

        check.Advance(5.19);
        Assert.Equal(1, check.Snapshot.MissedCount);
        Assert.False(check.TryRecord(GuidedSoundCheckInput.Keyboard, 5.2));
        check.Advance(20);

        GuidedSoundCheckSnapshot snapshot = check.Snapshot;
        Assert.Equal(GuidedSoundCheckState.Complete, snapshot.State);
        Assert.Equal(8, snapshot.MissedCount);
        Assert.False(snapshot.CanApplyRecommendation);
        Assert.Throws<InvalidOperationException>(() => check.RecommendOffsetSeconds(0));
    }

    [Fact]
    public void Begin_and_reset_clear_previous_run()
    {
        var check = new GuidedLatencySoundCheck(8);
        check.Begin(GuidedSoundCheckInput.Keyboard, 1);
        Assert.True(check.TryRecord(GuidedSoundCheckInput.Keyboard, 1.01));

        check.Begin(GuidedSoundCheckInput.Midi, 20, 1);
        Assert.Equal(0, check.Snapshot.AcceptedCount);
        Assert.Equal(20, check.Snapshot.NextTargetTimeSeconds);
        check.Reset();
        Assert.Equal(GuidedSoundCheckState.Idle, check.Snapshot.State);
        Assert.Null(check.Snapshot.NextTargetTimeSeconds);
    }

    [Fact]
    public void Rejects_invalid_schedule_values()
    {
        var check = new GuidedLatencySoundCheck();
        Assert.Throws<ArgumentOutOfRangeException>(() => check.Begin(GuidedSoundCheckInput.Keyboard, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => check.Begin(GuidedSoundCheckInput.Keyboard, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => check.Begin((GuidedSoundCheckInput)99, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => check.Advance(double.PositiveInfinity));
    }
}
