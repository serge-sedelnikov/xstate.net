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

            Interpreter interpreter = new Interpreter();
            interpreter.StartStateMachine(machine);

            await Task.Delay(2000);
            isLoopRunning = true;

            Assert.False(isAsyncTaskRunning);
        }

        [Fact]
        public void FinalStateExitsStateMachineOnAllServicesDone()
        {
            State state1 = new State("state1");
            state1.WithInvoke(() => {

            });
        }
    }
}