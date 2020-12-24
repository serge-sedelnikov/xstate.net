using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;

namespace NetState.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void OnenterActionsExecuted()
        {
            bool onEnterActionRun = false;

            var state = new State("My test");
            state.WithActionOnEnter(() => {
                onEnterActionRun = true;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter();
            interpreter.StartStateMachine(stateMachine);

            // TODO: wait until state machine is done
            Task.Delay(2000).GetAwaiter().GetResult();

            Assert.True(onEnterActionRun);
        }
    }
}
