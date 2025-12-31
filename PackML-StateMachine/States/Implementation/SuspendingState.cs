using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;
/**
 * The {@link SuspendingState} denotes a transitive state that is entered on a suspend command. This command is typically issued when external
 * conditions prevent a machine from continuing execution (e.g. waiting for downstream machines). After executing the action the state machine will
 * transition to the {@link SuspendedState}
 */
public class SuspendingState : StoppableState
{
    public override void start(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void hold(Isa88StateMachine stateMachine)
    {
        // Hold cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void unhold(Isa88StateMachine stateMachine)
    {
        // Unhold cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void suspend(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void unsuspend(Isa88StateMachine stateMachine)
    {
        // Unsuspend cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void reset(Isa88StateMachine stateMachine)
    {
        // Reset cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void clear(Isa88StateMachine stateMachine)
    {
        // Clear cannot be fired from Suspending -> Do nothing except maybe giving a warning
    }


    public override void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Suspending);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still execute before going to Completing (could have been changed in the mean time).
        if (stateMachine.getState() is SuspendingState) {
            stateMachine.setStateAndRunAction(new SuspendedState());
        }
    }

}
