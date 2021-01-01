using System;
using XStateNet;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.MqttClient;
using System.Threading.Tasks;

namespace demo_console_app.MqttDemo
{
    public static class MqttDemoRunner
    {
        private const string MQTT_BROKER_ADDRESS = "broker.mqttdashboard.com";
        private static MqttClient _client;
        private static bool _doorUnlocked = false;

        private static void MqttMessageReceived(object sender, MqttMsgPublishEventArgs args)
        {
            Console.WriteLine($"MQTT message reeived for topic: {args.Topic}");
            switch (args.Topic)
            {
                case "/netstate/door-open":
                    _doorUnlocked = true;
                    break;
                default:
                    Console.WriteLine($"Unknown topic was received: {args.Topic}");
                    break;
            }
        }

        public static void Run()
        {
            State connectingToMqtt = new State("connectingToMqtt")
            .WithInvoke(async (callback) =>
            {
                Console.WriteLine("Connecting to MQTT...");
                // create client instance
                _client = new MqttClient(MQTT_BROKER_ADDRESS);
                // connect
                string clientId = Guid.NewGuid().ToString();
                _client.Connect(clientId);
                Console.WriteLine("MQTT connected!");
                await callback("MQTT_CONNECTED");
            })
            .WithTransition("MQTT_CONNECTED", "waitingUserToUnlockTheDoor");

            State waitingUserToUnlockTheDoor = new State("waitingUserToUnlockTheDoor")
            .WithInvoke(async (callback) =>
            {
                Console.WriteLine("Door is locked, waiting for the remote message to come to unlock it");
                // subscribe to the topic "/home/temperature" with QoS 2
                _client.Subscribe(new string[] { "/netstate/door-open" },
                    new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                _client.MqttMsgPublishReceived += MqttMessageReceived;
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
                _client.Unsubscribe(new string[] { "/netstate/door-open" });
                _client.MqttMsgPublishReceived -= MqttMessageReceived;
            })
            .WithTransition("DOOR_UNLOCKED", "waitingUserToOpenTheDoor");

            State waitingUserToOpenTheDoor = new State("waitingUserToOpenTheDoor")
            .WithTimeout(7000, "userDidNotOpenTheDoor")
            .WithInvoke(async (callback) =>
            {
                await Task.FromResult(0);
            })
            .WithTransition("DOOT_OPENED", "exitingTheApplication");

            State userDidNotOpenTheDoor = new State("userDidNotOpenTheDoor")
            .WithActionOnEnter(() => {
                Console.WriteLine("Door was not opened in time, locking the door");
                _doorUnlocked = false;
            })
            .AsTransientState("waitingUserToUnlockTheDoor");

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
                userDidNotOpenTheDoor
            };
            var interpreter = new Interpreter(machine);
            interpreter.StartStateMachine();
        }
    }
}