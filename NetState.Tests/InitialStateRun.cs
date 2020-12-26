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
                Console.WriteLine("1");
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                Console.WriteLine("2");
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                Console.WriteLine("3");
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
            state.WithInvoke((callback) =>
            {
                serviceExecuted = true;
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
            Assert.Throws<InvalidOperationException>(() =>
            {
                var state = new State("My test 1");

                var stateMachine = new StateMachine("test", "test", "My test");
                stateMachine.States = new State[]{
                state
            };

                var interpreter = new Interpreter(stateMachine);
                interpreter.StartStateMachine();
            });
        }

        [Fact]
        public async Task OneInitialStateMultipleServicesExecuted()
        {
            object lockObject = new object();
            int serviceExecuted = 0;
            int thread1 = 0;
            int thread2 = 0;

            var state = new State("My test");
            state.WithInvoke((callback) =>
            {
                // executed in parallel
                thread1 = Task.CurrentId.GetValueOrDefault();
                Console.WriteLine(thread1);
                lock (lockObject)
                {
                    serviceExecuted += 1;
                }
            })
            .WithInvoke((callback) =>
            {
                // executed in parallel
                thread2 = Task.CurrentId.GetValueOrDefault();
                Console.WriteLine(thread2);
                lock (lockObject)
                {
                    serviceExecuted += 1;
                }
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
            Assert.NotEqual(thread1, thread2);
        }

        [Fact]
        public void NoStateToTransitionExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var state = new State("My test");
                state.WithTransition("DONE", "next state")
                .WithInvoke((callback) =>
                {
                    callback("DONE");
                });

                var stateMachine = new StateMachine("test", "test", "My test");
                stateMachine.States = new State[]{
                    state
                };

                var interpreter = new Interpreter(stateMachine);
                interpreter.StartStateMachine();

                await Task.Delay(1000);
            });
        }

        [Fact]
        public async Task NoTransitionExist_NoException()
        {
            var state1 = new State("My test");
            state1.WithTransition("DONE", "My test 2")
            .WithInvoke((callback) =>
            {
                callback("NOT_EXISTS_NOT_REGISTERED");
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
            interpreter.OnStateChanged += (sender, args) => {
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
                await Task.Delay(500);
                callback("DONE");
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
            interpreter.OnStateChanged += (sender, args) => {
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
