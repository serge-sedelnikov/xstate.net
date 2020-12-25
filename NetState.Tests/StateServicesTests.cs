using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NetState.Tests
{
    public class StateServicesTests
    {
        [Fact]
        public void DelayedTransitionRunSuccessfully()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            bool state2Triggered = false;

            var state1 = new State("My test");
            state1.WithDelayedTransition(TimeSpan.FromSeconds(5), "My test 2");

            var state2 = new State("My test 2")
            .WithActionOnEnter(() =>
            {
                stopwatch.Stop();
                state2Triggered = true;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state1, state2
            };

            var interpreter = new Interpreter();
            interpreter.StartStateMachine(stateMachine);

            // wait for 6 sec to be sure
            Task.Delay(TimeSpan.FromSeconds(6)).GetAwaiter().GetResult();
            Assert.True(state2Triggered);
            Assert.False(stopwatch.IsRunning);
            // check that stopwatch timer shows about 5 sec
            Assert.InRange(stopwatch.ElapsedMilliseconds,
            TimeSpan.FromSeconds(5).TotalMilliseconds,
            TimeSpan.FromSeconds(5.1).TotalMilliseconds);
        }
    }
}