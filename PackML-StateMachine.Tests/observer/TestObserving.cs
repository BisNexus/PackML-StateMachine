using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;
using Xunit.Sdk;
using Xunit.v3;


namespace PackML_StateMachine.Tests.observer;

[Collection("TestObservingIsolation")]
[TestCaseOrderer(typeof(PriorityOrderer))]
public class TestObserving
{
    class ExampleObserver : IStateChangeObserver
    {
        public string observedStateName;

        public void onStateChanged(IState newState)
        {
            observedStateName = newState.GetType().Name;
        }
    }

    private const int dummyActionTime = 300;
    private static IStateAction dummyAction;
    private static Isa88StateMachine stateMachine;
    private static ExampleObserver firstObserver;
    private static ExampleObserver secondObserver;

    // Static constructor replaces @BeforeAll...setUp()
    static TestObserving()
    {
        firstObserver = new ExampleObserver();
        secondObserver = new ExampleObserver();

        // Create a dummy action that just pauses the thread
        dummyAction = new DummyAction();

        StateMachineBuilder builder = new StateMachineBuilder();
        stateMachine = builder
            .withActionInAborting(dummyAction)
            .withActionInClearing(dummyAction)
            .withActionInCompleting(dummyAction)
            .withActionInExecute(dummyAction)
            .withActionInHolding(dummyAction)
            .withActionInResetting(dummyAction)
            .withActionInStarting(dummyAction)
            .withActionInStopping(dummyAction)
            .withActionInSuspending(dummyAction)
            .withActionInUnholding(dummyAction)
            .withActionInUnsuspending(dummyAction)
            .build();
    }

    private class DummyAction : IStateAction
    {
        public void execute(CancellationToken cancellationToken)
        {
            try
            {
                Task.Delay(dummyActionTime, cancellationToken).GetAwaiter().GetResult(); // Simulate work with periodic checks for cancellation
            }
            catch (OperationCanceledException)
            {
                // Handle thread interruption
            }
        }
    }

    [Fact]
    [TestPriority(1)]
    public void AddFirstObserverAndTestStart()
    {
        stateMachine.addStateChangeObserver(firstObserver);
        stateMachine.start();
        Assert.True("StartingState" == firstObserver.observedStateName, "Observer should be notified that the state machine is now in Starting");
    }

    [Fact]
    [TestPriority(2)]
    public void TestResetWithFirstObserver()
    {
        Thread.Sleep(dummyActionTime * 4); // Wait for execution of starting, execute, completing + safetyTime
        stateMachine.reset();
        Thread.Sleep(dummyActionTime * 2); // Wait for execution of resetting + safetyTime
        Assert.True("IdleState" == firstObserver.observedStateName, "Observer should be notified that the state machine is now in Idle");
    }

    [Fact]
    [TestPriority(3)]
    public void AddSecondObserverAndTestStart()
    {
        stateMachine.addStateChangeObserver(secondObserver);
        stateMachine.start();
        Assert.True(secondObserver.observedStateName == "StartingState", "Second observer should be notified that the state machine is now in Starting");
    }

    [Fact]
    [TestPriority(4)]
    public void MakeSureFirstObserverStillWorking()
    {
        Thread.Sleep(dummyActionTime * 4); // Wait for execution of starting, execute, completing + safetyTime
        Assert.True(firstObserver.observedStateName == "CompleteState", "First observer should have tracked changes and should now be in CompleteState");
    }

    [Fact]
    [TestPriority(5)]
    public void RemoveSecondObserverAndMakeSureFirstObserverStillWorking()
    {
        stateMachine.removeStateChangeObserver(secondObserver);
        stateMachine.reset();
        Thread.Sleep(dummyActionTime * 2); // Wait for execution of resetting + safetyTime
        Assert.True(firstObserver.observedStateName == "IdleState", "First observer should now be in IdleState");
    }

    [Fact]
    [TestPriority(6)]
    public void TestSecondObserverNoLongerNotified()
    {
        Assert.True(secondObserver.observedStateName == "CompleteState", "Second observer should not have been notified after removal and should still be in CompleteState");
    }
}

[CollectionDefinition("TestObservingIsolation", DisableParallelization = true)]
public class TestObservingIsolationCollection { }

// Custom test priority attribute
public class TestPriorityAttribute : System.Attribute
{
    public int Priority { get; }

    public TestPriorityAttribute(int priority)
    {
        Priority = priority;
    }
}

// Custom Xunit v3 test orderer
public class PriorityOrderer : ITestCaseOrderer
{
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
    {
        var sortedMethods = new SortedDictionary<int, List<TTestCase>>();

        foreach (var testCase in testCases)
        {
            var priority = 0;

            // In xUnit v3, use Traits to get the priority
            if (testCase.Traits.TryGetValue("Priority", out var priorityValues) && priorityValues.Count > 0)
            {
                int.TryParse(priorityValues.First(), out priority);
            }

            GetOrCreate(sortedMethods, priority).Add(testCase);
        }

        var result = new List<TTestCase>();
        foreach (var priority in sortedMethods.Keys)
        {
            result.AddRange(sortedMethods[priority]);
        }
        return result;
    }

    private static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        if (dictionary.TryGetValue(key, out var result)) return result;

        result = new TValue();
        dictionary[key] = result;
        return result;
    }
}