![.NET](https://github.com/serge-sedelnikov/xstate.net/workflows/.NET/badge.svg?branch=main)

# xstate.net
.NET implementation of xstate framework for NodeJS (https://github.com/davidkpiano/xstate)

# Main classes

- StateMachine
    - ~~has list of states~~;
    - ~~has initial state~~;
    - Has OnDone event handler;
    - Has OnError event handler;
    - Export state machine to JSON for visualization;
- State
    - ~~Can Invoke Service(s) or another StateMachine~~;
    - Can have own states;
    - ~~Has OnEntry actions~~;
    - ~~Has OnExit actions~~;
    - Can be a final state;
    - Can be a transient state;
- Service
    - ~~Executes while machine in the state, can change state via callback("name");~~
    - has OnDone transition in case of service is async;
    - has OnError transition in case of service is async
- Action
    - ~~Fire and forget Action~~
- Activity
- StateMachineInterpreter
    - Can run state machine;
    - Has event on state changed;
    - Has on error event;