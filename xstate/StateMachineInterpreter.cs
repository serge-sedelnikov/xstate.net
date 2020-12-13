using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XStateNet
{
    /// <summary>
    /// The arguments for the state changed event callback.
    /// </summary>
    public class StateChangeEventArgs : EventArgs
    {
        /// <summary>
        /// State that the machine switched to.
        /// </summary>
        /// <value></value>
        public State State { get; private set; }

        /// <summary>
        /// Previous machine state.
        /// </summary>
        /// <value></value>
        public State PreviousState { get; private set; }

        /// <summary>
        /// Creates an instance of the arguments.
        /// </summary>
        /// <param name="state">State that the machine switched to</param>
        /// <param name="previousState">Previous machine state</param>
        public StateChangeEventArgs(State state, State previousState)
        {
            State = state;
            PreviousState = previousState;
        }
    }

    /// <summary>
    /// Handler for the state changed event.
    /// </summary>
    /// <param name="sender">Interpreter who sent the event.</param>
    /// <param name="args">Arguments containing the current and previous stated of the machine.</param>
    public delegate void StateChangedEventHandler(object sender, StateChangeEventArgs args);

    /// <summary>
    /// Interprets the state machine.
    /// </summary>
    public class Interpreter
    {
        /// <summary>
        /// Event fires every time state machine chnages the state.
        /// </summary>
        public event StateChangedEventHandler OnStateChanged;

        /// <summary>
        /// State machine.
        /// </summary>
        private StateMachine _stateMachine;

        private void RaiseOnStateChangedEvent(State newState, State previousState)
        {
            StateChangedEventHandler handler = OnStateChanged;
            handler?.Invoke(this, new StateChangeEventArgs(newState, previousState));
        }

        /// <summary>
        /// Starts the state machine.
        /// </summary>
        /// <param name="machine">State machine to start.</param>
        public void StartStateMachine(StateMachine machine)
        {
            this._stateMachine = machine ?? throw new ArgumentNullException(nameof(machine));

            var initialState = _stateMachine.States.FirstOrDefault(s => s.Id == _stateMachine.InitialStateId);
            if (initialState == null)
            {
                throw new InvalidOperationException("Initial state is not defined for the state machine or not found. Define the correct initial state.");
            }

            Task.Run(() =>
            {
                
                Invoke(initialState);
            });
        }

        /// <summary>
        /// Invokes one state.
        /// </summary>
        /// <param name="state"></param>
        private void Invoke(State state, State previousState = null)
        {
            // raise state changed event
            RaiseOnStateChangedEvent(state, previousState);

            // store cleanup actions
            var cleanUpActions = new List<Action>();

            // callback that affects the state change.
            var callback = new Action<string>((string eventId) =>
            {
                // check next state
                var nextStateId = state.Transitions[eventId];
                if (string.IsNullOrEmpty(nextStateId))
                {
                    return;
                }

                // try to find next state to invoke
                var nextState = _stateMachine.States.FirstOrDefault(s => s.Id == nextStateId);
                if (nextState == null)
                {
                    return;
                }

                // execute on exit actions before moving to the next state
                cleanUpActions.ForEach(cleanUpAction =>
                {
                    cleanUpAction();
                });

                // invoke on exit actions
                state.InvokeExitActions();

                // invoke next state, provising previous state for event raising
                Invoke(nextState, state);
            });

            // execute all on entry actions one by one
            state.InvokeEnterActions();

            // execute all services in parallel
            state.ServiceDelegates.ForEach(d =>
            {
                // each service returns the destructor action, store them to execute on state exit.
                cleanUpActions.Add(d(state, callback));
            });


        }
    }
}