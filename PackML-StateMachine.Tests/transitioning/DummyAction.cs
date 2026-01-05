using Newtonsoft.Json.Linq;
using PackML_StateMachine.States;
using System;
using System.Security.Cryptography.X509Certificates;

namespace PackML_StateMachine.Tests.transitioning;
public class DummyAction : IStateAction
{
    readonly int dummyActionTime;

    public DummyAction(int dummyActionTime)
    {
        this.dummyActionTime = dummyActionTime;
    }

    public void execute(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested(); // <--- Check for cancellation
            Task.Delay(dummyActionTime, cancellationToken).Wait(cancellationToken); // Simulate work with periodic checks for cancellation
        }
        catch (OperationCanceledException)
        {
             
        }
    }
}
 