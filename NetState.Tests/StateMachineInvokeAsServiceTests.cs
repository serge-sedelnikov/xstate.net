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
            State state1 = new State("state1");


            var machine = new StateMachine("machine1", "machine 1", "state1", state1);
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();
        }
    }
}