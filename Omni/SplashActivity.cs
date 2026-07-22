/* 
    The OMNI telemetry app logs data and uploads data captured by 
    eChook.uk systems for Greenpower electric cars.
    Copyright (C) 2026  Keith Roberts

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Omni
{
    [Activity(Label = "OMNI telemetry launcher", Theme = "@style/AppTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        static readonly string TAG = typeof (SplashActivity).FullName ?? "SplashActivity";

        bool simulation = false;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState); //, persistentState);
            Log.Debug(TAG, "SplashActivity.OnCreate");

            SetContentView(Resource.Layout.activity_splash);

            // Prevent the back button from canceling the startup process

            OnBackPressedDispatcher.AddCallback(new OnBackPressedCallback(() => {  }));

            if (Shared.FirstRun)
            {
                View? view = FindViewById(Resource.Id.activity_splash);
                if (view != null)
                {
                    Snackbar.Make(view,
                                  GetString(Resource.String.snackbar_demonstration),
                                  Snackbar.LengthIndefinite)
                            .SetAction(GetString(Resource.String.snackbar_go), (o) => { simulation = true; })
                            .Show();
                }
                Shared.FirstRun = false;
            }
        }

        // Launches the startup task
        protected override async void OnResume()
        {
            base.OnResume();
            await Task.Delay(Constants.SPLASH_SCREEN_INTERVAL);
            var intent = new Intent(Application.Context, typeof(MainActivity));
            intent.PutExtra(Constants.SIMULATION_NAME, simulation);
            StartActivity(intent);
        }

    }
}