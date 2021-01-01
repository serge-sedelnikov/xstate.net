using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;

namespace NetState.Tests
{
    public class LoopMachineTests
    {
        [Fact]
        public async Task RunCallbackLoopMachineTest()
        {
            int invokeCount = 0;
            int maxCount = 5;

            State state1 = new State("state1")
            .WithInvoke(async (callback) =>
            {
                Interlocked.Increment(ref invokeCount);
                if (invokeCount < maxCount)
                {
                    
                    await callback("GOTO_STATE2");
                }
                else
                {
                    await callback("DONE");
                }
            })
            .WithTransition("GOTO_STATE2", "state2")
            .WithTransition("DONE", "final");

            State state2 = new State("state2")
            .WithInvoke(async (callback) =>
            {
                Interlocked.Increment(ref invokeCount);
                if (invokeCount < maxCount)
                {
                    
                    await callback("GOTO_STATE1");
                }
                else
                {
                    await callback("DONE");
                }
            })
            .WithTransition("GOTO_STATE1", "state1")
            .WithTransition("DONE", "final");

            State finalState = new State("final")
            .AsFinalState();


            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2, finalState);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();

            Assert.Equal(maxCount, invokeCount);
        }
    }
}