using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;

namespace PackML_StateMachine.Tests.transitioning;

public class TestStateMachineSetup
{
    const int dummyActionTime = 500;
    IStateAction dummyAction = new DummyAction(dummyActionTime);

    [Theory]
    [InlineData(ActiveStateName.Starting)]
    [InlineData(ActiveStateName.Execute)]
    [InlineData(ActiveStateName.Completing)]
    [InlineData(ActiveStateName.Holding)]
    [InlineData(ActiveStateName.Unholding)]
    [InlineData(ActiveStateName.Suspending)]
    [InlineData(ActiveStateName.Unsuspending)]
    [InlineData(ActiveStateName.Stopping)]
    [InlineData(ActiveStateName.Clearing)]
    [InlineData(ActiveStateName.Aborting)]
    [InlineData(ActiveStateName.Resetting)]
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