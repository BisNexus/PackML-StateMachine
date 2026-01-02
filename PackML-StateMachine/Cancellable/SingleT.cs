using System.Collections.Concurrent;

namespace PackML_StateMachine.Threading;

/// <summary>
/// High-performance synchronous single-thread executor optimized for low latency.
/// Executes tasks sequentially on a dedicated background thread.
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
    // ConcurrentQueue is faster than BlockingCollection for our use case
    private readonly ConcurrentQueue<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _taskQueue = new();
    private readonly Thread _workerThread;
    private volatile CancellationTokenSource? _currentTaskCts;
    private readonly AutoResetEvent _taskAvailableEvent = new(false);
    private volatile bool _isShutdown;

    public SyncSingleThreadExecutor()
    {
        _workerThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Name = "SyncSingleThreadExecutor-Worker",
            Priority = ThreadPriority.Normal // Maximize scheduling priority
        };
        _workerThread.Start();
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        _taskQueue.Enqueue((task, cts, cancellableTask));
        _taskAvailableEvent.Set(); // Immediate signal

        return cancellableTask;
    }

    public void CancelCurrentTask()
    {
        _currentTaskCts?.Cancel(); // Lock-free volatile read
    }

    private void ProcessTasks()
    {
        var spinWait = new SpinWait();

        while (!_isShutdown)
        {
            // Try to dequeue immediately (hot path)
            if (_taskQueue.TryDequeue(out var item))
            {
                ExecuteTask(item);
                spinWait.Reset();
                continue;
            }

            // Spin before blocking (avoids context switch)
            if (spinWait.Count < 50)
            {
                spinWait.SpinOnce();
                continue;
            }

            // Block only after spinning fails
            spinWait.Reset();
            _taskAvailableEvent.WaitOne(20); // Short timeout for responsiveness
        }
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
        _taskAvailableEvent.Set();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();

        _taskAvailableEvent.Set();
        _workerThread.Join(TimeSpan.FromSeconds(2));
        _taskAvailableEvent.Dispose();

        while (_taskQueue.TryDequeue(out var item))
            item.Cts.Dispose();
    }
}