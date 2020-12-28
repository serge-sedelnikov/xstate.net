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
        public void ExecuteStateMachineAsService()
        {

            bool childState1Called = false;
            bool childFinalStateCalled = false;

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


            // compose host state machine
            State state1 = new State("state1");
            state1.WithInvoke(childMachine, "state2");


            var machine = new StateMachine("machine1", "machine 1", "state1", state1);
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();
        }
    }
}