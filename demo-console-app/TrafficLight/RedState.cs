using System;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app.TrafficLight 
{
    class ShowingRedLight : State
    {
        public ShowingRedLight() : base("showingRedLight")
        {
            // wait for the red light timer
            this.WithInvoke(WaitForRedLightTimer);
            // show the red light picture
            this.WithActionOnEnter(ShowRedLight);
        }

        /// <summary>
        /// Turns on the red light.
        /// </summary>
        private void ShowRedLight()
        {
            Helper.DrawTrafficLight(Helper.Mode.Red);
        }

        /// <summary>
        /// Waits for the timer to switch to next state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        private async Task WaitForRedLightTimer(CallbackAction callback)
        {
            await Task.Delay(5000);
            callback("RED_LIGHT_DONE");
        }
    }
}