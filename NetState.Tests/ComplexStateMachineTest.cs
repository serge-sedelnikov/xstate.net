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
            await interpreter.StartStateMachineAsync();

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
            await interpreter.StartStateMachineAsync();

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
            .WithInvoke(async (cancel) =>
            {
                await Task.Delay(2000);
                // this should be checked, if cancel was requested, this is optional but valuable feature
                if (!cancel.IsCancellationRequested)
                    state1Finalized = true;
            }, "asyncStateTransition")
            .WithInvoke(async (callback) =>
            {
                await Task.Delay(500);
                callback("NORMAL_CALLBACK_CALLED");
            })
            .WithTransition("NORMAL_CALLBACK_CALLED", "normalStateTransition")
            .WithActionOnExit(() =>
            {
                state1CleanedUp = true;
            });

            State state2 = new State("normalStateTransition")
            .WithActionOnEnter(() =>
            {
                // this should happen
                normalStateTransitionHappened = true;
            });

            State state3 = new State("asyncStateTransition")
            .WithActionOnEnter(() =>
            {
                // this should never happen
                asyncStateTransitionHappened = true;
            });

            var machine = new StateMachine("machine1", "machine2", "state1", state1, state2, state3);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();

            await Task.Delay(3000);
            Assert.False(state1Finalized);
            Assert.True(state1CleanedUp);
            Assert.True(normalStateTransitionHappened);
            Assert.False(asyncStateTransitionHappened);
        }

        [Fact]
        public async Task AsyncStateCanceledOnAsyncStateTransition()
        {
            // this is quite rare case, normally those services are not mixed
            bool state1Finalized = false;
            bool state1CleanedUp = false;
            bool normalStateTransitionHappened = false;
            bool asyncStateTransitionHappened = false;

            State state1 = new State("state1")
            .WithInvoke(async (cancel) =>
            {
                await Task.Delay(2000);
                // this should be checked, if cancel was requested, this is optional but valuable feature
                if (!cancel.IsCancellationRequested)
                    state1Finalized = true;
            }, "asyncStateTransition")
            .WithInvoke(async (cancel) =>
            {
                await Task.Delay(500);
            }, "normalStateTransition")
            .WithActionOnExit(() =>
            {
                state1CleanedUp = true;
            });

            State state2 = new State("normalStateTransition")
            .WithActionOnEnter(() =>
            {
                // this should happen
                normalStateTransitionHappened = true;
            });

            State state3 = new State("asyncStateTransition")
            .WithActionOnEnter(() =>
            {
                // this should never happen
                asyncStateTransitionHappened = true;
            });

            var machine = new StateMachine("machine1", "machine2", "state1", state1, state2, state3);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();

            await Task.Delay(3000);
            Assert.False(state1Finalized);
            Assert.True(state1CleanedUp);
            Assert.True(normalStateTransitionHappened);
            Assert.False(asyncStateTransitionHappened);
        }

        [Fact]
        public async Task OnErrorEventFiredOnAsyncStateService()
        {
            bool errorWasNoticed = false;
            bool state2WasCalled = false;

            var error = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                State state1 = new State("state1")
                .WithInvoke(async (cancel) =>
                {
                    await Task.Delay(100);
                    throw new Exception("Handled exception");
                }, "state2");

                State state2 = new State("state2")
                .WithActionOnEnter(() =>
                {
                    state2WasCalled = true;
                })
                .AsFinalState();

                var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2);
                var interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
                await Task.Delay(500);
            });

            errorWasNoticed = error != null;

            Assert.True(errorWasNoticed);
            Assert.False(state2WasCalled, "state 2 was called, but it should not");
        }

        [Fact]
        public async Task OnErrorEventNotFiredOnAnyErrorInCaseOfErrorTransitionGiven()
        {
            bool state2WasCalled = false;
            bool state3WasCalled = false;

            State state1 = new State("state1")
                .WithInvoke(async (cancel) =>
                {
                    await Task.Delay(100);
                    throw new Exception("Handled exception");
                }, "state2", "state3");

            State state2 = new State("state2")
            .WithActionOnEnter(() =>
            {
                state2WasCalled = true;
            })
            .AsFinalState();

            State state3 = new State("state3")
            .WithActionOnEnter(() =>
            {
                state3WasCalled = true;
            })
            .AsFinalState();

            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2, state3);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();
            await Task.Delay(500);

            // as we are having on error transition in state service, next line will not be set to true
            Assert.False(state2WasCalled, "state 2 was called, but it should not");
            Assert.True(state3WasCalled, "state 3 was not called but it should");
        }

        [Fact]
        public async Task OnErrorEventFiredOnCallbackSateService()
        {
            bool errorWasNoticed = false;
            bool state2WasCalled = false;

            Exception error = await Assert.ThrowsAsync<FormatException>(async () =>
            {
                State state1 = new State("state1")
                .WithInvoke((callback) =>
                {
                    int.Parse("error");
                    callback("DONE");
                })
                .WithTransition("DONE", "state2");

                State state2 = new State("state2")
                .WithActionOnEnter(() =>
                {
                    state2WasCalled = true;
                })
                .AsFinalState();

                var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2);
                var interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
                await Task.Delay(500);
            });

            errorWasNoticed = error != null;

            Assert.True(errorWasNoticed);
            Assert.False(state2WasCalled, "state 2 was called, but it should not");
        }
    }
}