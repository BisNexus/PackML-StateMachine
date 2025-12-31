using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;

/**
 * The {@link CompletingState} denotes a transitive state that can be used to bring production to an end (e.g. when the specified number of products
 * have been produced). After the completing action has been executed, the state machine will change to the {@link ExecuteState}.
 */
public class CompletingState : StoppableState
{
    public override void start(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void hold(Isa88StateMachine stateMachine)
    {
        // Hold cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void unhold(Isa88StateMachine stateMachine)
    {
        // Unhold cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void suspend(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void unsuspend(Isa88StateMachine stateMachine)
    {
        // Unsuspend cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void reset(Isa88StateMachine stateMachine)
    {
        // Reset cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void clear(Isa88StateMachine stateMachine)
    {
        // Clear cannot be fired from Completing -> Do nothing except maybe giving a warning
    }


    public override void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Completing);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still Completing before going to Complete (could have been changed in the mean time).
        if (stateMachine.getState() is CompletingState) {
            stateMachine.setStateAndRunAction(new CompleteState());
        }
    }

}
