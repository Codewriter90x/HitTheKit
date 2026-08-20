using System;
using HitTheKit.Core;
using Xunit;

namespace HitTheKit.Core.Tests;

public sealed class PerformanceGhostReplayTests
{
    [Fact]
    public void Commits_a_defensive_time_ordered_ghost_and_starts_a_fresh_take()
    {
        var replay = new PerformanceGhostReplay();
        Assert.True(replay.Record(2, DrumPad.Snare, 90, HitGrade.Good));
        Assert.True(replay.Record(1, DrumPad.Kick, 110, HitGrade.Perfect));

        Assert.True(replay.CommitCurrentTake());
        Assert.Equal(0, replay.CurrentHitCount);
        Assert.Equal(2, replay.Ghost.Count);
        Assert.Equal(DrumPad.Kick, replay.Ghost[0].Pad);
        Assert.Equal(DrumPad.Snare, replay.Ghost[1].Pad);
        Assert.True(replay.HasGhost);
    }

    [Fact]
    public void Ignores_count_in_and_empty_commits_preserve_existing_ghost()
    {
        var replay = new PerformanceGhostReplay();
        Assert.False(replay.Record(-0.1, DrumPad.Kick, 100, null));
        Assert.True(replay.Record(1, DrumPad.Kick, 100, null));
        Assert.True(replay.CommitCurrentTake());
        Assert.False(replay.CommitCurrentTake());
        Assert.Single(replay.Ghost);
    }

    [Fact]
    public void Reset_current_does_not_clear_the_committed_ghost()
    {
        var replay = new PerformanceGhostReplay();
        replay.Record(1, DrumPad.Ride, 80, HitGrade.Late);
        replay.CommitCurrentTake();
        replay.Record(2, DrumPad.Snare, 100, HitGrade.Perfect);
        replay.ResetCurrent();

        Assert.Equal(0, replay.CurrentHitCount);
        Assert.Single(replay.Ghost);
        replay.ClearGhost();
        Assert.False(replay.HasGhost);
    }

    [Fact]
    public void Rejects_invalid_hits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GhostReplayHit(double.NaN, DrumPad.Kick, 100, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GhostReplayHit(1, (DrumPad)99, 100, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GhostReplayHit(1, DrumPad.Kick, 128, null));
    }
}
