using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;
using PackML_StateMachine.States.Implementation;
using PackML_StateMachine.Tests.transitioning;

namespace PackML_StateMachine.Tests.transitioning;
public class TestTransitioningWithDummyActions
{
    private const int DummyActionTime = 300;
    private readonly IStateAction dummyAction = new DummyAction(DummyActionTime);

    [Fact]
    public void TestSimpleSetup()
    {
        var stateMachine = new StateMachineBuilder().build();
        Assert.True(stateMachine.getState() is IdleState, "After setup, state machine should be in IdleState");
    }

    [Fact]
    public void TestOtherInitialState()
    {
        // Setup machine in stopped state
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new StoppedState())
                                .build();
        Assert.True(stateMachine.getState() is StoppedState, "Machine should be in StoppedState");
    }

    [Fact]
    public void TestAbortFromIdle()
    {
        // Setup in any state, we just take suspended
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new SuspendedState())
                                .withActionInAborting(dummyAction)
                                .build();
        stateMachine.abort();
        var currentState = stateMachine.getState();
        Assert.True(currentState is AbortingState, "Dummy action should lead to a delay, state machine should therefore be in Aborting");
    }

    [Fact]
    public void WaitForAbortedToBeReached()
    {
        // Setup in any state, we just take complete
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new CompleteState())
                                .withActionInAborting(dummyAction)
                                .build();
        stateMachine.abort();
        WaitForDummyActionToBeCompleted(1); // Wait for aborting
        Assert.True(stateMachine.getState() is AbortedState, "After the aborting action has been executed, Aborted should have been reached");
    }

    [Fact]
    public void AbortAgain()
    {
        // Setup in Aborted State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new AbortedState())
                                .withActionInAborting(dummyAction)
                                .build();
        stateMachine.abort();
        Assert.True(stateMachine.getState() is AbortedState, "State machine should stay in Aborted when abort is fired again");
    }

    [Fact]
    public void TestClearingWhenAborted()
    {
        // Setup in Aborted State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new AbortedState())
                                .withActionInClearing(dummyAction)
                                .build();
        stateMachine.clear();
        Assert.True(stateMachine.getState() is ClearingState, "Machine should switch to ClearingState when clearing is issued from aborted");
    }

    [Fact]
    public void TestAbortFromClearing()
    {
        // Setup in Aborted State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new AbortedState())
                                .withActionInAborting(dummyAction)
                                .withActionInClearing(dummyAction)
                                .build();
        stateMachine.clear();
        stateMachine.abort();
        Assert.True(stateMachine.getState() is AbortingState, "Machine should switch to AbortingState when abort is fired while clearing ");
    }

    [Fact]
    public void TestClearingAndResettingFromAborting()
    {
        // Setup in Aborted State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new AbortedState())
                                .withActionInClearing(dummyAction)
                                .withActionInResetting(dummyAction)
                                .build();
        stateMachine.clear();
        WaitForDummyActionToBeCompleted(1); // Wait for clearing
        stateMachine.reset();
        WaitForDummyActionToBeCompleted(1); // Wait for resetting
        Assert.True(stateMachine.getState() is IdleState, "Machine should switch to IdleState when clearing and resetting from Aborted");
    }

    [Fact]
    public void TestStartingFromIdle()
    {
        // Setup in Idle State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new IdleState())
                                .withActionInStarting(dummyAction)
                                .build();
        stateMachine.start();
        Assert.True(stateMachine.getState() is StartingState, "Machine should switch to StartingState when starting from Idle");
    }

    [Fact]
    public void TestStartingAndCompletingFromIdle()
    {
        // Setup in Idle State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new IdleState())
                                .withActionInStarting(dummyAction)
                                .withActionInExecute(dummyAction)
                                .withActionInCompleting(dummyAction)
                                .build();
        stateMachine.start();
        WaitForDummyActionToBeCompleted(3); // Wait for starting, executing and completing
        Assert.True(stateMachine.getState() is CompleteState,
                "Machine should switch to CompleteState after transitioning through Starting, Execute and Completing. State is: " + stateMachine.getState());
    }

    [Fact]
    public void TestResettingToIdleFromComplete()
    {
        // Setup in Complete State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new CompleteState())
                                .withActionInResetting(dummyAction)
                                .build();
        stateMachine.reset();
        WaitForDummyActionToBeCompleted(1); // Wait for resetting
        Assert.True(stateMachine.getState() is IdleState, "Machine should switch to IdleState after transitioning through Resetting");
    }

    [Fact]
    public void TestSuspendFromComplete()
    {
        // Setup in Idle State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new IdleState())
                                .withActionInStarting(dummyAction)
                                .withActionInSuspending(dummyAction)
                                .withActionInExecute(dummyAction)
                                .build();
        stateMachine.start();
        WaitForDummyActionToBeCompleted(1); // Wait for starting to be complete
        stateMachine.suspend();
        Assert.True(stateMachine.getState() is SuspendingState,
                "Machine should switch to SuspendingState when suspend is fired in ExecuteState. State is: " + stateMachine.getState());
    }

    [Fact]
    public void TestUnsuspendFromSuspend()
    {
        // Setup in Execute State
        var stateMachine = new StateMachineBuilder()
                                .withInitialState(new ExecuteState())
                                .withActionInSuspending(dummyAction)                                                  
                                .withActionInUnsuspending(dummyAction)
                                .build();
        WaitForDummyActionToBeCompleted(1); // Wait for suspending
        stateMachine.unsuspend();
        WaitForDummyActionToBeCompleted(1); // Wait for unsuspending

        Assert.True(stateMachine.getState() is ExecuteState, "Machine should go back to Execute after unsuspend is fired in SuspendedState");
    }

    private void WaitForDummyActionToBeCompleted(int numberOfActionsToAwait)
    {
        Thread.Sleep(numberOfActionsToAwait * DummyActionTime + 200);
    }
}
