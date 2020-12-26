using System;
using System.Threading.Tasks;
using XStateNet;

namespace demo_console_app.TrafficLight 
{
    class ShowingYellowLight : State
    {
        public ShowingYellowLight() : base("showingYellowLight")
        {
            // wait for the red light timer
            this.WithInvoke(WaitForYellowLightTimer);
            // show the red light picture
            this.WithActionOnEnter(ShowYellowLight);
        }

        /// <summary>
        /// Turns on the red light.
        /// </summary>
        private void ShowYellowLight()
        {
            Helper.DrawTrafficLight(Helper.Mode.Yellow);
        }

        /// <summary>
        /// Waits for the timer to switch to next state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="callback"></param>
        private async void WaitForYellowLightTimer(Action<string> callback)
        {
            await Task.Delay(3000);
            callback("YELLOW_LIGHT_DONE");
        }
    }
}