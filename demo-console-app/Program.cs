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
            TrafficLight.TrafficLightDemoRunner.Run();

            // ==================== MQTT DEMO ============================
            


            // never exit the program
            while (true)
            {
                // this should block main thread, but state machine
                // is executed and interpreted in own thread.
                Console.ReadLine();
            }
        }
    }
}
