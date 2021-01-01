![.NET](https://github.com/serge-sedelnikov/xstate.net/workflows/.NET/badge.svg?branch=main)

# Important

Work in progress! I will notify here once the version of the state machine engine can be used in production.

# xstate.net

.NET implementation of the finite state machine framework.

# Main classes

- StateMachine
    - ~~has list of states~~;
    - ~~has initial state~~;
    - Export state machine to JSON for visualization;
- State
    - ~~Can Invoke Service(s) or another StateMachine~~;
    - ~~Has OnEntry actions~~;
    - ~~Has OnExit actions~~;
    - ~~Can be a final state;~~
    - ~~Can be a transient state~~;
    - ~~Can have a delayed (timeout) transition~~;
- Service
    - ~~Executes while machine in the state, can change state via callback("name");~~
    - ~~has OnDone transition in case of service is async;~~
    - ~~has OnError transition in case of service is async~~
- Action
    - ~~Fire and forget Action~~
- Activity
- StateMachineInterpreter
    - ~~Can run state machine;~~
    - ~~Has event on state changed;~~
    - Throws error in any case;