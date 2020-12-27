using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NetState.Tests
{
    public class ComplexStateMachineTest
    {
        [Fact]
        public async Task TestTwoStatesTransition()
        {
            object lockObject = new object();
            int onEnterActionCount = 0;
            int onExitActionCount = 0;
            int cleanupCount = 0;
            int serviceCount = 0;
            int activityCount = 0;
            int activityCleanupCount = 0;

            State state1 = new State("state1");

            // on enter actions
            state1.WithActionOnEnter(() =>
            {
                onEnterActionCount++;
            })
            .WithActionOnEnter(() =>
            {
                onEnterActionCount++;
            });

            // services with callback
            state1.WithInvoke((callback) =>
            {
                lock (lockObject)
                {
                    serviceCount++;
                }
                callback("DONE");
            }, () =>
            {
                cleanupCount++;
            })
            .WithInvoke((callback) =>
            {
                lock (lockObject)
                {
                    serviceCount++;
                }
            }, () =>
            {
                cleanupCount++;
            });

            // activities
            state1.WithActivity(() =>
            {
                lock (lockObject)
                {
                    activityCount++;
                }
            }, () =>
            {
                activityCleanupCount++;
            })
            .WithActivity(() =>
            {
                lock (lockObject)
                {
                    activityCount++;
                }
            }, () =>
            {
                activityCleanupCount++;
            });

            // on exit actions
            state1.WithActionOnExit(() =>
            {
                onExitActionCount++;
            })
            .WithActionOnExit(() =>
            {
                onExitActionCount++;
            });

            // transition to state 2
            state1.WithTransition("DONE", "state2");

            State state2 = new State("state2");

            StateMachine machine = new StateMachine("test", "test", "state1");
            machine.States = new[] {
                state1, state2
            };

            Interpreter interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is DONE
            await Task.Delay(1000);

            Assert.Equal(2, onEnterActionCount);
            Assert.Equal(2, serviceCount);
            Assert.Equal(2, cleanupCount);
            Assert.Equal(2, activityCount);
            Assert.Equal(2, activityCleanupCount);
            Assert.Equal(2, onExitActionCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MultipleStateCallbacks(bool failure)
        {
            bool failed = false;
            bool done = false;

            State state1 = new State("state1");
            state1.WithInvoke((callback) =>
            {
                if (!failure)
                    callback("DONE");
            })
            .WithInvoke((callback) =>
            {
                if (failure)
                    callback("FAILED");
            })
            .WithTransition("DONE", "doneState")
            .WithTransition("FAILED", "failStated");

            State doneState = new State("doneState")
            .WithActionOnEnter(() =>
            {
                done = true;
                failed = false;
            });
            State failStated = new State("failStated")
            .WithActionOnEnter(() =>
            {
                done = false;
                failed = true;
            });

            StateMachine machine = new StateMachine("test", "test", "state1");
            machine.States = new[] {
                state1,
                doneState,
                failStated
            };

            Interpreter interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is done
            await Task.Delay(1000);

            Assert.Equal(failure, failed);
            Assert.NotEqual(failure, done);
        }

        [Fact]
        public async Task AsyncStateCanceledOnNormalStateTransition()
        {
            // this is quite rare case, normally those services are not mixed
            bool state1Finalized = false;
            bool state1CleanedUp = false;
            bool normalStateTransitionHappened = false;
            bool asyncStateTransitionHappened = false;

            State state1 = new State("state1")
            .WithInvoke(async (cancel) => {
                await Task.Delay(2000);
                // this should be checked, if cancel was requested, this is optional but valuable feature
                if(!cancel.IsCancellationRequested)
                    state1Finalized = true;
            }, "asyncStateTransition")
            .WithInvoke(async (callback) => {
                await Task.Delay(500);
                callback("NORMAL_CALLBACK_CALLED");
            })
            .WithTransition("NORMAL_CALLBACK_CALLED", "normalStateTransition")
            .WithActionOnExit(() => {
                state1CleanedUp = true;
            });

            State state2 = new State("normalStateTransition")
            .WithActionOnEnter(() => {
                // this should happen
                normalStateTransitionHappened = true;
            });

            State state3 = new State("asyncStateTransition")
            .WithActionOnEnter(() => {
                // this should never happen
                asyncStateTransitionHappened = true;
            });

            var machine = new StateMachine("machine1", "machine2", "state1", state1, state2, state3);
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            await Task.Delay(3000);
            Assert.False(state1Finalized);
            Assert.True(state1CleanedUp);
            Assert.True(normalStateTransitionHappened);
            Assert.False(asyncStateTransitionHappened);
        }
    }
}