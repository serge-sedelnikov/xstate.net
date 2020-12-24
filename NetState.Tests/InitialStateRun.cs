using System;
using Xunit;
using XStateNet;
using System.Threading.Tasks;
using System.Threading;

namespace NetState.Tests
{
    public class InitialStateRun
    {
        [Fact]
        public void OneInitialStateOneOnEnterActionExecuted()
        {
            bool onEnterActionRun = false;

            var state = new State("My test");
            state.WithActionOnEnter(() =>
            {
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

        [Fact]
        public void OneInitialStateManyOnEnterActionExecuted()
        {
            int onEnterActionRun = 0;

            var state = new State("My test");
            state.WithActionOnEnter(() =>
            {
                Console.WriteLine("1");
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                Console.WriteLine("2");
                onEnterActionRun += 1;
            }).WithActionOnEnter(() =>
            {
                Console.WriteLine("3");
                onEnterActionRun += 1;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter();
            interpreter.StartStateMachine(stateMachine);

            // TODO: wait until state machine is done
            Task.Delay(2000).GetAwaiter().GetResult();

            Assert.Equal(3, onEnterActionRun);
        }

        [Fact]
        public void OneInitialStateOneServiceExecuted()
        {
            bool serviceExecuted = false;

            var state = new State("My test");
            state.WithInvoke((state, callback) =>
            {
                serviceExecuted = true;
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter();
            interpreter.StartStateMachine(stateMachine);

            // TODO: wait until state machine is done
            Task.Delay(2000).GetAwaiter().GetResult();

            Assert.True(serviceExecuted);
        }

        [Fact]
        public void NoInitialStateGiven()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var state = new State("My test 1");

                var stateMachine = new StateMachine("test", "test", "My test");
                stateMachine.States = new State[]{
                state
            };

                var interpreter = new Interpreter();
                interpreter.StartStateMachine(stateMachine);
            });
        }

        [Fact]
        public void OneInitialStateMultipleServicesExecuted()
        {
            object lockObject = new object();
            int serviceExecuted = 0;
            int thread1 = 0;
            int thread2 = 0;

            var state = new State("My test");
            state.WithInvoke((state, callback) =>
            {
                // executed in parallel
                thread1 = Thread.CurrentThread.ManagedThreadId;
                lock(lockObject)
                {
                    serviceExecuted += 1;
                }
            })
            .WithInvoke((state, callback) =>
            {
                // executed in parallel
                thread2 = Thread.CurrentThread.ManagedThreadId;
                lock(lockObject)
                {
                    serviceExecuted += 1;
                }
            });

            var stateMachine = new StateMachine("test", "test", "My test");
            stateMachine.States = new State[]{
                state
            };

            var interpreter = new Interpreter();
            interpreter.StartStateMachine(stateMachine);

            // TODO: wait until state machine is done
            Task.Delay(2000).GetAwaiter().GetResult();

            Assert.Equal(2, serviceExecuted);
            Assert.NotEqual(thread1, thread2);
        }
    }
}
