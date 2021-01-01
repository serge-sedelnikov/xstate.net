using System;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app.TrafficLight 
{
    class ShowingGreenLight : State
    {
        public ShowingGreenLight() : base("showingGreenLight")
        {
            // wait for the red light timer
            this.WithInvoke(WaitForGreenLightTimer);
            // show the red light picture
            this.WithActionOnEnter(ShowGreenLight);
        }

        /// <summary>
        /// Turns on the red light.
        /// </summary>
        private void ShowGreenLight()
        {
            Helper.DrawTrafficLight(Helper.Mode.Green);
        }

        /// <summary>
        /// Waits for the timer to switch to next state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        private async Task WaitForGreenLightTimer(CallbackAction callback)
        {
            await Task.Delay(1000);
            await callback("GREEN_LIGHT_DONE");
        }
    }
}