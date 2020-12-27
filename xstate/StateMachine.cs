using System;
using System.Collections.Generic;

namespace XStateNet
{
    /// <summary>
    /// The state machine class, used to create a new state machine, then run it with the interpreter.
    /// </summary>
    public class StateMachine
    {
        private readonly string _initialStateId;
        private readonly string _id;
        private string _name;

        /// <summary>
        /// The list of the states for the state machine.
        /// </summary>
        /// <value></value>
        public IEnumerable<State> States { get; set; }

        /// <summary>
        /// The ID of the initial state. If not set, with throw an exception.
        /// </summary>
        /// <value></value>
        public string InitialStateId { get => _initialStateId; }

        /// <summary>
        /// Executes when state machien is exiting with success. To do so the state machien must have at least one final state.
        /// </summary>
        /// <value></value>
        public Action DoneHandler { get; set; }

        /// <summary>
        /// Creates the state machine.
        /// </summary>
        /// <param name="id">ID of the state machine.</param>
        /// <param name="name">Name of the state machine.</param>
        /// <param name="initialStateId">Initial state ID that the machine will start from.</param>
        /// <param name="states">Array of states for this state machine.</param>
        public StateMachine(string id, string name, string initialStateId, params State[] states)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException($"'{nameof(id)}' cannot be null or empty", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            if (string.IsNullOrEmpty(initialStateId))
            {
                throw new ArgumentException($"'{nameof(initialStateId)}' cannot be null or empty", nameof(initialStateId));
            }
            _initialStateId = initialStateId;
            _id = id;
            _name = name;
            States = states;
        }
    }
}
