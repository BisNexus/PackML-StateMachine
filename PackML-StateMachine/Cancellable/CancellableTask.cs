namespace PackML_StateMachine.Threading;

public class CancellableTask<T>
{
    public string Id { get; }
    public Task<T> Task { get; }
    private readonly CancellableTaskItem<T> _taskItem;

    internal CancellableTask(string id, Task<T> task, CancellableTaskItem<T> taskItem)
    {
        Id = id;
        Task = task;
        _taskItem = taskItem;
    }

    public void Cancel()
    {
        _taskItem.Cancel();
    }

    public bool IsCancelled => _taskItem.IsCancelled;
}

public class CancellableTask1
{
    public string Id { get; }
    public Task Task { get; }
    private readonly CancellableTaskItem _taskItem;

    internal CancellableTask1(string id, Task task, CancellableTaskItem taskItem)
    {
        Id = id;
        Task = task;
        _taskItem = taskItem;
    }

    public void Cancel()
    {
        _taskItem.Cancel();
    }

    public bool IsCancelled => _taskItem.IsCancelled;
}