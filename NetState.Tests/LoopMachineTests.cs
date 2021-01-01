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

        [Fact]
        public async Task AsyncServiceLoopRunTest()
        {
            int invokeCount = 0;
            int maxCount = 5;

            State state1 = new State("state1")
            .WithInvoke(async (cancel) =>
            {
                Interlocked.Increment(ref invokeCount);
                if (invokeCount < maxCount)
                {
                    await Task.FromResult(0);
                }
                else
                {
                    throw new Exception("Number is reached, too big number");
                }
            }, "state2", "final");

            State state2 = new State("state2")
            .WithInvoke(async (cancel) =>
            {
                Interlocked.Increment(ref invokeCount);
                if (invokeCount < maxCount)
                {
                    await Task.FromResult(0);
                }
                else
                {
                    throw new Exception("Number is reached, too big number");
                }
            }, "state1", "final");

            State finalState = new State("final")
            .AsFinalState();


            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2, finalState);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();

            Assert.Equal(maxCount, invokeCount);
        }

        [Fact]
        public async Task StateMachineWithTimeoutLoopTest()
        {
            int invokeCount = 0;
            int maxCount = 5;
            int service1InvocationCount = 0;
            int service2InvocationCount = 0;

            State state1 = new State("state1")
            .WithTimeout(1000, "state2")
            .WithTransition("DONE", "final")
            .WithInvoke(async (callback) =>
            {
                Interlocked.Increment(ref invokeCount);
                Interlocked.Increment(ref service1InvocationCount);
                if (invokeCount >= maxCount)
                {
                    await callback("DONE");
                }
            });

            State state2 = new State("state2")
            .WithTimeout(1000, "state1")
            .WithTransition("DONE", "final")
            .WithInvoke(async (callback) =>
            {
                Interlocked.Increment(ref invokeCount);
                Interlocked.Increment(ref service2InvocationCount);
                if (invokeCount >= maxCount)
                {
                    await callback("DONE");
                }
            });

            State finalState = new State("final")
            .AsFinalState();


            var machine = new StateMachine("machine1", "machine 1", "state1", state1, state2, finalState);
            var interpreter = new Interpreter(machine);
            await interpreter.StartStateMachineAsync();

            Assert.Equal(maxCount, invokeCount);
            // check that no duplicates and no extra callbacks were called
            Assert.Equal(3, service1InvocationCount);
            Assert.Equal(2, service2InvocationCount);
        }
    }
}