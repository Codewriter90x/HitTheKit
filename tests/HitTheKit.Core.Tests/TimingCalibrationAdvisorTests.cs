using System;
using HitTheKit.Core;
using Xunit;

namespace HitTheKit.Core.Tests;

public sealed class TimingCalibrationAdvisorTests
{
    [Fact]
    public void Requires_eight_matched_hits_before_recommending_an_offset()
    {
        var advisor = new TimingCalibrationAdvisor();
        for (int index = 0; index < 7; index++) advisor.Add(0.030);

        Assert.False(advisor.Snapshot.HasRecommendation);
        Assert.Equal(0.010, advisor.RecommendOffsetSeconds(0.010), 6);

        advisor.Add(0.030);

        Assert.True(advisor.Snapshot.HasRecommendation);
        Assert.Equal(0.040, advisor.RecommendOffsetSeconds(0.010), 6);
    }

    [Fact]
    public void Median_is_robust_to_a_single_outlier()
    {
        var advisor = new TimingCalibrationAdvisor();
        foreach (double delta in new[] { 0.020, 0.021, 0.019, 0.020, 0.018, 0.022, 0.020, 0.140 })
            advisor.Add(delta);

        Assert.Equal(0.020, advisor.Snapshot.MedianDeltaSeconds, 6);
        Assert.InRange(advisor.Snapshot.MedianAbsoluteDeviationSeconds, 0, 0.003);
    }

    [Fact]
    public void Recommendation_is_clamped_to_the_supported_range()
    {
        var advisor = new TimingCalibrationAdvisor();
        for (int index = 0; index < 8; index++) advisor.Add(0.100);

        Assert.Equal(0.250, advisor.RecommendOffsetSeconds(0.220), 6);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.151)]
    public void Rejects_invalid_or_unmatched_deltas(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimingCalibrationAdvisor().Add(value));
    }
}
