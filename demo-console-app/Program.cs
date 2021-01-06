using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app
{
    class Program
    {

        static void Main(string[] args)
        {

            Console.WriteLine("Demo application to demonstrate the state machien engine");

            // ==================== TRAFFIC LIGHT DEMO ============================
            TrafficLight.TrafficLightDemoRunner.Run();

            // ==================== MQTT DEMO ============================
            //MqttDemo.MqttDemoRunner.Run();

            // never exit the program
            while (true)
            {
                // this should block main thread, but state machine
                // is executed and interpreted in own thread.
                Console.ReadLine();
            }

        }

        void Demo()
        {
            // create state. Each state has a unique ID
            State myState1 = new State("myState1");
            State myState2 = new State("myState2");
            State myState3 = new State("myState3");
            State myState4 = new State("myState4");

            // create state machine
            // provide ID, name, and initial state ID
            StateMachine machine = new StateMachine("myMachine1", "My Machine", "myState1");
            // fill up states
            machine.States = new []{
                myState1, myState2, myState3, myState4
            };

            Interpreter interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();
            // you can subscribe for state changed event
            interpreter.OnStateChanged += (sender, args) => {
                Console.WriteLine("Current state: " + args.State.Id);
                Console.WriteLine("Previous state: " + args.PreviousState.Id);
            };
            // you can subscribe for machine done event
            interpreter.OnStateMachineDone += (sender, args) => {
                Console.WriteLine("State machine finalized!");
            };
        }
    }
}
