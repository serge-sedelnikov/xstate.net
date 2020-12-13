using System;

namespace XStateNet
{
    /// <summary>
    /// The one state of the state machine.
    /// </summary>
    public class State
    {
        /// <summary>
        /// ID of the state
        /// </summary>
        private readonly string _id;

        /// <summary>
        /// Gets the state ID value.
        /// </summary>
        /// <value></value>
        public string Id { get { return _id; }}

        /// <summary>
        /// Creates an instance of the state.
        /// </summary>
        /// <param name="id">State ID value. All state IDs must be unique.</param>
        public State(string id)
        {
            this._id = id;
        }
    }
}