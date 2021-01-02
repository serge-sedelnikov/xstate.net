using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;

namespace NetState.Tests
{
    public class InitialStateRun
    {
        [Fact]
        public async Task OneInitialStateOneOnEnterActionExecuted()
        {
            bool onEnterActionRun = false;

            var state = new State("My test");
            state.WithActionOnEnter(() =>
            {
                onEnterActionRun = true;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is done
            await Task.Delay(2000);

            Assert.True(onEnterActionRun);
        }

        [Fact]
        public async Task OneInitialStateManyOnEnterActionExecuted()
        {
            int onEnterActionRun = 0;

            var state = new State("My test");
            state.WithActionOnEnter(() =>
            {
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                onEnterActionRun += 1;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is done
            await Task.Delay(2000);

            Assert.Equal(3, onEnterActionRun);
        }

        [Fact]
        public async Task OneInitialStateOneServiceExecuted()
        {
            bool serviceExecuted = false;

            var state = new State("My test");
            state.WithInvoke(async (callback) =>
            {
                serviceExecuted = true;
                await Task.FromResult(0);
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is done
            await Task.Delay(2000);

            Assert.True(serviceExecuted);
        }

        [Fact]
        public void NoInitialStateGiven()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var state = new State("My test 1");

                var stateMachine = new StateMachine("test", "test", "My test");
                stateMachine.States = new State[]{
                state
            };

                var interpreter = new Interpreter(stateMachine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task OneInitialStateMultipleServicesExecuted()
        {
            int serviceExecuted = 0;
            int thread1 = 0;
            int thread2 = 0;

            var state = new State("My test");
            state.WithInvoke(async (callback) =>
            {
                // executed in parallel
                thread1 = Thread.CurrentThread.ManagedThreadId;
                Interlocked.Increment(ref serviceExecuted);
                await Task.Delay(100);
                await callback("DONE");
            })
            .WithInvoke(async (callback) =>
            {
                // executed in parallel
                thread2 = Thread.CurrentThread.ManagedThreadId;
                Interlocked.Increment(ref serviceExecuted);
                await Task.FromResult(0);
                await callback("DONE");
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // TODO: wait until state machine is done
            await Task.Delay(2000);

            Assert.Equal(2, serviceExecuted);
            // services are running in the same managed thread ID
            Assert.Equal(thread1, thread2);
        }

        [Fact]
        public async Task NoStateToTransitionExist()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var state = new State("My test");
                state.WithTransition("DONE", "next state")
                .WithInvoke(async (callback) =>
                {
                    await callback("DONE");
                });

                var stateMachine = new StateMachine("test", "test", "My test");
                stateMachine.States = new State[]{
                    state
                };

                var interpreter = new Interpreter(stateMachine);
                await interpreter.StartStateMachineAsync();
                await Task.Delay(1000);
            });
        }

        [Fact]
        public async Task NoTransitionExist_NoException()
        {
            var state1 = new State("My test");
            state1.WithTransition("DONE", "My test 2")
            .WithInvoke(async (callback) =>
            {
                await callback("NOT_EXISTS_NOT_REGISTERED");
            });

            var state2 = new State("My test 2");

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                    state1,
                    state2
                };

            var interpreter = new Interpreter(stateMachine);

            var newStateId = "";
            var prevStateId = "";
            interpreter.OnStateChanged += (sender, args) =>
            {
                newStateId = args.State.Id;
                prevStateId = args.PreviousState?.Id;
            };

            interpreter.StartStateMachine();

            await Task.Delay(1000);

            Assert.Equal("My test", newStateId);
            Assert.Null(prevStateId);
        }

        [Fact]
        public async Task OnStateChangeEvent_Executed()
        {
            var state1 = new State("My test");
            state1.WithTransition("DONE", "My test 2")
            .WithInvoke(async (callback) =>
            {
                await Task.Delay(100);
                await callback("DONE");
            });

            var state2 = new State("My test 2");

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                    state1,
                    state2
                };

            var interpreter = new Interpreter(stateMachine);

            var newStateId = "";
            var prevStateId = "";
            interpreter.OnStateChanged += (sender, args) =>
            {
                newStateId = args.State.Id;
                prevStateId = args.PreviousState?.Id;
            };

            interpreter.StartStateMachine();

            await Task.Delay(1000);

            Assert.Equal("My test 2", newStateId);
            Assert.Equal("My test", prevStateId);
        }
    }
}
