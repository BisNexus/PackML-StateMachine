using System.Collections.Concurrent;

namespace PackML_StateMachine.Threading;

/// <summary>
/// High-performance synchronous single-thread executor optimized for low latency.
/// Executes tasks sequentially on a dedicated background thread.
/// </summary>
public interface ISyncSingleThreadExecutor : IDisposable
{
    CancellableTask Submit(Action<CancellationToken> task);
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
    private readonly ConcurrentQueue<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _taskQueue = new();
    //private readonly Thread _workerThread;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _workerTask;
    private readonly ManualResetEventSlim _taskAvailableEvent = new(false);
    private volatile bool _isShutdown;

    public SyncSingleThreadExecutor()
    {
        _workerTask = Task.Factory.StartNew(
            () => ProcessTasks(_shutdownCts.Token),
            _shutdownCts.Token,
            TaskCreationOptions.LongRunning, // dedicated worker-style thread
            TaskScheduler.Default);
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        _taskQueue.Enqueue((task, cts, cancellableTask));
        _taskAvailableEvent.Set();

        return cancellableTask;
    }

    private void ProcessTasks(CancellationToken shutdownToken)
    {
        while (!shutdownToken.IsCancellationRequested)
        {
            while (_taskQueue.TryDequeue(out var item))
            {
                ExecuteTask(item);
            }

            if (shutdownToken.IsCancellationRequested)
                break;

            _taskAvailableEvent.Wait();
            _taskAvailableEvent.Reset();
        }
    }

    private void ExecuteTask((Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task) item)
    {
        var (action, cts, cancellableTask) = item;

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
        /*
        finally
        {
            _currentTaskCts = null;
            cancellableTask.MarkDisposed();
            cts.Dispose();
        }
        */
    }

    public void Shutdown()
    {
        _isShutdown = true;
        _shutdownCts.Cancel();
        _taskAvailableEvent.Set();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();

        _workerTask.Dispose();
        _taskAvailableEvent.Dispose();

        while (_taskQueue.TryDequeue(out var item))
            item.Cts.Dispose();
    }
}