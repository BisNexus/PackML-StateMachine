using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;

/**
 * The {@link StartingState} denotes a transitive state that should make a machine ready for producing. After having completed a startup procedure,
 * the state machine will change to the {@link ExecuteState}.
 */
public class StartingState : StoppableState
{
    public override void start(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void hold(Isa88StateMachine stateMachine)
    {
        // Hold cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void unhold(Isa88StateMachine stateMachine)
    {
        // Unhold cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void suspend(Isa88StateMachine stateMachine)
    {
        // Suspend cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void unsuspend(Isa88StateMachine stateMachine)
    {
        // Unsuspend cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void reset(Isa88StateMachine stateMachine)
    {
        // Reset cannot be fired from Starting -> Do nothing except maybe giving a warning
    }
    public override void clear(Isa88StateMachine stateMachine)
    {
        // Clear cannot be fired from Starting -> Do nothing except maybe giving a warning
    }

    public override void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Starting);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still Starting before going to Execute (could have been changed in the mean time).
        if (stateMachine.getState() is StartingState) {
            stateMachine.setStateAndRunAction(new ExecuteState());
        }
    }
}
