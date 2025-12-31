using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;
/**
 * The {@link HoldingState} denotes a transitive state that brings a machine to a stop when internal conditions prevent further production. After
 * having completed holding procedure, the state machine will change to the {@link HeldState}.
 */
public class HoldingState : StoppableState
{



    public override void start(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void hold(Isa88StateMachine stateMachine)
    {
        // Hold cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void unhold(Isa88StateMachine stateMachine)
    {
        // Unhold cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void suspend(Isa88StateMachine stateMachine)
    {
        // Start cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void unsuspend(Isa88StateMachine stateMachine)
    {
        // Unsuspend cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void reset(Isa88StateMachine stateMachine)
    {
        // Reset cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void clear(Isa88StateMachine stateMachine)
    {
        // Clear cannot be fired from Holding -> Do nothing except maybe giving a warning
    }


    public override void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Holding);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still Holding before going to Held (could have been changed in the mean time).
        if (stateMachine.getState() is HoldingState) {
            stateMachine.setStateAndRunAction(new HeldState());
        }
    }

}

