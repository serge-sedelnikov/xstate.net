using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;

namespace NetState.Tests
{
    public class StateMachineInvokeAsServiceTests
    {
        [Fact]
        public async Task ExecuteStateMachineAsService()
        {
            bool childState1Called = false;
            bool childFinalStateCalled = false;
            bool state2Called = false;

            // compose service state machine
            State childState1 = new State("childState1");
            childState1.WithInvoke((callback) =>
            {
                childState1Called = true;
                callback("DONE");
            })
            .WithTransition("DONE", "childFinalState");

            State childFinalState = new State("childFinalState")
            .AsFinalState()
            .WithInvoke(async (cancel) =>
            {
                await Task.FromResult(0);
                childFinalStateCalled = true;
            });
            var childMachine = new StateMachine("childMachine", "childMachine", childState1.Id,
            childState1, childFinalState);


            // ==================compose host state machine==================
            State state1 = new State("state1");
            state1.WithInvoke(childMachine, "state2");

            State state2 = new State("state2")
            .AsFinalState()
            .WithInvoke(async (cancel) =>
            {
                await Task.FromResult(0);
                state2Called = true;
            });


            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2);
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            await Task.Delay(1000);
            Assert.True(childState1Called);
            Assert.True(childFinalStateCalled);
            Assert.True(state2Called);
        }


        [Fact]
        public async Task CancelStateMachineExecutionByAnotherService()
        {
            // this is not a normal case as usually state executes only one state machine as a service

            bool childState1Called = false;
            bool childFinalStateCalled = false;
            bool state2Called = false;

            // compose service state machine
            State childState1 = new State("childState1");
            childState1.WithInvoke((callback) =>
            {
                Task.Delay(3000).GetAwaiter().GetResult();
                childState1Called = true;
                callback("DONE");
            })
            .WithTransition("DONE", "childFinalState");

            State childFinalState = new State("childFinalState")
            .AsFinalState()
            .WithInvoke(async (cancel) =>
            {
                await Task.Delay(3000);
                childFinalStateCalled = true;
            });
            var childMachine = new StateMachine("childMachine", "childMachine", childState1.Id,
            childState1, childFinalState);


            // ==================compose host state machine==================
            State state1 = new State("state1");
            state1.WithInvoke(childMachine, "state2")
            .WithInvoke(async (cancel) =>
            {
                await Task.Delay(100);
            }, "state2");

            State state2 = new State("state2")
            .AsFinalState()
            .WithInvoke(async (cancel) =>
            {
                await Task.FromResult(0);
                state2Called = true;
            });


            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2);
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();

            await Task.Delay(4000);
            Assert.True(childState1Called);
            // as state machine has been forced to stop, this should be false
            Assert.False(childFinalStateCalled);
            Assert.True(state2Called);
        }
    }
}