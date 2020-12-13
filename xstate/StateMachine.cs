using System;
using System.Collections.Generic;

namespace XStateNet
{
    /// <summary>
    /// The state machine class, used to create a new state machine, then run it with the interpreter.
    /// </summary>
    public class StateMachine
    {
        /// <summary>
        /// The list of the states for the state machine.
        /// </summary>
        /// <value></value>
        public IEnumerable<State> States { get; set; }

        /// <summary>
        /// The ID of the initial state. If not set, with throw an exception.
        /// </summary>
        /// <value></value>
        public string InitialStateId {get; set;}

        /// <summary>
        /// Creates the state machine.
        /// </summary>
        public StateMachine() 
        {

        }
    }
}
