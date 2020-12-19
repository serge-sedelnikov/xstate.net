using System;
using System.Diagnostics;
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

            var redState = new TrafficLight.ShowingRedLight();
            var yellowState = new TrafficLight.ShowingYellowLight();
            var greenState = new TrafficLight.ShowingGreenLight();

            redState.WithTransition("RED_LIGHT_DONE", yellowState.Id);
            yellowState.WithTransition("YELLOW_LIGHT_DONE", greenState.Id);
            greenState.WithTransition("GREEN_LIGHT_DONE", redState.Id);

            // creating state machine and setting up initial state ID,
            // as well as machine's ID and name for future usage.
            StateMachine machine = new StateMachine("myStateMachine", "Demo state machine", redState.Id);


            // set states to the state machine
            machine.States = new State[]{
                redState,
                yellowState, 
                greenState
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
