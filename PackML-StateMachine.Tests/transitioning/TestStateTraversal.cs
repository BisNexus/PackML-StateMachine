using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;
using PackML_StateMachine.States.Implementation;
using System;

namespace PackML_StateMachine.Tests.transitioning;
public class TestStateTraversal
{
    private static readonly int dummyActionTime = 3000;
    private IStateAction dummyAction = new DummyAction(dummyActionTime);

    // Set up an observer that collects all states that have been reached
    class ExampleObserver : IStateChangeObserver
    {
        List<String> stateList = [];

        public void onStateChanged(IState newState)
        {
            var observedStateName = newState.GetType().Name;
            stateList.Add(observedStateName + "_" + Environment.CurrentManagedThreadId);
        }

        public List<String> getStateList()
        {
            return this.stateList;
        }
    };

    [Fact]
    public void TestAbortingWhileStarting()
    {
        // Setup in Execute State
        var stateMachine = new StateMachineBuilder()
            .withInitialState(new IdleState())
            .withActionInStarting(dummyAction)
            .withActionInExecute(dummyAction)
            .withActionInCompleting(dummyAction)
            .build();

        var observer = new ExampleObserver();
        stateMachine.addStateChangeObserver(observer);

        // start and wait for execute
        stateMachine.start();
        waitForDummyActionToBeCompleted(1);

        stateMachine.abort();
        waitForDummyActionToBeCompleted(1); // Wait for aborting

        int numberOfStatesTraversed = observer.getStateList().Count;
        Assert.True(4==numberOfStatesTraversed, 
            $"State machine should only traverse through 4 States: Starting, Execute, Aborting, Aborted. States traversed: {string.Join(", ", observer.getStateList())}");
    }

    [Fact]
    public void TestAbortingWhileStopping()
    {
        // Setup in Execute State
        var stateMachine = new StateMachineBuilder()
            .withInitialState(new IdleState())
            .withActionInStarting(new PrintingAction("Starting", dummyActionTime))
            .withActionInExecute(new PrintingAction("Execute", dummyActionTime))
            .withActionInStopping(new PrintingAction("Stopping", dummyActionTime))
            .withActionInAborting(new PrintingAction("Aborting", dummyActionTime))
            .build();

        var observer = new ExampleObserver();
        stateMachine.addStateChangeObserver(observer);

        stateMachine.start(); // start and wait a bit for starting to execute its action
        waitForDummyActionToBeCompleted(0);

        stateMachine.stop(); // stop and wait a bit, then abort
        waitForDummyActionToBeCompleted(0);
        stateMachine.abort();

        waitForDummyActionToBeCompleted(1); // Wait for aborting and then get the number of states that have been traversed
        int numberOfStatesTraversed = observer.getStateList().Count;

        Assert.True(4==numberOfStatesTraversed,
                "State machine should only traverse through the 4 states Starting, Stopping, Aborting, Aborted. States traversed: "
                        + string.Join(", ", observer.getStateList()));
    }

    [Fact]
    public void TestAbortingWhileStopped()
    {
        // Setup in Execute State
        var stateMachine = new StateMachineBuilder()
            .withInitialState(new IdleState())
            .withActionInStarting(new PrintingAction("Starting", dummyActionTime))
            .withActionInStopping(new PrintingAction("Stopping", dummyActionTime))
            .withActionInAborting(new PrintingAction("Aborting", dummyActionTime))
            .build();

        var observer = new ExampleObserver();
        stateMachine.addStateChangeObserver(observer);

        stateMachine.start(); // Start and stop directly
        stateMachine.stop();
        waitForDummyActionToBeCompleted(1); // Wait for stopping
        stateMachine.abort();
        waitForDummyActionToBeCompleted(1); // Wait for aborting

        int numberOfStatesTraversed = observer.getStateList().Count;
        Assert.True(5 == numberOfStatesTraversed,
                "State machine should only traverse through the 5 states Starting, Stopping, Stopped, Aborting, Aborted.  States traversed: "
                        + string.Join(", ", observer.getStateList()));
    }

    [Fact]
    public void TestStoppingWhileStopping()
    {
        // Setup in Execute State
        Isa88StateMachine stateMachine = new StateMachineBuilder().withInitialState(new IdleState()).withActionInStopping(dummyAction).build();
        ExampleObserver observer = new ExampleObserver();
        stateMachine.addStateChangeObserver(observer);
        // stop and stop a couple of times directly again
        stateMachine.stop();
        stateMachine.stop();
        stateMachine.stop();
        stateMachine.stop();
        waitForDummyActionToBeCompleted(1); // Wait for stopping
        int numberOfStatesTraversed = observer.getStateList().Count();
        Assert.True(2 == numberOfStatesTraversed,
                "State machine should only traverse through the 2 states Stoppping and Stopped.  States traversed: " + string.Join(", ", observer.getStateList()));
    }

    [Fact]
    public void TestCompleteTraversalWithActions()
    {
        // Setup in Idle State
        Isa88StateMachine stateMachine = new StateMachineBuilder().withInitialState(new IdleState())
                .withActionInStarting(dummyAction)
                .withActionInExecute(dummyAction)
                .withActionInCompleting(dummyAction)
                .withActionInResetting(dummyAction)
                .build();
        ExampleObserver observer = new ExampleObserver();
        stateMachine.addStateChangeObserver(observer);

        stateMachine.start();                   // Start and wait for Starting, Execute and Completing
        waitForDummyActionToBeCompleted(3);

        stateMachine.reset();                   // Reset and wait for Resetting
        waitForDummyActionToBeCompleted(1);

        int numberOfStatesTraversed = observer.getStateList().Count();
        Assert.True(6 == numberOfStatesTraversed,
                "State machine should do a full traversal through the 6 states Starting, Execute, Completing, Complete, Resetting, Idle.  States traversed: " + string.Join(", ", observer.getStateList()));

    }

    private void waitForDummyActionToBeCompleted(int numberOfActionsToAwait)
    {
        try
        {
            Thread.Sleep((int)(numberOfActionsToAwait * dummyActionTime + 0.2 * dummyActionTime));
        }
        catch (ThreadInterruptedException e)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");
        }
    }
}
