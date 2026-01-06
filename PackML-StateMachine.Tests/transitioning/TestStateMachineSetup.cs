using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;

namespace PackML_StateMachine.Tests.transitioning;

public class TestStateMachineSetup
{
    private readonly int dummyActionTime = 500;
    private readonly IStateAction dummyAction;

    public TestStateMachineSetup()
    {
        dummyAction = new DummyAction(dummyActionTime);
    }

    public static IEnumerable<object[]> GetActiveStateNames()
    {
        foreach (ActiveStateName stateName in Enum.GetValues<ActiveStateName>())
        {
            yield return new object[] { stateName };
        }
    }

    
    [MemberData(nameof(GetActiveStateNames))]
    public void testActionSetup(ActiveStateName stateName)
    {
        // Arrange
        var stateMachine = new StateMachineBuilder().withAction(dummyAction, stateName).build();

        // Act
        IStateAction action = stateMachine.getStateActionManager().getAction(stateName);

        // Assert
        Assert.True(ReferenceEquals(dummyAction, action), $"dummyAction should be added to state action for {stateName}");
    }
}