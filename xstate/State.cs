using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// Service selegates to run async.
        /// </summary>
        private List<InvokeServiceAsyncDelegate> _serviceDelegates;

        private Dictionary<string, string> _transitions;

        public Dictionary<string, string> Transitions { get { return _transitions; } }

        /// <summary>
        /// Gets the state ID value.
        /// </summary>
        /// <value></value>
        public string Id { get { return _id; } }

        /// <summary>
        /// List of services to execute.
        /// </summary>
        /// <value></value>
        public List<InvokeServiceAsyncDelegate> ServiceDelegates { get => _serviceDelegates; }

        /// <summary>
        /// Service invocation delegate.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        public delegate Action InvokeServiceAsyncDelegate(State state, Action<string> callback);

        /// <summary>
        /// Creates an instance of the state.
        /// </summary>
        /// <param name="id">State ID value. All state IDs must be unique.</param>
        public State(string id)
        {
            this._id = id;
            _serviceDelegates = new List<InvokeServiceAsyncDelegate>();
            _transitions = new Dictionary<string, string>();
        }

        /// <summary>
        /// Invokes the service as async method.
        /// </summary>
        /// <param name="invoke">The service to invoke.</param>
        /// <returns></returns>
        public State WithInvoke(InvokeServiceAsyncDelegate invokeAsync)
        {
            _serviceDelegates.Add(invokeAsync);
            // return current state to be able to chain up the services.
            return this;
        }

        /// <summary>
        /// Registers the transition on event to the next state.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="targetStateId"></param>
        /// <returns></returns>
        public State WithTransition(string eventId, string targetStateId)
        {
            _transitions.Add(eventId, targetStateId);
            return this;
        }
    }
}