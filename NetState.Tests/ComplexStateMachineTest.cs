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

            Interpreter interpreter = new Interpreter();
            interpreter.StartStateMachine(machine);

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

            Interpreter interpreter = new Interpreter();
            interpreter.StartStateMachine(machine);

            // TODO: wait until state machine is done
            await Task.Delay(1000);

            Assert.Equal(failure, failed);
            Assert.NotEqual(failure, done);
        }
    }
}