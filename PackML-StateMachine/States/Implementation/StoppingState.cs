using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;

/**
 * The {@link StoppingState} denotes a transitive state that should bring a machine to a sudden halt. Contrary to actions in {@link AbortingState},
 * actions in {@link StoppingState} should not lead to product damages. After having executed the action in stopping, the state machine will
 * transition to the {@link StoppedState}.
 */
public class StoppingState : AbortableState
{
    public override void start(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void hold(Isa88StateMachine stateMachine)
    {
        // Hold cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void unhold(Isa88StateMachine stateMachine)
    {
        // Unhold cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void suspend(Isa88StateMachine stateMachine)
    {
        // Suspend cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void unsuspend(Isa88StateMachine stateMachine)
    {
        // Unsuspend cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void reset(Isa88StateMachine stateMachine)
    {
        // Reset cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }
    public override void clear(Isa88StateMachine stateMachine)
    {
        // Clear cannot be fired from Stopping -> Do nothing except maybe giving a warning
    }

    public override void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Stopping);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still Stopping before going to Stopped (could have been changed in the mean time).
        if (stateMachine.getState() is StoppingState) {
            stateMachine.setStateAndRunAction(new StoppedState());          
        }
    }
}
