namespace FlahaGrow.Core.Operations;

/// <summary>An action held True fires once; restored documents must first observe False.</summary>
public sealed class ActionLatch
{
    private bool previous;
    public bool Observe(bool value)
    {
        var fire = value && !previous;
        previous = value;
        return fire;
    }
    public void Disarm() => previous = true;
}

public sealed record OperationResult<T>(T? Value, string? Error, bool Cancelled) where T : class;

/// <summary>Owner-thread controller; serialized work avoids accumulating blocked filesystem operations.</summary>
public sealed class SetupOperation<T> where T : class
{
    private CancellationTokenSource? cancellation;
    public Task<OperationResult<T>>? Current { get; private set; }
    public long Revision { get; private set; }

    public Task<OperationResult<T>> Start(Func<CancellationToken, Task<T>> work)
    {
        Cancel();
        var previous = Current;
        var source = new CancellationTokenSource();
        cancellation = source;
        Current = Task.Run(async () =>
        {
            try
            {
                if (previous is not null) await previous.ConfigureAwait(false);
                source.Token.ThrowIfCancellationRequested();
                var value = await work(source.Token).ConfigureAwait(false);
                source.Token.ThrowIfCancellationRequested();
                return new OperationResult<T>(value, null, false);
            }
            catch (OperationCanceledException) { return new(null, null, true); }
            catch (Exception ex) { return new(null, ex.Message, false); }
        });
        return Current;
    }

    public void Cancel()
    {
        Revision++;
        if (cancellation is null) return;
        var source = cancellation;
        cancellation = null;
        source.Cancel();
        if (Current is null || Current.IsCompleted) source.Dispose();
        else _ = Current.ContinueWith(_ => source.Dispose(), TaskScheduler.Default);
    }
}
