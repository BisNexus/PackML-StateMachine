using System.Collections.Concurrent;

namespace PackML_StateMachine.Threading;

/// <summary>
/// A synchronous single-thread executor similar to Java's Executors.newSingleThreadExecutor().
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
            // CTS was already disposed, ignore
        }
    }

    public bool IsCancelled => _cts.IsCancellationRequested;

    internal void MarkDisposed() => _isDisposed = true;
}

public class SyncSingleThreadExecutor : ISyncSingleThreadExecutor
{
    private readonly BlockingCollection<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _taskQueue = new();
    private readonly Thread _workerThread;
    private CancellationTokenSource _currentTaskCts;
    private readonly object _lock = new();

    public SyncSingleThreadExecutor()
    {
        _workerThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Name = "SyncSingleThreadExecutor-Worker"
        };
        _workerThread.Start();
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);
        _taskQueue.Add((task, cts, cancellableTask));
        return cancellableTask;
    }

    public void CancelCurrentTask()
    {
        //lock (_lock)
        //{
            _currentTaskCts?.Cancel();
        //}
    }

    private void ProcessTasks()
    {
        foreach (var (action, cts, cancellableTask) in _taskQueue.GetConsumingEnumerable())
        {
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
                // Task was cancelled, continue to next
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task execution error: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _currentTaskCts = null;
                }
                cancellableTask.MarkDisposed();
                cts.Dispose();
            }
        }
    }

    public void Shutdown()
    {
        _taskQueue.CompleteAdding();
    }

    public void Dispose()
    {
        Shutdown();
        _workerThread.Join(TimeSpan.FromSeconds(2));
        _taskQueue.Dispose();
    }
}