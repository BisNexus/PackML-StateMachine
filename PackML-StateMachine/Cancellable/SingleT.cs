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
    private readonly BlockingCollection<(Action<CancellationToken> Action, CancellationTokenSource Cts, CancellableTask Task)> _taskQueue = [];
    //private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _workerTask;
    private volatile bool _isShutdown;

    public SyncSingleThreadExecutor()
    {/*
        _workerTask = Task.Factory.StartNew( () => ProcessTasks(),//_shutdownCts.Token),
            //_shutdownCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        */

        //_workerTask = Task.Factory.StartNew(() => ProcessTasks(),TaskCreationOptions.LongRunning);

        var _workerThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest


        };
         
        _workerThread.Start();

    }

    public CancellableTask Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("Executor is shutdown");

        var cts = new CancellationTokenSource();
        var cancellableTask = new CancellableTask(cts);

        // Add blocks only if bounded; here it's unbounded, so it's effectively enqueue
        _taskQueue.Add((task, cts, cancellableTask));

        return cancellableTask;
    }

    private void ProcessTasks()//CancellationToken shutdownToken)
    {
        //try
        //{
            foreach (var item in _taskQueue.GetConsumingEnumerable())//shutdownToken))
            {
                ExecuteTask(item);
            }
       // }
        //catch (OperationCanceledException)
        //{
            // Shutdown requested, exit loop
       // }
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
        //_shutdownCts.Cancel();

        // Signal no more items will be added; unblocks GetConsumingEnumerable
        _taskQueue.CompleteAdding();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();
        /*
        try
        {
            _workerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
        }
        */

        _taskQueue.Dispose();
        //_shutdownCts.Dispose();
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