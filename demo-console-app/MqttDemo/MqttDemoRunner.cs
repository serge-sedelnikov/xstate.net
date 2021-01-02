using System;
using XStateNet;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.MqttClient;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;

namespace demo_console_app.MqttDemo
{
    public static class MqttDemoRunner
    {
        private const string MQTT_BROKER_ADDRESS = "broker.mqttdashboard.com";
        private const string DOOR_UNLOCK_TOPIC = "/netstate/door-unlock";
        private const string DOOR_OPEN_TOPIC = "/netstate/door-open";
        private const string DOOR_CLOSE_TOPIC = "/netstate/door-close";

        private static MqttClient _client;
        private static bool _doorUnlocked = false;
        private static bool _doorOpened = false;

        /// <summary>
        /// On MQTT message received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void MqttMessageReceived(object sender, MqttMsgPublishEventArgs args)
        {
            Console.WriteLine($"MQTT message reeived for topic: {args.Topic}");
            switch (args.Topic)
            {
                case DOOR_UNLOCK_TOPIC:
                    _doorUnlocked = true;
                    break;
                default:
                    Console.WriteLine($"Unknown topic was received: {args.Topic}");
                    break;
            }
        }

        /// <summary>
        /// Runs the state machine to demo MQTT events pub/sub logic.
        /// </summary>
        public static void Run()
        {
            State connectingToMqtt = new State("connectingToMqtt")
            .WithInvoke(async (callback) =>
            {
                Console.WriteLine("Connecting to MQTT...");
                // create client instance
                _client = new MqttClient(MQTT_BROKER_ADDRESS);
                _client.MqttMsgPublishReceived += MqttMessageReceived;
                // connect
                string clientId = Guid.NewGuid().ToString();
                try
                {
                    var result = _client.Connect(clientId);
                    Console.WriteLine("MQTT connected!");
                    await callback("MQTT_CONNECTED");
                }
                catch(SocketException error)
                {
                    Console.WriteLine("MQTT connection error!");
                    Console.WriteLine(error);
                    await callback("MQTT_ERROR");
                }
                
            })
            .WithTransition("MQTT_CONNECTED", "waitingUserToUnlockTheDoor")
            .WithTransition("MQTT_ERROR", "couldNotConnectToMqtt");

            // if we can't connect to MQTT, notify user and retry. We can't continue without connection.
            State couldNotConnectToMqtt = new State("couldNotConnectToMqtt")
            .WithActionOnEnter(() => {
                Console.WriteLine("Could not connect to MQTT! Retrying in 3 sec.");
            })
            .WithTimeout(3000, "connectingToMqtt");
            
            // =====================================================================

            State waitingUserToUnlockTheDoor = new State("waitingUserToUnlockTheDoor")
            .WithInvoke(async (callback) =>
            {
                Console.WriteLine("Door is locked, waiting for the remote message to come to unlock it");
                // subscribe to the topic "/home/temperature" with QoS 2
                _client.Subscribe(new string[] { DOOR_UNLOCK_TOPIC },
                    new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                
                // wait until door is unlocked via MQTT message
                while (!_doorUnlocked)
                {
                    await Task.Delay(500);
                }
                
                Console.WriteLine("Door is unlocked, waiting for user to open it!");
                await callback("DOOR_UNLOCKED");
            }, () =>
            {
                // when state machine leaving the state, unsubscribe to not to receive any messages
                _client.Unsubscribe(new string[] { DOOR_UNLOCK_TOPIC });
            })
            .WithTransition("DOOR_UNLOCKED", "waitingUserToOpenTheDoor");

            // =====================================================================

            var doorOpenTimeout = TimeSpan.FromSeconds(5);
            State waitingUserToOpenTheDoor = new State("waitingUserToOpenTheDoor")
            .WithTimeout(doorOpenTimeout, "userDidNotOpenTheDoor")
            .WithActivity(async () => {
                // indicate 30 sec progress bar
                var progress = new Stopwatch();
                progress.Start();
                while(progress.Elapsed < doorOpenTimeout)
                {
                    await Task.Delay(100);
                    double elapsedPercent = Math.Floor(100 * progress.ElapsedMilliseconds / doorOpenTimeout.TotalMilliseconds);
                    double secondsElapsed = Math.Floor(progress.Elapsed.TotalSeconds);
                    // compose progress string as "*****------ 50% (15/30 sec.)" with indicators
                    string message = $"{secondsElapsed}/{doorOpenTimeout.TotalSeconds}sec.";
                    string progressBar = "";
                    int progressbarLength = 30;
                    for(var i = 0; i < progressbarLength; i++)
                    {
                        double iProgressPercent = Math.Floor(100.0 * i / progressbarLength);
                        progressBar = progressBar + (iProgressPercent < elapsedPercent ? "=" : "-");
                    }
                    Console.Write($"\r[{progressBar}] {message}");
                }
                progress.Stop();
                Console.WriteLine();
            })
            .WithInvoke(async (callback) =>
            {
                await Task.FromResult(0);
            })
            .WithTransition("DOOT_OPENED", "exitingTheApplication");

            // =====================================================================

            State userDidNotOpenTheDoor = new State("userDidNotOpenTheDoor")
            .WithActionOnEnter(() => {
                Console.WriteLine("Door was not opened in time, locking the door");
                _doorUnlocked = false;
            })
            .AsTransientState("waitingUserToUnlockTheDoor");

            // =====================================================================

            State exitingTheApplication = new State("exitingTheApplication")
            .AsFinalState()
            .WithActionOnEnter(() =>
            {
                Console.WriteLine("User has opened the door, state machine is finalized!");
            });


            var machine = new StateMachine("mqttMachine", "mqtt machine", "connectingToMqtt");
            machine.States = new[]
            {
                connectingToMqtt,
                waitingUserToUnlockTheDoor,
                waitingUserToOpenTheDoor,
                exitingTheApplication,
                userDidNotOpenTheDoor,
                couldNotConnectToMqtt
            };
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();
        }
    }
}