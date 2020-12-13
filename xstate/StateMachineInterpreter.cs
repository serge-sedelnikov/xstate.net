using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XStateNet
{
    /// <summary>
    /// Interprets the state machine.
    /// </summary>
    public class Interpreter
    {
        /// <summary>
        /// State machine.
        /// </summary>
        private StateMachine _stateMachine;

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

            Task.Run(() => {
                Invoke(initialState);
            });
        }

        /// <summary>
        /// Invokes one state.
        /// </summary>
        /// <param name="state"></param>
        private void Invoke(State state)
        {
            var cleanUpActions = new List<Action>();
            
            // callback that affects the state change.
            var callback = new Action<string>((string eventId) => {
                // check next state
                var nextStateId = state.Transitions[eventId];
                if(string.IsNullOrEmpty(nextStateId)){
                    return;
                }

                // try to find next state to invoke
                var nextState = _stateMachine.States.FirstOrDefault(s => s.Id == nextStateId);
                if(nextState == null){
                    return;
                }

                // execute on exit actions before moving to the next state
                cleanUpActions.ForEach(cleanUpAction => {
                    cleanUpAction();
                });

                // invoke on exit actions
                state.InvokeExitActions();

                // invoke next state
                Invoke(nextState);
            });

            // execute all on entry actions one by one
            state.InvokeEnterActions();

            // execute all services in parallel
            state.ServiceDelegates.ForEach(d => {
                // each service returns the destructor action, store them to execute on state exit.
                cleanUpActions.Add(d(state, callback));
            });

            
        }
    }
}