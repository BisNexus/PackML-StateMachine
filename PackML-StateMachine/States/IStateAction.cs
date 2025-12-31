namespace PackML_StateMachine.States;

public interface IStateAction
{
    public void execute(CancellationToken cancellationToken);
}
