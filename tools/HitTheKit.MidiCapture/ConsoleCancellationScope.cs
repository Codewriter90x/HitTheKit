namespace HitTheKit.MidiCapture;

public sealed class ConsoleCancellationScope : IDisposable
{
    private readonly CancellationTokenSource source = new();
    private bool disposed;

    public ConsoleCancellationScope() => Console.CancelKeyPress += Handle;

    public CancellationToken Token => source.Token;

    private void Handle(object? sender, ConsoleCancelEventArgs args)
    {
        args.Cancel = true;
        source.Cancel();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Console.CancelKeyPress -= Handle;
        source.Dispose();
    }
}
