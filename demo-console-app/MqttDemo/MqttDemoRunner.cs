using System;
using XStateNet;

namespace demo_console_app.MqttDemo
{
    public static class MqttDemoRunner
    {
        public static void Run()
        {
            State connectingToMqtt = new State("connectingToMqtt")
            .WithInvoke(async (callback) =>
            {

            })
            .WithTransition("MQTT_CONNECTED", "waitingUserToUnlockTheDoor");

            State waitingUserToUnlockTheDoor = new State("waitingUserToUnlockTheDoor")
            .WithInvoke(async (callback) =>
            {

            })
            .WithTransition("DOOR_UNLOCKED", "waitingUserToOpenTheDoor");

            State waitingUserToOpenTheDoor = new State("waitingUserToOpenTheDoor")
            .WithTimeout(7000, "userDidNotOpenTheDoor")
            .WithInvoke(async (callback) =>
            {

            })
            .WithTransition("DOOT_OPENED", "exitingTheApplication");

            State exitingTheApplication = new State("exitingTheApplication")
            .AsFinalState()
            .WithActionOnEnter(() =>
            {
                Console.WriteLine("User has opened the door, state machine is finalized!");
            });
        }
    }
}