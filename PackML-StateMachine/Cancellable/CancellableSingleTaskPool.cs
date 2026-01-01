using System.Collections.Concurrent;

namespace PackML_StateMachine.Threading;

public interface ICancellableSingleTaskPool : IDisposable
{
    CancellableTask<T> Submit<T>(Func<CancellationToken, T> task);
    CancellableTask1 Submit(Action<CancellationToken> task);
    void Execute(Action<CancellationToken> task);
    bool CancelTask(string taskId);
    void CancelCurrentTask();
    void CancelAllTasks();
    bool IsRunning { get; }
    string CurrentTaskId { get; }
    void Shutdown();
}

public class CancellableSingleTaskPool : ICancellableSingleTaskPool
{
    private readonly ConcurrentQueue<CancellableTaskItem> _taskQueue = new();
    private readonly ConcurrentDictionary<string, CancellableTaskItem> _taskRegistry = new();
    private readonly ManualResetEventSlim _taskAvailable = new(false);
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private readonly Thread _workerThread;
    private volatile bool _isShutdown = false;
    private volatile bool _isRunning = false;
    private volatile string _currentTaskId = null;

    public bool IsRunning => _isRunning;
    public string CurrentTaskId => _currentTaskId;

    public CancellableSingleTaskPool()
    {
        _workerThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Name = "CancellableSingleTaskPool-Worker"
        };
        _workerThread.Start();
    }

    public CancellableTask<T> Submit<T>(Func<CancellationToken, T> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("TaskPool is shutdown");

        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
        CancellableTaskItem<T> taskItem = new CancellableTaskItem<T>(task, tcs);

        _taskRegistry[taskItem.Id] = taskItem;
        _taskQueue.Enqueue(taskItem);
        _taskAvailable.Set();

        return new CancellableTask<T>(taskItem.Id, tcs.Task, taskItem);
    }

    public CancellableTask1 Submit(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("TaskPool is shutdown");

        TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
        CancellableActionTaskItem taskItem = new CancellableActionTaskItem(task, tcs);

        _taskRegistry[taskItem.Id] = taskItem;
        _taskQueue.Enqueue(taskItem);
        _taskAvailable.Set();

        return new CancellableTask1(taskItem.Id, tcs.Task, taskItem);
    }

    public void Execute(Action<CancellationToken> task)
    {
        if (_isShutdown)
            throw new InvalidOperationException("TaskPool is shutdown");

        CancellableActionTaskItem taskItem = new CancellableActionTaskItem(task, null);

        _taskRegistry[taskItem.Id] = taskItem;
        _taskQueue.Enqueue(taskItem);
        _taskAvailable.Set();
    }

    public bool CancelTask(string taskId)
    {
        if (_taskRegistry.TryGetValue(taskId, out CancellableTaskItem? taskItem))
        {
            taskItem.Cancel();
            return true;
        }
        return false;
    }

    public void CancelCurrentTask()
    {
        if (_currentTaskId != null)
        {
            CancelTask(_currentTaskId);
        }
    }

    public void CancelAllTasks()
    {
        foreach (CancellableTaskItem taskItem in _taskRegistry.Values)
        {
            taskItem.Cancel();
        }
    }

    private void ProcessTasks()
    {
        while (!_shutdownTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                _taskAvailable.Wait(_shutdownTokenSource.Token);

                while (_taskQueue.TryDequeue(out CancellableTaskItem? taskItem) && !_shutdownTokenSource.Token.IsCancellationRequested)
                {
                    _isRunning = true;
                    _currentTaskId = taskItem.Id;

                    try
                    {
                        taskItem.Execute();
                    }
                    finally
                    {
                        _taskRegistry.TryRemove(taskItem.Id, out _);
                        _currentTaskId = null;
                        _isRunning = false;
                    }
                }

                if (_taskQueue.IsEmpty)
                {
                    _taskAvailable.Reset();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in task processing: {ex.Message}");
                _currentTaskId = null;
                _isRunning = false;
            }
        }
    }

    public void Shutdown()
    {
        _isShutdown = true;
        CancelAllTasks();
        _shutdownTokenSource.Cancel();
    }

    public void Dispose()
    {
        if (!_isShutdown)
            Shutdown();

        _workerThread?.Join(TimeSpan.FromSeconds(5));
        _taskAvailable?.Dispose();
        _shutdownTokenSource?.Dispose();

        foreach (CancellableTaskItem taskItem in _taskRegistry.Values)
        {
            taskItem.CancellationTokenSource?.Dispose();
        }
    }
}