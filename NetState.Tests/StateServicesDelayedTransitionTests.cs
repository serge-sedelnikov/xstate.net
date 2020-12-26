using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NetState.Tests
{
    public class StateServicesDelayedTransitionTests
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

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // wait for 6 sec to be sure
            Task.Delay(TimeSpan.FromSeconds(6)).GetAwaiter().GetResult();
            Assert.True(state2Triggered);
            Assert.False(stopwatch.IsRunning);
            // check that stopwatch timer shows about 5 sec
            Assert.InRange(stopwatch.ElapsedMilliseconds,
            TimeSpan.FromSeconds(4.9).TotalMilliseconds,
            TimeSpan.FromSeconds(5.1).TotalMilliseconds);
        }

        [Fact]
        public void DelayedTransitionRunSuccessfullyWithMiliseconds()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            bool state2Triggered = false;

            var state1 = new State("My test");
            state1.WithDelayedTransition(5000, "My test 2");

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

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // wait for 6 sec to be sure
            Task.Delay(TimeSpan.FromSeconds(6)).GetAwaiter().GetResult();
            Assert.True(state2Triggered);
            Assert.False(stopwatch.IsRunning);
            // check that stopwatch timer shows about 5 sec
            Assert.InRange(stopwatch.ElapsedMilliseconds,
            TimeSpan.FromSeconds(4.9).TotalMilliseconds,
            TimeSpan.FromSeconds(5.1).TotalMilliseconds);
        }

        [Fact]
        public void DelayedTransitionDidNotRun()
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

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // wait for 4 sec
            Task.Delay(TimeSpan.FromSeconds(4)).GetAwaiter().GetResult();
            Assert.False(state2Triggered);
            Assert.True(stopwatch.IsRunning);
        }

        [Fact]
        public void DelayedTransitionServicesCanceledAfterDelay()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            bool state1ServiceRunning = false;
            int state1ServiceCount = 0;
            bool state2Triggered = false;

            var state1 = new State("My test");
            state1.WithDelayedTransition(TimeSpan.FromSeconds(5), "My test 2")
            .WithInvoke(async (callback) =>
            {
                // start counting service
                state1ServiceRunning = true;
                while (state1ServiceRunning)
                {
                    state1ServiceCount++;
                    // count every half a secm to get 10 times to check
                    await Task.Delay(500);
                }
                // never call callback here to make sure service is canceled by delay service
            })
            .WithActionOnExit(() =>
            {
                // stop the loop
                state1ServiceRunning = false;
            });

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

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // wait for 6 sec
            Task.Delay(TimeSpan.FromSeconds(6)).GetAwaiter().GetResult();
            Assert.True(state2Triggered);
            Assert.False(stopwatch.IsRunning);

            // check that parallel service was stopped
            Assert.False(state1ServiceRunning);
            // how many times loop was running before stopped
            Assert.True(state1ServiceCount >= 10);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DelayedTransitionRunAfterNotmalStateChange(bool timeout)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            bool timeoutStateTriggered = false;
            bool successStateTriggered = false;

            var state1 = new State("My test");
            state1
            // wait for timeout or success if success is provided
            .WithDelayedTransition(TimeSpan.FromSeconds(5), "timedout")
            .WithTransition("SUCCESS_NO_TIMEOUT", "success")
            .WithInvoke(async (callback) =>
            {
                await Task.Delay(2000);
                if (!timeout)
                {
                    callback("SUCCESS_NO_TIMEOUT");
                }
            });

            // timeout state
            var timeoutState = new State("timedout")
            .WithActionOnEnter(() =>
            {
                stopwatch.Stop();
                timeoutStateTriggered = true;
                successStateTriggered = false;
            });

            // not a timeout state
            var successState = new State("success")
            .WithActionOnEnter(() =>
            {
                stopwatch.Stop();
                timeoutStateTriggered = false;
                successStateTriggered = true;
            });


            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state1, timeoutState, successState
            };

            var interpreter = new Interpreter(stateMachine);
            interpreter.StartStateMachine();

            // wait for 6 sec to be sure
            Task.Delay(TimeSpan.FromSeconds(6)).GetAwaiter().GetResult();

            Assert.Equal(timeout, timeoutStateTriggered);
            Assert.NotEqual(timeout, successStateTriggered);
            if (timeout)
            {
                Assert.InRange(stopwatch.ElapsedMilliseconds,
                TimeSpan.FromSeconds(4.9).TotalMilliseconds,
                TimeSpan.FromSeconds(5.1).TotalMilliseconds);
            }
            else 
            {
                Assert.InRange(stopwatch.ElapsedMilliseconds,
                TimeSpan.FromSeconds(1.9).TotalMilliseconds,
                TimeSpan.FromSeconds(2.1).TotalMilliseconds);
            }
            Assert.False(stopwatch.IsRunning);
        }
    }
}