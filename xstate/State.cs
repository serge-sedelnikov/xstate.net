using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace XStateNet
{
    /// <summary>
    /// Represents the state type. State has `Normal` type by default.
    /// </summary>
    public enum StateMode
    {
        /// <summary>
        /// Normal state, executes onEnter actions, then runs in parallel all Activities, Services, then on exit, run onExit actions.
        /// </summary>
        Normal,
        /// <summary>
        /// Executes onEnter actions, then immidiately switched the machine to another state, switching, executes onExit actions.
        /// </summary>
        Transient,
        /// <summary>
        /// Executes onEnter actions, then executes in parallel Activities and Services without callback. Once services are done, exits the state machine, state machine is then marked as Done. Before finalizing, calls onExit actions.
        /// </summary>
        Final
    }

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

        /// <summary>
        /// Stores the chain of clean up activities to execute on state exit.
        /// </summary>
        private Action _serviviceCleanupDelegates;

        /// <summary>
        /// Internal list of activities to run in parallel to services.
        /// </summary>
        private List<Action> _activities;

        /// <summary>
        /// Actions invoked on state enter before all services has started. Services are not executed until all enter actions are finalized.
        /// </summary>
        private Action _onEnterActions;

        /// <summary>
        /// Actions invoked on state exit after all services has finished.
        /// </summary>
        private Action _onExitActions;

        /// <summary>
        /// State mode.
        /// </summary>
        private StateMode _mode;

        /// <summary>
        /// Internal list of transitions.
        /// </summary>
        private Dictionary<string, string> _transitions;

        /// <summary>
        /// List of transitions.
        /// </summary>
        /// <value></value>
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
        internal List<InvokeServiceAsyncDelegate> ServiceDelegates { get => _serviceDelegates; }

        /// <summary>
        /// Gets the list of clean up actions to execute on state exit.
        /// The clean up actions are related to services or activities.
        /// </summary>
        /// <value></value>
        internal Action CleanUpActions { get => _serviviceCleanupDelegates; }

        /// <summary>
        /// List of activities to execute.
        /// </summary>
        /// <value></value>
        public List<Action> Activities { get => _activities; }

        /// <summary>
        /// Service invocation delegate.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        public delegate void InvokeServiceAsyncDelegate(State state, Action<string> callback);


        /// <summary>
        /// The mode of the state, represents the state is either normal, final or transient.
        /// </summary>
        /// <value></value>
        public StateMode Mode { get => _mode; }

        /// <summary>
        /// Creates an instance of the state.
        /// </summary>
        /// <param name="id">State ID value. All state IDs must be unique.</param>
        public State(string id)
        {
            this._id = id;
            _serviceDelegates = new List<InvokeServiceAsyncDelegate>();
            _transitions = new Dictionary<string, string>();
            _activities = new List<Action>();
            _mode = StateMode.Normal;
        }

        /// <summary>
        /// Invokes actions registered on enter.
        /// </summary>
        internal void InvokeEnterActions()
        {
            if (_onEnterActions != null)
            {
                _onEnterActions();
            }
        }

        /// <summary>
        /// Invokes actions registered on exit.
        /// </summary>
        internal void InvokeExitActions()
        {
            if (_onExitActions != null)
            {
                _onExitActions();
            }
        }

        /// <summary>
        /// Invokes the service and activities cleanup actions chain.
        /// </summary>
        internal void InvokeCleanupActions()
        {
            if (_serviviceCleanupDelegates != null)
            {
                _serviviceCleanupDelegates();
            }
        }

        /// <summary>
        /// Invokes the service as async method.
        /// </summary>
        /// <param name="invoke">The service to invoke.</param>
        /// <returns></returns>
        public State WithInvoke(InvokeServiceAsyncDelegate invokeAsync, Action cleanUpAction = null)
        {
            // save invoke action to execute
            _serviceDelegates.Add(invokeAsync);

            // save the clean up action if given
            if (cleanUpAction != null)
            {
                AddCleanupActionToChain(cleanUpAction);
            }

            // return current state to be able to chain up the services.
            return this;
        }

        /// <summary>
        /// Adds the cleanup action to chain of actions.
        /// </summary>
        /// <param name="cleanUpAction">Action to add.</param>
        private void AddCleanupActionToChain(Action cleanUpAction)
        {
            if (_serviviceCleanupDelegates == null)
            {
                _serviviceCleanupDelegates = cleanUpAction;
            }
            else
            {
                _serviviceCleanupDelegates += cleanUpAction;
            }
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

        /// <summary>
        /// Transition to another state after the timespan elapsed.
        /// </summary>
        /// <param name="delay">Time after what to make a transition</param>
        /// <param name="targetStateId">The ID of the target state.</param>
        public State WithDelayedTransition(TimeSpan delay, string targetStateId)
        {
            var eventId = Guid.NewGuid().ToString();
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            // create service and transition to go to after time delay
            return WithTransition(eventId, targetStateId)
            .WithInvoke(async (state, callback) =>
            {
                // wait for delay
                // pass token if this timeout is canceled by another service invokation
                // NOTE!: if delay got canceled, the exception is thrown,
                // hence we use .ContinueWith(...) to rid off unnecessary exception here.
                await Task.Delay(delay, tokenSource.Token).ContinueWith(t => {});

                // transition to another state only if token was not canceled
                if (!tokenSource.IsCancellationRequested)
                    callback(eventId);
            }, () =>
            {
                // if the state is exiting because of another service invoking callback,
                // need to stop this timeout awaiting and exit too
                tokenSource.Cancel();
            });
        }

        /// <summary>
        /// Transition to another state after the timespan elapsed.
        /// </summary>
        /// <param name="miliseconds">Time after what to make a transition in miliseconds</param>
        /// <param name="targetStateId">The ID of the target state.</param>
        public State WithDelayedTransition(int miliseconds, string targetStateId)
        {
            return WithDelayedTransition(TimeSpan.FromMilliseconds(miliseconds), targetStateId);
        }


        /// <summary>
        /// Adds actions to be executed when state enters and before the services.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public State WithActionOnEnter(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_onEnterActions is null)
            {
                _onEnterActions = action;
            }
            else
            {
                _onEnterActions += action;
            }
            return this;
        }

        /// <summary>
        /// Adds actions to be executed when state exits and before the transition to the next state.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public State WithActionOnExit(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_onExitActions is null)
            {
                _onExitActions = action;
            }
            else
            {
                _onExitActions += action;
            }
            return this;
        }

        /// <summary>
        /// Registers the activity to run in parallel to services while state is active.
        /// Activity can't change the state, it can only execute background operations.
        /// </summary>
        /// <param name="activity">The activity to run.</param>
        /// <param name="cleanUpAction">Clean up action to be called when the state machine leaves the state.</param>
        /// <returns></returns>
        public State WithActivity(Action activity, Action cleanUpAction = null)
        {
            if (activity != null)
            {
                Activities.Add(activity);
            }
            if (cleanUpAction != null)
            {
                AddCleanupActionToChain(cleanUpAction);
            }
            return this;
        }


        /// <summary>
        /// Sets the state to be a transient state and stores the next state ID to switch right after the onEnter and onExit actions are executed.
        /// </summary>
        /// <param name="targetStateId">Next state ID to switch the state machine to.</param>
        public State AsTransientState(string targetStateId)
        {
            this._mode = StateMode.Transient;
            return this.WithTransition("", targetStateId);
        }
    }
}