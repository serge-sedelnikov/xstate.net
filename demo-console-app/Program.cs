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
            // TrafficLight.TrafficLightDemoRunner.Run();

            // ==================== MQTT DEMO ============================
            //MqttDemo.MqttDemoRunner.Run();


            Demo();
            
            // never exit the program
            while (true)
            {
                // this should block main thread, but state machine
                // is executed and interpreted in own thread.
                Console.ReadLine();
            }
        }

        static async Task Demo() 
        {
            // starting the stopwatch
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // creating the state with three on enter actions
            // all of them are asyncronouse.
            State state1 = new State("myState")
            .WithActionOnEnter(async () => {
                // in each action we wait for 10 seconds
                await Task.Delay(10000);
                Console.WriteLine("Action 1");
                Console.WriteLine(watch.Elapsed);
            })
            .WithActionOnEnter(async () => {
                await Task.Delay(10000);
                Console.WriteLine("Action 2");
                Console.WriteLine(watch.Elapsed);
            })
            .WithActionOnEnter(async () => {
                await Task.Delay(10000);
                Console.WriteLine("Action 3");
                Console.WriteLine(watch.Elapsed);
            });

            // starting the state machine
            var machine = new StateMachine("myMachine", "my machine", "myState", state1);
            var interpreter = new Interpreter(machine);
            // waiting untl machine is over
            await interpreter.StartStateMachineAsync();
        }
    }
}
