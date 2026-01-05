using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Threading.Tasks.Dataflow;

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
            Priority = ThreadPriority.Normal
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
        _taskAvailableEvent.Set();

        return cancellableTask;
    }

    public void CancelCurrentTask()
    {
        _currentTaskCts?.Cancel();
    }

    private void ProcessTasks()
    {
        var spinWait = new SpinWait();

        while (!_isShutdown)
        {
            // Hot path: try immediate dequeue
            if (_taskQueue.TryDequeue(out var item))
            {
                ExecuteTask(item);
                spinWait.Reset();
                continue;
            }

            // Aggressive spinning before blocking
            if (spinWait.Count < 100) // Increased from 50
            {
                spinWait.SpinOnce();
                continue;
            }

            // Block only after extensive spinning
            spinWait.Reset();
            _taskAvailableEvent.WaitOne(10); // Reduced from 20ms
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

public class RxSyncSingleThreadExecutor : ISyncSingleThreadExecutor
{
    private readonly EventLoopScheduler _scheduler;

    public RxSyncSingleThreadExecutor()
    {
        _scheduler = new EventLoopScheduler();
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        _scheduler.Schedule(() =>
        {
            try
            {
                if (!cts.IsCancellationRequested)
                {
                    task(cts.Token);
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
                cancellableTask.MarkDisposed();
                //cts.Dispose();
            }
        });

        return cancellableTask;
    }

    public void Shutdown()
    {
        _scheduler.Dispose();
    }

    public void Dispose()
    {
        _scheduler.Dispose();
    }
}

public class DataflowSyncSingleThreadExecutor : ISyncSingleThreadExecutor
{
    private readonly ActionBlock<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _actionBlock;
    private volatile bool _isShutdown;

    public DataflowSyncSingleThreadExecutor()
    {
        // Create a dedicated TaskScheduler with a single thread
        var dedicatedScheduler = new DedicatedThreadTaskScheduler();

        _actionBlock = new ActionBlock<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)>(
            item => ExecuteTask(item),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                TaskScheduler = dedicatedScheduler,  // Use dedicated thread
                EnsureOrdered = true,
                SingleProducerConstrained = false,
                BoundedCapacity = DataflowBlockOptions.Unbounded
            });
    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        _actionBlock.Post((task, cts, cancellableTask));

        return cancellableTask;
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
        finally
        {
            cancellableTask.MarkDisposed();
            cts.Dispose();
        }
    }

    public void Shutdown()
    {
        if (_isShutdown)
            return;

        _isShutdown = true;
        _actionBlock.Complete();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();

        try
        {
            _actionBlock.Completion.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // Timeout or other exception during shutdown
        }
    }
}

internal class DedicatedThreadTaskScheduler : TaskScheduler
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly Thread _thread;

    public DedicatedThreadTaskScheduler()
    {
        _thread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "DataflowExecutor"
        };
        _thread.Start();
    }

    protected override void QueueTask(Task task)
    {
        _tasks.Add(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Only execute inline if we're already on our dedicated thread
        return Thread.CurrentThread == _thread && TryExecuteTask(task);
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return _tasks.ToArray();
    }

    private void ProcessTasks()
    {
        foreach (var task in _tasks.GetConsumingEnumerable())
        {
            TryExecuteTask(task);
        }
    }
}