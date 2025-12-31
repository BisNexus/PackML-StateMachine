namespace PackML_StateMachine.Threading;

internal abstract class CancellableTaskItem
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public bool IsCancelled => CancellationTokenSource.Token.IsCancellationRequested;

    public abstract void Execute();
    public virtual void Cancel()
    {
        if(IsCancelled)
            throw new Exception("Task was aleady cancelled");
        CancellationTokenSource.Cancel();
    }
}

internal class CancellableTaskItem<T> : CancellableTaskItem
{
    private readonly Func<CancellationToken, T> _task;
    private readonly TaskCompletionSource<T> _completionSource;

    public CancellableTaskItem(Func<CancellationToken, T> task, TaskCompletionSource<T> completionSource)
    {
        _task = task;
        _completionSource = completionSource;
    }

    public override void Execute()
    {
        if (IsCancelled)
        {
            _completionSource?.SetCanceled();
            return;
        }

        try
        {
            T? result = _task(CancellationTokenSource.Token);
            _completionSource?.SetResult(result);
        }
        catch (OperationCanceledException)
        {
            _completionSource?.SetCanceled();
        }
        catch (Exception ex)
        {
            _completionSource?.SetException(ex);
        }
    }
}

internal class CancellableActionTaskItem : CancellableTaskItem
{
    private readonly Action<CancellationToken> _action;
    private readonly TaskCompletionSource<object> _completionSource;

    public CancellableActionTaskItem(Action<CancellationToken> action, TaskCompletionSource<object> completionSource)
    {
        _action = action;
        _completionSource = completionSource;
    }

    public override void Execute()
    {
        if (IsCancelled)
        {
            _completionSource?.SetCanceled();
            return;
        }

        try
        {
            _action(CancellationTokenSource.Token);
            _completionSource?.SetResult(null);
        }
        catch (OperationCanceledException)
        {
            _completionSource?.SetCanceled();
        }
        catch (Exception ex)
        {
            _completionSource?.SetException(ex);
        }
    }
}