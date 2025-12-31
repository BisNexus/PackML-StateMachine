namespace PackML_StateMachine.States;
public class NullStateAction : IStateAction
{
    /**
	 * Execute of a {@link NullStateAction} simply does nothing
	 */
    public void execute(CancellationToken cancellationToken)
    {
    }
}
