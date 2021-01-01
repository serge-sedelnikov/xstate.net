using System;
using System.Diagnostics;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app.TrafficLight
{
    public static class TrafficLightDemoRunner
    {
        private static Stopwatch _stopwatch;

        private static bool _isPrintLoopRunning = false;
        
        public static void Run()
        {
            Console.WriteLine("Hello World!");

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            var redState = new TrafficLight.ShowingRedLight();
            var yellowState = new TrafficLight.ShowingYellowLight();
            var greenState = new TrafficLight.ShowingGreenLight();

            // organize transition state
            State restartingTrafficLight = new State("uploadingTrafficLightHealth")
            .AsTransientState(redState.Id)
            .WithActionOnEnter(() =>
            {
                _stopwatch.Restart();
                Console.Clear();
            })
            .WithActionOnExit(() =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Traffic light is healthy!");
            });

            // create transitions on events for states.
            redState.WithTransition("RED_LIGHT_DONE", yellowState.Id);
            yellowState.WithTransition("YELLOW_LIGHT_DONE", greenState.Id);
            greenState.WithTransition("GREEN_LIGHT_DONE", restartingTrafficLight.Id);

            // setup side effect ectivities to print elapsed time.
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
                greenState,
                restartingTrafficLight
            };

            // start state machine
            var interpreter = new Interpreter(machine);
            interpreter.OnStateChanged += OnStateChanged;
            // run state machine in the background thread
            interpreter.StartStateMachine();
        }

        private static Task PrintElapsedTime()
        {
            _isPrintLoopRunning = true;

            Action runPrintLoop = new Action(async () =>
            {
                while (_isPrintLoopRunning)
                {
                    await Task.Delay(150);
                    Console.Write($"\r{_stopwatch.Elapsed.ToString()}");
                }
            });

            return Task.Run(runPrintLoop);
        }

        private static void StopPrintElapsedTime()
        {
            _isPrintLoopRunning = false;
        }

        private static void OnStateChanged(object sender, StateChangeEventArgs args)
        {

        }
    }
}