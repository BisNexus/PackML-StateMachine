using PackML_StateMachine.States;

namespace PackML_StateMachine.Tests.transitioning;

public class PrintingAction : IStateAction
{
    private readonly int dummyActionTime;
    private readonly string stateName;

    public PrintingAction(string stateName, int dummyActionTime)
    {
        this.stateName = stateName;
        this.dummyActionTime = dummyActionTime;
    }

    public void execute(CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        try
        {
            while ((DateTime.UtcNow - start).TotalMilliseconds < dummyActionTime)
            {
                cancellationToken.ThrowIfCancellationRequested(); // <--- Check for cancellation
                Console.WriteLine($"doing something in state: {stateName}. -- Thread {Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId.ToString()}");
                Task.Delay(500, cancellationToken).GetAwaiter().GetResult(); // Simulate work with periodic checks for cancellation
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Printing Action interrupted in State {stateName}");
        }
    }
}