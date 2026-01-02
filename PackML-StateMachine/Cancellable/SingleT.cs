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
    private volatile CancellationTokenSource? _currentTaskCts;
    private readonly ManualResetEventSlim _taskAvailableEvent = new ManualResetEventSlim(false);
    private volatile bool _isShutdown;

    public SyncSingleThreadExecutor()
    {
        _workerThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Name = "SyncSingleThreadExecutor-Worker",
            Priority = ThreadPriority.AboveNormal // Higher priority for better scheduling under load
        };
        _workerThread.Start();
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);
        
        _taskQueue.Add((task, cts, cancellableTask));
        _taskAvailableEvent.Set(); // Signal immediately for faster wake-up
        
        return cancellableTask;
    }

    public void CancelCurrentTask()
    {
        // Use volatile read - no lock needed, reduces contention
        var currentCts = _currentTaskCts;
        currentCts?.Cancel();
    }

    private void ProcessTasks()
    {
        var spinWait = new SpinWait();
        
        while (!_isShutdown)
        {
            try
            {
                // Optimistic spinning before blocking - reduces context switches
                while (_taskQueue.Count == 0 && !_isShutdown)
                {
                    if (spinWait.Count < 10) // Spin for a short time
                    {
                        spinWait.SpinOnce();
                    }
                    else
                    {
                        // Reset spin and wait on event
                        spinWait.Reset();
                        _taskAvailableEvent.Wait(100); // Wait with timeout to check shutdown
                        _taskAvailableEvent.Reset();
                        break;
                    }
                }

                if (_isShutdown)
                    break;

                if (_taskQueue.TryTake(out var item, 0))
                {
                    var (action, cts, cancellableTask) = item;
                    
                    // Volatile write for visibility
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
                        // Volatile write to null
                        _currentTaskCts = null;
                        cancellableTask.MarkDisposed();
                        cts.Dispose();
                        spinWait.Reset(); // Reset for next iteration
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessTasks error: {ex.Message}");
            }
        }
    }

    public void Shutdown()
    {
        _isShutdown = true;
        _taskQueue.CompleteAdding();
        _taskAvailableEvent.Set(); // Wake up worker thread
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();
            
        _taskAvailableEvent.Set(); // Ensure thread wakes up
        _workerThread.Join(TimeSpan.FromSeconds(2));
        _taskQueue.Dispose();
        _taskAvailableEvent.Dispose();
    }
}