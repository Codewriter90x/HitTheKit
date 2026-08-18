namespace HitTheKit.MidiCapture;

public enum GuidedDecision
{
    Accept,
    Retry,
    Skip,
    Finish
}

public sealed record GuidedCaptureResult(
    IReadOnlyList<CaptureEvent> Events,
    IReadOnlyList<CaptureStepState> Steps,
    bool FinishedEarly);

public static class GuidedCaptureWorkflow
{
    public static readonly IReadOnlyList<CaptureStepDefinition> DefaultSteps = Array.AsReadOnly(new[]
    {
        Step("kick", "Kick"),
        Step("snare-center", "Snare center"),
        Step("snare-rim", "Snare rim"),
        Step("tom-1", "Tom 1"),
        Step("tom-2", "Tom 2"),
        Step("floor-tom", "Floor tom"),
        Step("crash-1", "Crash 1"),
        Step("crash-2-optional", "Crash 2", optional: true),
        Step("ride-bow", "Ride bow"),
        Step("ride-bell-optional", "Ride bell", optional: true),
        Step("hihat-closed", "Hi-hat closed"),
        Step("hihat-open", "Hi-hat open"),
        Step("hihat-pedal", "Hi-hat pedal"),
        Step("hihat-continuous", "Hi-hat continuous controller", duration: 8),
        Step("crash-choke", "Crash choke", samples: 3),
        Step("ride-choke-optional", "Ride choke", optional: true, samples: 3),
        Step("free-play", "Free play", duration: 20)
    });

    public static async Task<GuidedCaptureResult> RunAsync(
        IReadOnlyList<CaptureStepDefinition> steps,
        Func<CaptureStepDefinition, int, CancellationToken, Task<IReadOnlyList<CaptureEvent>>> captureAttempt,
        Func<CaptureStepDefinition, IReadOnlyList<CaptureEvent>, CancellationToken, Task<GuidedDecision>> decide,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(captureAttempt);
        ArgumentNullException.ThrowIfNull(decide);
        var events = new List<CaptureEvent>();
        var states = new List<CaptureStepState>();
        bool finishedEarly = false;

        foreach (CaptureStepDefinition step in steps)
        {
            int attempt = 1;
            int stepEventCount = 0;
            while (true)
            {
                IReadOnlyList<CaptureEvent> captured = await captureAttempt(step, attempt, cancellationToken);
                events.AddRange(captured);
                stepEventCount += captured.Count;
                GuidedDecision decision = await decide(step, captured, cancellationToken);
                if (decision == GuidedDecision.Retry) { attempt++; continue; }
                if (decision == GuidedDecision.Skip)
                {
                    states.Add(new(step.Id, false, true, stepEventCount));
                    break;
                }
                if (decision == GuidedDecision.Finish)
                {
                    states.Add(new(step.Id, captured.Count > 0, captured.Count == 0, stepEventCount));
                    finishedEarly = true;
                    return new(events.AsReadOnly(), states.AsReadOnly(), finishedEarly);
                }
                states.Add(new(step.Id, true, false, stepEventCount));
                break;
            }
        }
        return new(events.AsReadOnly(), states.AsReadOnly(), finishedEarly);
    }

    public static IReadOnlyList<CaptureStepDefinition> WithOverrides(int samples, double freePlayDuration)
    {
        if (samples <= 0) throw new ArgumentOutOfRangeException(nameof(samples));
        if (!double.IsFinite(freePlayDuration) || freePlayDuration <= 0) throw new ArgumentOutOfRangeException(nameof(freePlayDuration));
        return DefaultSteps.Select(step => step with
        {
            TargetSamples = step.SuggestedDurationSeconds.HasValue ? step.TargetSamples : step.Id.Contains("choke", StringComparison.Ordinal) ? Math.Min(samples, 3) : samples,
            SuggestedDurationSeconds = step.Id == "free-play" ? freePlayDuration : step.SuggestedDurationSeconds
        }).ToArray();
    }

    private static CaptureStepDefinition Step(string id, string name, bool optional = false, int samples = 5, double? duration = null) =>
        new(id, name, optional, samples, duration);
}
