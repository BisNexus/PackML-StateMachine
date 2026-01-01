[![NuGet](https://img.shields.io/nuget/v/PackML-StateMachine.svg?label=NuGet)](https://www.nuget.org/packages/PackML-StateMachine/)

# .NET PackML-StateMachine
A .NET implementation of the state machine standardized in PackML. The state machine guarantees that only 'valid' transitions can be executed. Have a look at the following figure which depicts the state machine of PackML:

![State machine as defined in PackML (figure taken from http://omac.org/wp-content/uploads/2016/11/PackML_Unit_Machine_Implementation_Guide-V1-00.pdf](https://github.com/aljoshakoecher/ISA88-StateMachine/blob/documentation/images/documentation/isa88-state-machine.png?raw=true)

As you can see in the figure, the state machine defines states and transitions that can be fired on certain states. Here are some examples:
* A 'start'-transition only brings the state machine to the 'Starting' state when it is currently in 'Idle' state
* After production of an order has been completed, the state machine will change its current state to 'Complete'. It can only be reset from this state. 
* When you fire a 'stop'-transition while being in 'Stopped' state, nothing happens
The state machine makes sure that no invalid transitions can be fired.
<br>

## Documentation
### A simple state machine without actions
To use the simplest version of the state machine in your code, you simply obtain an instance from the state machine builder. This state machine will then be in 'Idle' state and you can invoke the transitions shown in the figure above. Note that this state machine cannot execute any actions while being in the active states and that it can just be used to simulate the state machine behavior.

```csharp
// necessary usings
using PackML_StateMachine.StateMachine;
using PackML_StateMachine.States;
using PackML_StateMachine.States.Implementation;

// somewhere in your code, you can setup the most simple state machine (initial state will be 'Idle')
var stateMachine = new StateMachineBuilder().Build();

// you can then invoke the ISA88-transitions on this state machine:
stateMachine.Start();
stateMachine.Suspend();
stateMachine.Stop();
// ...
// see figure for more transitions
```

You can also create a state machine with a different initial state than 'Idle'. This can be done with the `WithInitialState(State s)`-function of the builder. Simply pass in the state you want to have as the initial state to this function. The following example creates a state machine instance that starts in the 'Stopped' state:

```csharp
var stateMachine = new StateMachineBuilder().WithInitialState(new StoppedState()).Build();
```

As shown above, you can invoke transitions by calling the corresponding methods (Start(), Stop(), Hold(), ...) on the state machine. Alternatively, you can also use this more dynamic version:

```csharp
stateMachine.InvokeTransition(TransitionName transitionName);
```
This will invoke a transition with the given TransitionName.
<br>

### A real state machine that executes actions
The state machine of ISA88 allows for executing actions in all active states. These active states are:

* Starting
* Execute
* Holding
* Unholding
* Suspending
* Unsuspending
* Completing
* Resetting
* Stopping
* Aborting
* Clearing

You can create arbitrary actions and pass them to the state machine to let the state machine execute these actions in the correct states. To implement your own actions, simply implement the interface `IStateAction` as shown here:

```csharp
using PackML_StateMachine.States;

public class ExampleActionInStarting : IStateAction
{
    public void Execute()
    {
        Console.WriteLine("Starting up whatever you are working with");
        // You can do what you want in here...
    }
}
```

Your actions can be passed in while creating a state machine instance. For example, you could pass in this `ExampleActionInStarting` as the action that is executed while the state machine is in state 'Starting':

```csharp
var stateMachine = new StateMachineBuilder().WithActionInStarting(new ExampleActionInStarting()).Build();
```

You may use one or more of the following functions of the StateMachineBuilder:

##### WithActionInStarting(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Starting' state.

##### WithActionInExecute(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Execute' state.

##### WithActionInCompleting(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Completing' state.

##### WithActionInSuspending(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Suspending' state.

##### WithActionInUnsuspending(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Unsuspending' state.

##### WithActionInHolding(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Holding' state.

##### WithActionInUnholding(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Unholding' state.

##### WithActionInAborting(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Aborting' state.

##### WithActionInClearing(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Clearing' state.

##### WithActionInStopping(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Stopping' state.

##### WithActionInResetting(IStateAction action)
Sets action to be the action that is going to be executed when the state machine is in 'Resetting' state.

<br><br>
Alternatively, you can also use the more flexible way of adding actions to states:

```csharp
stateMachine.WithAction(IStateAction action, ActiveStateName stateName);
```
You can pass in an action and the name of an active state to add this action to a state.

### Async Actions Support
The state machine also supports asynchronous actions for better performance and non-blocking operations:

```csharp
public class AsyncExampleAction : IStateAction
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await SomeAsyncOperation(cancellationToken);
        Console.WriteLine("Async action completed");
    }
    
    public void Execute()
    {
        // Synchronous fallback if needed
        ExecuteAsync().GetAwaiter().GetResult();
    }
}
```

### Getting notified on state changes
You can create an observer that is notified whenever the state machine changes its state. To do that, you have to create your observer implementing the IStateChangeObserver interface. This interface's method is called on every state change, you can do whatever you like in this function. Here's an example for such a class:

```csharp
public class ExampleObserver : IStateChangeObserver
{
    public void OnStateChanged(IState newState)
    {
        Console.WriteLine($"State has changed, new State is: {newState.GetType().Name}");
    }
}
```

To add a new observer to the state machine simply call `stateMachine.AddStateChangeObserver(IStateChangeObserver observer)` passing in an instance of your observer class. In case an observer should no longer be notified on state changes, simply remove it by calling `stateMachine.RemoveStateChangeObserver(IStateChangeObserver observer)`.

### Dependency Injection Support
The state machine can be easily integrated with .NET's built-in dependency injection container:

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<IPackMLService, PackMLService>();
services.AddSingleton<Isa88StateMachine>(provider => 
{
    return new StateMachineBuilder()
        .WithActionInStarting(new MyStartingAction())
        .WithActionInExecute(new MyExecuteAction())
        .Build();
});
```

## Installation
### NuGet Package
Install the package via NuGet Package Manager:

```bash
dotnet add package PackML-StateMachine
```

Or via Package Manager Console:

```powershell
Install-Package PackML-StateMachine
```

### Building from source
This project is built with .NET 9.0. You can build this library from source if you have the .NET SDK installed. Clone or download this repository and run:

```bash
dotnet build
```

To run tests:

```bash
dotnet test
```

To create a NuGet package:

```bash
dotnet pack
```

## Requirements
- .NET 10.0 or later
- Microsoft.Extensions.Hosting (for background service support)
- Serilog (for logging)

## Disclaimer
Please note that the figure above and all definitions of states and transitions have been taken from the [OMAC PackML Implementation Guide](http://omac.org/wp-content/uploads/2016/11/PackML_Unit_Machine_Implementation_Guide-V1-00.pdf) for PackML.