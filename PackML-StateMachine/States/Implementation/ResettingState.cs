using PackML_StateMachine.StateMachine;

namespace PackML_StateMachine.States.Implementation;
public class ResettingState : StoppableState
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
        // Start cannot be fired from Starting -> Do nothing except maybe giving a warning
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
        IStateAction actionToRun = stateMachine.getStateActionManager().getAction(ActiveStateName.Resetting);
        base.executeAction(actionToRun, cancellationToken);

        // Make sure the current state is still Resetting before going to Idle (could have been changed in the mean time).
        if (stateMachine.getState() is ResettingState) {
            stateMachine.setStateAndRunAction(new IdleState());
        }
    }

}

