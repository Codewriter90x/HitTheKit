namespace HitTheKit.Core.Tests;

public sealed class HitMatcherTests
{
    [Fact]
    public void Exact_hit_is_perfect()
    {
        var note = new ChartNote(1.000, DrumPad.Snare);

        HitResult result = Match(
            new HitMatcher(TimingWindows.Default),
            new[] { note },
            new DrumHit(DrumPad.Snare, 1.000));

        Assert.Equal(HitGrade.Perfect, result.Grade);
        Assert.Equal(0, result.DeltaSeconds);
        Assert.Same(note, result.Note);
        Assert.True(result.Hit.HasValue);
    }

    [Fact]
    public void Hit_before_note_within_perfect_window_is_perfect()
    {
        HitResult result = MatchSingleNote(noteTime: 1.000, hitTime: 0.980);

        Assert.Equal(HitGrade.Perfect, result.Grade);
        Assert.True(result.DeltaSeconds < 0);
    }

    [Fact]
    public void Hit_after_note_within_perfect_window_is_perfect()
    {
        HitResult result = MatchSingleNote(noteTime: 1.000, hitTime: 1.020);

        Assert.Equal(HitGrade.Perfect, result.Grade);
        Assert.True(result.DeltaSeconds > 0);
    }

    [Fact]
    public void Hit_in_good_band_is_good()
    {
        HitResult result = MatchSingleNote(noteTime: 1.000, hitTime: 1.060);

        Assert.Equal(HitGrade.Good, result.Grade);
    }

    [Fact]
    public void Hit_before_note_in_outer_band_is_early()
    {
        HitResult result = MatchSingleNote(noteTime: 1.000, hitTime: 0.880);

        Assert.Equal(HitGrade.Early, result.Grade);
        Assert.Equal(-0.120, result.DeltaSeconds!.Value, precision: 6);
    }

    [Fact]
    public void Hit_after_note_in_outer_band_is_late()
    {
        HitResult result = MatchSingleNote(noteTime: 1.000, hitTime: 1.120);

        Assert.Equal(HitGrade.Late, result.Grade);
        Assert.Equal(0.120, result.DeltaSeconds!.Value, precision: 6);
    }

    [Theory]
    [InlineData(0.849)]
    [InlineData(1.151)]
    public void Hit_outside_window_does_not_resolve_note(double invalidHitTime)
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);
        ChartNote[] notes = { note };

        bool outsideMatched = matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Snare, invalidHitTime),
            out HitResult? outsideResult);
        bool validMatched = matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Snare, 1.000),
            out HitResult? validResult);

        Assert.False(outsideMatched);
        Assert.Null(outsideResult);
        Assert.True(validMatched);
        Assert.NotNull(validResult);
    }

    [Fact]
    public void Hit_on_wrong_pad_does_not_resolve_note()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);
        ChartNote[] notes = { note };

        bool wrongPadMatched = matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Kick, 1.000),
            out HitResult? wrongPadResult);
        bool correctPadMatched = matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Snare, 1.000),
            out HitResult? correctPadResult);

        Assert.False(wrongPadMatched);
        Assert.Null(wrongPadResult);
        Assert.True(correctPadMatched);
        Assert.NotNull(correctPadResult);
    }

    [Fact]
    public void Articulation_specific_note_rejects_a_different_zone_without_resolving_it()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Ride, 110, DrumArticulation.Bell);
        ChartNote[] notes = { note };

        Assert.False(matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Ride, 1.000, 110, DrumArticulation.Bow),
            out HitResult? wrongZone));
        Assert.Null(wrongZone);
        Assert.True(matcher.TryMatch(
            notes,
            new DrumHit(DrumPad.Ride, 1.000, 110, DrumArticulation.Bell),
            out HitResult? correctZone));
        Assert.NotNull(correctZone);
    }

    [Fact]
    public void Default_articulation_remains_a_backward_compatible_wildcard()
    {
        var note = new ChartNote(1.000, DrumPad.Snare);

        HitResult result = Match(
            new HitMatcher(TimingWindows.Default),
            new[] { note },
            new DrumHit(DrumPad.Snare, 1.000, 100, DrumArticulation.Rim));

        Assert.Equal(HitGrade.Perfect, result.Grade);
    }

    [Fact]
    public void Chart_note_rejects_invalid_velocity_and_pad_articulation_pairs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartNote(1, DrumPad.Snare, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartNote(1, DrumPad.Snare, 128));
        Assert.Throws<ArgumentException>(() =>
            new ChartNote(1, DrumPad.Kick, 100, DrumArticulation.Bell));
    }

    [Fact]
    public void Expired_note_is_miss()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);

        bool marked = matcher.TryMarkMissed(note, 1.151, out HitResult? result);

        Assert.True(marked);
        HitResult miss = Assert.IsType<HitResult>(result);
        Assert.Equal(HitGrade.Miss, miss.Grade);
        Assert.Same(note, miss.Note);
        Assert.False(miss.Hit.HasValue);
        Assert.False(miss.DeltaSeconds.HasValue);
    }

    [Fact]
    public void Duplicate_hit_does_not_resolve_same_note_twice()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);
        ChartNote[] notes = { note };
        var hit = new DrumHit(DrumPad.Snare, 1.000);

        bool firstMatched = matcher.TryMatch(notes, hit, out HitResult? firstResult);
        bool secondMatched = matcher.TryMatch(notes, hit, out HitResult? secondResult);

        Assert.True(firstMatched);
        Assert.NotNull(firstResult);
        Assert.False(secondMatched);
        Assert.Null(secondResult);
    }

    [Fact]
    public void Distinct_notes_with_same_values_resolve_independently_in_list_order()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var first = new ChartNote(1.000, DrumPad.Snare);
        var second = new ChartNote(1.000, DrumPad.Snare);
        ChartNote[] notes = { first, second };
        var hit = new DrumHit(DrumPad.Snare, 1.000);

        HitResult firstResult = Match(matcher, notes, hit);
        HitResult secondResult = Match(matcher, notes, hit);

        Assert.Same(first, firstResult.Note);
        Assert.Same(second, secondResult.Note);
        Assert.True(matcher.IsResolved(first));
        Assert.True(matcher.IsResolved(second));
    }

    [Fact]
    public void Duplicate_miss_does_not_resolve_same_note_twice()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);

        bool firstMarked = matcher.TryMarkMissed(note, 1.151, out HitResult? firstResult);
        bool secondMarked = matcher.TryMarkMissed(note, 2.000, out HitResult? secondResult);

        Assert.True(firstMarked);
        Assert.NotNull(firstResult);
        Assert.False(secondMarked);
        Assert.Null(secondResult);
    }

    [Fact]
    public void Empty_chart_does_not_match()
    {
        var matcher = new HitMatcher(TimingWindows.Default);

        bool matched = matcher.TryMatch(
            Array.Empty<ChartNote>(),
            new DrumHit(DrumPad.Snare, 1.000),
            out HitResult? result);

        Assert.False(matched);
        Assert.Null(result);
    }

    [Fact]
    public void Positive_offset_moves_note_later()
    {
        var matcher = new HitMatcher(TimingWindows.Default, offsetSeconds: 0.250);
        var note = new ChartNote(1.000, DrumPad.Snare);

        HitResult result = Match(
            matcher,
            new[] { note },
            new DrumHit(DrumPad.Snare, 1.250));

        Assert.Equal(HitGrade.Perfect, result.Grade);
        Assert.Equal(0, result.DeltaSeconds);
    }

    [Fact]
    public void Negative_offset_moves_note_earlier()
    {
        var matcher = new HitMatcher(TimingWindows.Default, offsetSeconds: -0.250);
        var note = new ChartNote(1.000, DrumPad.Snare);

        HitResult result = Match(
            matcher,
            new[] { note },
            new DrumHit(DrumPad.Snare, 0.750));

        Assert.Equal(HitGrade.Perfect, result.Grade);
        Assert.Equal(0, result.DeltaSeconds);
    }

    [Fact]
    public void Exact_perfect_boundary_is_perfect()
    {
        HitResult result = MatchSingleNote(noteTime: 0, hitTime: 0.040);

        Assert.Equal(HitGrade.Perfect, result.Grade);
    }

    [Fact]
    public void Exact_good_boundary_is_good()
    {
        HitResult result = MatchSingleNote(noteTime: 0, hitTime: 0.090);

        Assert.Equal(HitGrade.Good, result.Grade);
    }

    [Theory]
    [InlineData(-0.150, HitGrade.Early)]
    [InlineData(0.150, HitGrade.Late)]
    public void Exact_hit_boundary_is_match(double hitTime, HitGrade expectedGrade)
    {
        HitResult result = MatchSingleNote(noteTime: 0, hitTime: hitTime);

        Assert.Equal(expectedGrade, result.Grade);
    }

    [Theory]
    [InlineData(-0.001, 0.090, 0.150)]
    [InlineData(0.040, -0.001, 0.150)]
    [InlineData(0.040, 0.090, -0.001)]
    public void Negative_timing_window_is_rejected(
        double perfectSeconds,
        double goodSeconds,
        double hitSeconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimingWindows(perfectSeconds, goodSeconds, hitSeconds));
    }

    [Theory]
    [InlineData(0.100, 0.090, 0.150)]
    [InlineData(0.040, 0.160, 0.150)]
    public void Invalid_timing_window_order_is_rejected(
        double perfectSeconds,
        double goodSeconds,
        double hitSeconds)
    {
        Assert.Throws<ArgumentException>(() =>
            new TimingWindows(perfectSeconds, goodSeconds, hitSeconds));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Non_finite_timing_window_is_rejected(double invalidValue)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimingWindows(invalidValue, 0.090, 0.150));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimingWindows(0.040, invalidValue, 0.150));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimingWindows(0.040, 0.090, invalidValue));
    }

    [Fact]
    public void Closest_candidate_on_same_pad_is_selected()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var earlier = new ChartNote(0.900, DrumPad.Snare);
        var closer = new ChartNote(1.020, DrumPad.Snare);

        HitResult result = Match(
            matcher,
            new[] { earlier, closer },
            new DrumHit(DrumPad.Snare, 1.000));

        Assert.Same(closer, result.Note);
        Assert.False(matcher.IsResolved(earlier));
    }

    [Fact]
    public void Equidistant_candidates_select_earlier_note()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var later = new ChartNote(1.125, DrumPad.Snare);
        var earlier = new ChartNote(0.875, DrumPad.Snare);

        HitResult result = Match(
            matcher,
            new[] { later, earlier },
            new DrumHit(DrumPad.Snare, 1.000));

        Assert.Same(earlier, result.Note);
        Assert.False(matcher.IsResolved(later));
    }

    [Fact]
    public void Simultaneous_notes_on_different_pads_resolve_independently()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var kick = new ChartNote(1.000, DrumPad.Kick);
        var snare = new ChartNote(1.000, DrumPad.Snare);
        ChartNote[] notes = { kick, snare };

        HitResult kickResult = Match(
            matcher,
            notes,
            new DrumHit(DrumPad.Kick, 1.000));
        HitResult snareResult = Match(
            matcher,
            notes,
            new DrumHit(DrumPad.Snare, 1.000));

        Assert.Same(kick, kickResult.Note);
        Assert.Same(snare, snareResult.Note);
    }

    [Fact]
    public void Hit_note_cannot_later_become_miss()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);
        Match(matcher, new[] { note }, new DrumHit(DrumPad.Snare, 1.000));

        bool marked = matcher.TryMarkMissed(note, 2.000, out HitResult? result);

        Assert.False(marked);
        Assert.Null(result);
    }

    [Fact]
    public void Missed_note_cannot_later_be_hit()
    {
        var matcher = new HitMatcher(TimingWindows.Default);
        var note = new ChartNote(1.000, DrumPad.Snare);
        Assert.True(matcher.TryMarkMissed(note, 1.151, out HitResult? miss));

        bool matched = matcher.TryMatch(
            new[] { note },
            new DrumHit(DrumPad.Snare, 1.000),
            out HitResult? result);

        Assert.NotNull(miss);
        Assert.False(matched);
        Assert.Null(result);
    }

    private static HitResult MatchSingleNote(double noteTime, double hitTime)
    {
        var note = new ChartNote(noteTime, DrumPad.Snare);
        return Match(
            new HitMatcher(TimingWindows.Default),
            new[] { note },
            new DrumHit(DrumPad.Snare, hitTime));
    }

    private static HitResult Match(
        HitMatcher matcher,
        IReadOnlyList<ChartNote> notes,
        DrumHit hit)
    {
        bool matched = matcher.TryMatch(notes, hit, out HitResult? result);

        Assert.True(matched);
        return Assert.IsType<HitResult>(result);
    }
}
