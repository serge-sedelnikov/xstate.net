using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        /// Executes every time the state machien is exiting the final state with success.
        /// </summary>
        public event EventHandler OnStateMachineDone;

        /// <summary>
        /// State machine.
        /// </summary>
        private StateMachine _stateMachine;

        private CancellationTokenSource _cancelationTokenSource;

        public Interpreter(StateMachine machine)
        {
            _stateMachine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        /// <summary>
        /// Raises the state change event.
        /// </summary>
        /// <param name="newState">New state the machine has switched to.</param>
        /// <param name="previousState">Previous state the machine has switched from.</param>
        private void RaiseOnStateChangedEvent(State newState, State previousState)
        {
            StateChangedEventHandler handler = OnStateChanged;
            handler?.Invoke(this, new StateChangeEventArgs(newState, previousState));
        }

        /// <summary>
        /// Raises the on done event.
        /// </summary>
        private void RaiseOnStateMachineDone()
        {
            _cancelationTokenSource.Dispose();
            _cancelationTokenSource = null;

            EventHandler handler = OnStateMachineDone;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Starts the state machine.
        /// </summary>
        /// <param name="machine">State machine to start.</param>
        public async void StartStateMachine()
        {
            await StartStateMachineAsync();
        }

        /// <summary>
        /// Starts the state machine.
        /// </summary>
        /// <param name="machine">State machine to start.</param>
        public async Task StartStateMachineAsync()
        {
            if (_cancelationTokenSource != null)
            {
                throw new InvalidOperationException("The state machine is already running. Wait for the state machien to exit or force it to stop.");
            }

            if (_stateMachine.States == null)
            {
                throw new InvalidOperationException("States are not defined for that state machine. Define 'States' property.");
            }

            var initialState = _stateMachine.States.FirstOrDefault(s => s.Id == _stateMachine.InitialStateId);
            if (initialState == null)
            {
                throw new InvalidOperationException("Initial state is not defined for the state machine or not found. Define the correct initial state.");
            }

            // create the cancellation token to track if state machine was forced to close.
            _cancelationTokenSource = new CancellationTokenSource();

            // start invoking the state asyncronously
            await Invoke(initialState);
        }

        /// <summary>
        /// Invokes one state.
        /// </summary>
        /// <param name="state"></param>
        private async Task Invoke(State state, State previousState = null)
        {
            // check if state machine was forced to stop
            if (_cancelationTokenSource.IsCancellationRequested)
            {
                // dispose token
                _cancelationTokenSource.Dispose();
                _cancelationTokenSource = null;
                // force to exit
                return;
            }

            // raise state changed event
            RaiseOnStateChangedEvent(state, previousState);

            // callback that affects the state change.
            State.CallbackAction callback = (eventId, error) =>
            {
                // execute on exit actions before moving to the next state
                state.InvokeCleanupActions();

                // invoke on exit actions
                state.InvokeExitActions();

                // if this was the final state, check it and exit
                if (state.Mode == StateMode.Final)
                {
                    if (error != null)
                    {
                        throw error;
                    }

                    // call state machine on done handler
                    RaiseOnStateMachineDone();

                    // stop the state machine execution
                    return;
                }

                // check next state
                var nextStateId = state.Transitions.GetValueOrDefault(eventId);
                if (string.IsNullOrEmpty(nextStateId))
                {
                    // if there is no next state id, warn developer about it
                    Debug.WriteLine("Transition was called but can't find next state ID for it.", "Warning");
                    if (error != null)
                    {
                        throw error;
                    }
                    return;
                }

                // try to find next state to invoke
                var nextState = _stateMachine.States.FirstOrDefault(s => s.Id == nextStateId);
                if (nextState == null)
                {
                    // if we found next state ID but we could not find the state, thwow exception
                    throw new InvalidOperationException($"Found next state ID to invoke, but state with such ID was not found. Make sure you registered state with the ID '{nextStateId}'");
                }

                // invoke next state, provising previous state for event raising
                Invoke(nextState, state).Wait();
            };


            // execute all on entry actions one by one
            state.InvokeEnterActions();

            // if state is transient, invoke exit actions and return
            if (state.Mode == StateMode.Transient)
            {
                callback("");
                return;
            }

            // execute all services in parallel
            var services = state.ServiceDelegates.Select(d =>
            {
                // run the services on own threads
                return Task.Run(() => d(callback));
            });

            // execute all activities in parallel
            var activities = state.Activities.Select(d =>
            {
                // run the activities in own thread.
                return Task.Run(() => d());
            });

            await Task.WhenAll(services.Union(activities).ToArray());
        }

        /// <summary>
        /// Forces the state machine to be stopped. This method will call an event OnStateMachineError.
        /// </summary>
        public void ForceStopStateMachine()
        {
            if (_cancelationTokenSource != null)
            {
                _cancelationTokenSource.Cancel();
            }
        }
    }
}