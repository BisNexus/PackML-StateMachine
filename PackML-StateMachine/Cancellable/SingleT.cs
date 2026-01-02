using System.Threading.Tasks.Dataflow;

namespace PackML_StateMachine.Threading;

/// <summary>
/// High-performance synchronous single-thread executor optimized for low latency.
/// Executes tasks sequentially using TPL Dataflow.
/// </summary>
public interface ISyncSingleThreadExecutor : IDisposable
{
    CancellableTask Submit(Action<CancellationToken> task);
    void CancelCurrentTask();
    void Shutdown();
}

public class CancellableTask
{
    private readonly CancellationTokenSource _cts;
    private volatile bool _isDisposed;

    internal CancellableTask(CancellationTokenSource cts)
    {
        _cts = cts;
    }

    public void Cancel()
    {
        if (_isDisposed)
            return;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public bool IsCancelled => _cts.IsCancellationRequested;

    internal void MarkDisposed() => _isDisposed = true;
}

public class SyncSingleThreadExecutor : ISyncSingleThreadExecutor
{
    private readonly ActionBlock<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _actionBlock;
    private volatile CancellationTokenSource? _currentTaskCts;
    private volatile bool _isShutdown;

    public SyncSingleThreadExecutor()
    {
        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1, // Ensures sequential execution
            BoundedCapacity = DataflowBlockOptions.Unbounded,
            EnsureOrdered = true // Maintains submission order
        };

        _actionBlock = new ActionBlock<(Action<CancellationToken>, CancellationTokenSource, CancellableTask)>(
            item => ExecuteTask(item),
            options);
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        if (!_actionBlock.Post((task, cts, cancellableTask)))
        {
            cts.Dispose();
            throw new InvalidOperationException("Failed to submit task - executor may be shutting down");
        }

        return cancellableTask;
    }

    public void CancelCurrentTask()
    {
        _currentTaskCts?.Cancel();
    }

    private void ExecuteTask((Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task) item)
    {
        var (action, cts, cancellableTask) = item;
        _currentTaskCts = cts;

        try
        {
            if (!cts.IsCancellationRequested)
            {
                action(cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Task execution error: {ex.Message}");
        }
        finally
        {
            _currentTaskCts = null;
            cancellableTask.MarkDisposed();
            cts.Dispose();
        }
    }

    public void Shutdown()
    {
        _isShutdown = true;
        _actionBlock.Complete();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();

        // Wait for all pending tasks to complete
        _actionBlock.Completion.Wait(TimeSpan.FromSeconds(2));
    }
}