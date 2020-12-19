using System;

namespace demo_console_app.TrafficLight 
{ 
    static class Helper {

        public enum Mode {
            Red,
            Yellow,
            Green,
            RedGreen,
            YellowBlinking
        }

        public static void DrawTrafficLight(Mode trafficLightMode)
        {
            switch (trafficLightMode)
            {
                case Mode.Red: 
                    DrawRed();
                    break;
                case Mode.Yellow:
                    DrawYellow();
                    break;
                case Mode.Green:
                    DrawGreen();
                    break;
                default:
                    DrawDefault();
                    break;
            }
        }

        private static void DrawDefault()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
        }

        private static void DrawRed()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
        }

        private static void DrawYellow()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
        }

        private static void DrawGreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
            Console.WriteLine("----");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ** ");
            Console.WriteLine(" ** ");
        }
    }
}