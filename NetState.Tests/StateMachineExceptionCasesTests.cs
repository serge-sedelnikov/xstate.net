using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;
using static XStateNet.State;

namespace NetState.Tests
{
    public class StateMachineExceptionCasesTests
    {
        [Fact]
        public void CreateStateMachine_NoIdError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine(null, "mahiine 1", "state 1");
            });
        }

        [Fact]
        public void CreateStateMachine_NoNameError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine("machine1", null, "state 1");
            });
        }

        [Fact]
        public void CreateStateMachine_NoInitialStateError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine("machine1", "machine 1", null);
            });
        }

        [Fact]
        public void CreateStateMachine_EmptyIdError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine("", "mahiine 1", "state 1");
            });
        }

        [Fact]
        public void CreateStateMachine_EmptyNameError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine("machine1", "", "state 1");
            });
        }

        [Fact]
        public void CreateStateMachine_EmptyInitialStateError()
        {
            Assert.Throws<ArgumentException>(() => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "");
            });
        }

        [Fact]
        public void CreateInterpreter_NullStateMachine()
        {
            Assert.Throws<ArgumentNullException>(() => {
                Interpreter interpreter = new Interpreter(null);
            });
        }

        [Fact]
        public async Task CreateInterpreter_NoStatesInStateMachine()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");
                machine.States = null;
                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task CreateInterpreter_StartStateMachineTwise()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                State state1 = new State("state1")
                .AsFinalState()
                .WithInvoke(async (cancel) => {
                    await Task.Delay(2000);
                }, null, null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                interpreter.StartStateMachine(); // start without blocking
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task ExecuteAsyncService_ServiceIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                AsyncCancelableAction service = null;

                State state1 = new State("state1")
                .AsFinalState()
                .WithInvoke(service, null, null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task ExecuteStateMachineService_MachineIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                StateMachine childMachine = null;

                State state1 = new State("state1")
                .AsFinalState()
                .WithInvoke(childMachine, null, null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task ExecuteState_NullOnEnterAction()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                State state1 = new State("state1")
                .WithActionOnEnter(null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task ExecuteState_NullOnExitAction()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                State state1 = new State("state1")
                .WithActionOnExit(null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task ExecuteState_NullActivity()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => {
                StateMachine machine = new StateMachine("machine1", "machine 1", "state1");

                State state1 = new State("state1")
                .WithActivity(null);

                machine.States = new [] { state1 };

                Interpreter interpreter = new Interpreter(machine);
                await interpreter.StartStateMachineAsync();
            });
        }

        [Fact]
        public async Task CancelationTokenAsyncService_DoubleExecution_NoError()
        {
            bool service1Canceled = false;
            bool service2Canceled = false;

            // if two services are running and one switches the state,
            // another one is canceled by cancel token and cleanup methods are called twise.
            // here we need to handle this case
            State state1 = new State("state1")
            .WithInvoke(async (cancel) => {
                // continue with prevents task.delay to throw exception on cancel
                await Task.Delay(60000, cancel).ContinueWith((t) => {});
                service1Canceled = cancel.IsCancellationRequested;
            }, "finalState", null)
            .WithInvoke(async (cancel) => {
                await Task.Delay(2000, cancel);
                service2Canceled = cancel.IsCancellationRequested;
            }, "finalState", null);


            State finalState = new State("finalState")
            .AsFinalState();

            var machine = new StateMachine("machine1", "machine 1", "state1", state1, finalState);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();
            // no exceptions are expected
            Assert.True(service1Canceled);
            Assert.False(service2Canceled);
        }
    }
}