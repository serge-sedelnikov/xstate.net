using System;
using System.Threading;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app
{
    class Program
    {
        private static string _userInput;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // creating state machine and setting up initial state ID,
            // as well as machine's ID and name for future usage.
            StateMachine machine = new StateMachine("myStateMachine", "Demo state machine", "showingRedLight");

            // setting up states for machine
            var redLightState = new State("showingRedLight");
            // adding transitions
            redLightState.WithTransition("ON_REDLIGHT_DONE", "showingYellowLight")
            // adding invocation services
            .WithInvoke((s, callback) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Entering the red light state");

                var t1 = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    callback("ON_REDLIGHT_DONE");
                });

                // return the destructor, this is called when state machine is leaving the state
                return () =>
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Exiting the red light state.");
                };
            });


            var yellowLightState = new State("showingYellowLight");

            // define the OnEnter actions
            yellowLightState.WithActionOnEnter(() =>
            {
                Console.WriteLine("Cleaning the user input");
                _userInput = null;
            })

            // define transition events and state names
            .WithTransition("YELLOW_TIMEOUT_20000", "showingRedLight")
            .WithTransition("MANUAL_YELLOW_TO_RED_SWITCH", "showingRedLight")

            // define services to invoke
            .WithInvoke((s, callback) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Entering yellow light. Write 'red' to manually switch to red, or wait for 20 sec.");

                CancellationTokenSource cancelSource = new CancellationTokenSource();
                var ct = cancelSource.Token;

                var t1 = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (cancelSource.IsCancellationRequested)
                        {
                            Console.WriteLine("Keyboard red cancelled");
                            return;
                        }

                        var color = _userInput;
                        if (color == "red")
                        {
                            cancelSource.Cancel();
                            callback("MANUAL_YELLOW_TO_RED_SWITCH");
                        }
                        await Task.Delay(500);
                    }

                }, cancelSource.Token);

                return () =>
                {
                    cancelSource.Cancel();
                    Console.WriteLine(t1.Status);
                    Console.ForegroundColor = ConsoleColor.White;
                };
            })
            .WithInvoke((s, callback) =>
            {
                CancellationTokenSource cancelSource = new CancellationTokenSource();

                var t2 = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), cancelSource.Token);
                    if (cancelSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    callback("YELLOW_TIMEOUT_20000");
                }, cancelSource.Token);

                // destructor
                return () =>
                {
                    cancelSource.Cancel();
                    Console.WriteLine(t2.Status);
                    Console.ForegroundColor = ConsoleColor.White;
                };
            });


            // set states to the state machine
            machine.States = new State[]{
                redLightState,
                yellowLightState,
            };

            // start state machine
            var interpreter = new Interpreter();
            interpreter.OnStateChanged += OnStateChanged;
            interpreter.StartStateMachine(machine);


            // try to get user input to be used in state machine
            while (true)
            {
                // this should block main thread, but state machine
                // is executed and interpreted in own thread.
                _userInput = Console.ReadLine();
            }
        }

        private static void OnStateChanged(object sender, StateChangeEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"======= State changed from '{args.PreviousState?.Id ?? "NULL"}' to '{args.State.Id}' ==========");
        }
    }
}
