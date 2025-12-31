using PackML_StateMachine.StateMachine;
using System.Collections.Concurrent;

namespace PackML_StateMachine.States;
public abstract class State : IState
{
    public abstract void abort(Isa88StateMachine stateMachine);
    public abstract void clear(Isa88StateMachine stateMachine);
    public abstract void hold(Isa88StateMachine stateMachine);
    public abstract void reset(Isa88StateMachine stateMachine);
    public abstract void start(Isa88StateMachine stateMachine);
    public abstract void stop(Isa88StateMachine stateMachine);
    public abstract void suspend(Isa88StateMachine stateMachine);
    public abstract void unhold(Isa88StateMachine stateMachine);
    public abstract void unsuspend(Isa88StateMachine stateMachine);

    /**
	 * Execute an action, complete this state and transition to the next state 
	 * @param stateMachine The current state machine instance
	 */
    public virtual void executeActionAndComplete(Isa88StateMachine stateMachine, CancellationToken cancellationToken)
    {
        // Default implementation: Do nothing
        // Acting states have to override this method in order to automatically complete
    }

    /**
     * Default of a simple runAction implementation. Could be overriden if e.g. an action has to run in a separate thread
     * @param action {@link IStateAction} that is going to be executed
     */
    protected void executeAction(IStateAction action, CancellationToken cancellationToken)
    {
        action.execute(cancellationToken);
    }

    private static readonly ConcurrentDictionary<Type, ILogger> _loggers = new();

    protected ILogger Logger => _loggers.GetOrAdd(GetType(), type => StateMachineLogger.For(type));
}

