using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NetState.Tests
{
    public class FinalStateTests
    {
        [Fact]
        public async Task ServiceWithCallbackRunsUntilCallback()
        {
            bool isLoopRunning = true;
            bool isAsyncTaskRunning = true;

            State state1 = new State("state1");
            state1.WithInvoke((callback) => {
                
                // this should never exit until callback is called
                Task.Run(async () => {
                    while(!isLoopRunning)
                    {
                        await Task.Delay(500);
                    }
                }).ContinueWith((t) => {
                    isAsyncTaskRunning = false;
                });
            });

            StateMachine machine = new StateMachine("machine1", "machine 1", "state1");
            machine.States = new [] {
                state1
            };

            Interpreter interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            await Task.Delay(2000);
            isLoopRunning = true;

            Assert.False(isAsyncTaskRunning);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ActionServiceOnDoneAndError(bool error)
        {
            bool state2Triggered = false;
            bool errorStateTriggered = false;

            State state1 = new State("state1");
            state1.WithInvoke(async () => {
                await Task.Delay(1000);
                if(error)
                {
                    throw new Exception("Error in async service action");
                }
            }, "state2", "errorState");

            State state2 = new State("state2");
            state2.WithActionOnEnter(() => {
                state2Triggered = true;
                errorStateTriggered = false;
            });

            State errorState = new State("errorState");
            errorState.WithActionOnEnter(() => {
                state2Triggered = false;
                errorStateTriggered = true;
            });

            var machine = new StateMachine("machine1", "machine1", "state1");
            machine.States = new []{
                state1, state2, errorState
            };
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            await Task.Delay(3000);

            Assert.NotEqual(error, state2Triggered);
            Assert.Equal(error, errorStateTriggered);
        }

        [Fact]
        public async Task StateMachineExitsOnFinalState()
        {
            bool machineIsDone = false;
            string currentStateId = "";

            State state1 = new State("state1");
            state1.AsFinalState()
            .WithInvoke(async () => {
                await Task.Delay(100);
            });

            var machine = new StateMachine("machine1", "machine1", "state1", state1);
            // subscribe for done handler.
            machine.DoneHandler = () => {
                machineIsDone = true;
            };

            var interpreter = new Interpreter(machine);
            interpreter.OnStateChanged += (sender, args) => {
                currentStateId = args.State.Id;
            };
            interpreter.StartStateMachine();

            await Task.Delay(500);
            Assert.True(machineIsDone);
        }

        [Fact]
        public async Task StateMachineDoesNotExitsWithoutFinalState()
        {
            bool machineIsDone = false;
            string currentStateId = "";

            State state1 = new State("state1");
            state1.WithInvoke(async () => {
                await Task.Delay(100);
            });

            var machine = new StateMachine("machine1", "machine1", "state1", state1);
            // subscribe for done handler.
            machine.DoneHandler = () => {
                machineIsDone = true;
            };

            var interpreter = new Interpreter(machine);
            interpreter.OnStateChanged += (sender, args) => {
                currentStateId = args.State.Id;
            };

            interpreter.StartStateMachine();

            await Task.Delay(500);
            Assert.False(machineIsDone);
            Assert.Equal("state1", currentStateId);
        }

        [Fact]
        public async Task StateMachineOnErrorInFinalState_InitialState()
        {
            bool machineIsDone = false;
            Exception error = null;
            string currentStateId = "";

            State state1 = new State("state1");
            state1.AsFinalState()
            .WithInvoke(async () => {
                await Task.Delay(100);
                throw new Exception("Error in state execution.");
            });

            var machine = new StateMachine("machine1", "machine1", "state1", state1);
            // subscribe for done handler.
            machine.DoneHandler = () => {
                machineIsDone = true;
            };

            // subscribe for error
            machine.ErrorHandler = (e) => {
                error = e;
            };

            var interpreter = new Interpreter(machine);
            interpreter.OnStateChanged += (sender, args) => {
                currentStateId = args.State.Id;
            };
            interpreter.StartStateMachine();

            await Task.Delay(500);
            Assert.False(machineIsDone);
            Assert.NotNull(error);
        }
    }
}