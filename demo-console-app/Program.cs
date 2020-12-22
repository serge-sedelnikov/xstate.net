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
        private static Stopwatch _stopwatch;

        private static bool _isPrintLoopRunning = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            _stopwatch = new Stopwatch();
            _stopwatch.Start();           

            var redState = new TrafficLight.ShowingRedLight();
            var yellowState = new TrafficLight.ShowingYellowLight();
            var greenState = new TrafficLight.ShowingGreenLight();

            redState.WithTransition("RED_LIGHT_DONE", yellowState.Id);
            yellowState.WithTransition("YELLOW_LIGHT_DONE", greenState.Id);
            greenState.WithTransition("GREEN_LIGHT_DONE", redState.Id);

            redState.WithActivity(PrintElapsedTime, StopPrintElapsedTime);
            yellowState.WithActivity(PrintElapsedTime, StopPrintElapsedTime);
            greenState.WithActivity(PrintElapsedTime, StopPrintElapsedTime);

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

        private static void PrintElapsedTime()
        {
            _isPrintLoopRunning = true;

            Action runPrintLoop = new Action(async () => {
                while(_isPrintLoopRunning)
                {
                    await Task.Delay(150);
                    Console.Write($"\r{_stopwatch.Elapsed.ToString()}");
                }
            });

            Task.Run(runPrintLoop);
        }

        private static void StopPrintElapsedTime()
        {
            _isPrintLoopRunning = false;
             _stopwatch.Restart();
        }

        private static void OnStateChanged(object sender, StateChangeEventArgs args)
        {
            
        }
    }
}
