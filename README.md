# xstate.net
.NET implementation of xstate framework for NodeJS (https://github.com/davidkpiano/xstate)

# Main classes

- StateMachine
    - has list of states;
    - has initial state;
    - Has OnDone event handler;
    - Has OnError event handler;
- State
    - Can Invoke Service(s) or another StateMachine;
    - Can have own states;
    - Has OnEntry actions;
    - Has OnExit actions;
    - Has Activities executing in parallel to Service;
- Service
    - Executes while machine in the state, can change state via callback("name");
    - has OnDone transition in case of service is async;
    - has OnError transition in case of service is async
- Action
    - Fire and forget Action
- Activity
- StateMachineInterpreter