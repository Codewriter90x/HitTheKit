using System.Diagnostics;
using System.Threading.Channels;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace HitTheKit.MidiCapture;

public sealed class SequenceGenerator
{
    private long value;
    public long Next() => Interlocked.Increment(ref value);
}

public sealed class CaptureIngress
{
    private readonly object gate = new();
    private readonly SequenceGenerator sequence;
    private readonly Channel<CaptureEvent> channel = Channel.CreateUnbounded<CaptureEvent>(new()
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly List<CaptureEvent> events = new();
    private bool accepting = true;

    public CaptureIngress(SequenceGenerator? sequence = null) => this.sequence = sequence ?? new SequenceGenerator();

    public ChannelReader<CaptureEvent> Reader => channel.Reader;

    public bool TryAccept(Func<long, CaptureEvent> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        lock (gate)
        {
            if (!accepting) return false;
            CaptureEvent value = create(sequence.Next());
            if (!channel.Writer.TryWrite(value)) throw new InvalidOperationException("The capture queue rejected an accepted event.");
            events.Add(value);
            return true;
        }
    }

    public void Complete()
    {
        lock (gate)
        {
            if (!accepting) return;
            accepting = false;
            channel.Writer.TryComplete();
        }
    }

    public IReadOnlyList<CaptureEvent> Snapshot()
    {
        lock (gate) return events.ToArray();
    }
}

public sealed record LiveCaptureResult(IReadOnlyList<CaptureEvent> Events, double StartSeconds, double EndSeconds, IReadOnlyList<string> Errors);

public static class MidiCaptureRunner
{
    public static async Task<LiveCaptureResult> RunAsync(
        InputDevice device,
        TimeSpan? duration,
        Func<CaptureEvent, ValueTask> consume,
        Func<string?> stepProvider,
        CancellationToken cancellationToken,
        SequenceGenerator? sequence = null,
        Func<int?>? stepAttemptProvider = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(consume);
        ArgumentNullException.ThrowIfNull(stepProvider);
        sequence ??= new SequenceGenerator();
        var stopwatch = Stopwatch.StartNew();
        var ingress = new CaptureIngress(sequence);
        var errors = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (duration.HasValue) linked.CancelAfter(duration.Value);

        EventHandler<MidiEventReceivedEventArgs> handler = (_, args) =>
        {
            try
            {
                ingress.TryAccept(nextSequence => MidiEventAdapter.Adapt(
                    args.Event,
                    nextSequence,
                    stopwatch.Elapsed.TotalSeconds,
                    stepProvider(),
                    stepAttemptProvider?.Invoke()).Capture);
            }
            catch (Exception exception)
            {
                errors.Enqueue(exception.Message);
            }
        };
        EventHandler<ErrorOccurredEventArgs> errorHandler = (_, args) => errors.Enqueue(args.Exception.GetType().Name);
        Task consumer = Task.Run(async () =>
        {
            await foreach (CaptureEvent value in ingress.Reader.ReadAllAsync()) await consume(value);
        });

        device.SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOn;
        device.EventReceived += handler;
        device.ErrorOccurred += errorHandler;
        try
        {
            device.StartEventsListening();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, linked.Token); }
            catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
        }
        finally
        {
            if (device.IsListeningForEvents) device.StopEventsListening();
            device.EventReceived -= handler;
            device.ErrorOccurred -= errorHandler;
            ingress.Complete();
            await consumer;
            stopwatch.Stop();
        }
        return new(ingress.Snapshot(), 0, stopwatch.Elapsed.TotalSeconds, errors.ToArray());
    }
}
