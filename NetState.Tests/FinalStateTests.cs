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
    }
}