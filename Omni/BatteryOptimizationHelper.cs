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
    public class BatteryOptimizationHelper
    {
		//  starting from Android 6 (API level 23) android implements Doze power saving to extend battery life 
		//	Doze reduces battery consumption by deferring background CPU and network activity for apps when the device
		//	is unused for long period of time. While Dozing, apps' access to certain battery-intensive resources is
		//	deferred until maintenance windows.
		//	OMNI was found to doze during testing with the device sitting on the desk. Note however, that the device enters 
		//	Doze mode "if a user leaves a device unplugged and stationary for a period of time, with the screen off". Given
		//	that the device running OMNI is not stationary when in use then Doze mode should not be entered.
		//	Nonetheless this helper will help the user turn off battery optimization to avoid any possiblity of dozing.
		//	PlayStore does not like apps using permission REQUEST_IGNORE_BATTERY_OPTIMIZATIONS unless it can be demonstrated 
		//	that it is needed for app core functionality. 
		//	References:
		//		https://developer.android.com/training/monitoring-device-state/doze-standby
		//		https://stackoverflow.com/questions/39256501/check-if-battery-optimization-is-enabled-or-not-for-an-app
		//		https://stackoverflow.com/questions/39787886/how-we-can-make-sure-we-are-allowed-to-use-request_ignore_battery_optimizations-permission


		static readonly bool ENABLETHISHELPER = true;      // use to enable or disable use of this helper
															// i.e. whether to attempt to turn off battery optimizations

		static readonly string TAG = typeof(BatteryOptimizationHelper).FullName ?? "BatteryOptimizationHelper";

        public required MainActivity MainActivity { get; set; }

        public bool CheckBatteryOptimization()
		{
			Log.Info(TAG, "Checking battery optimization.");

			if (!ENABLETHISHELPER)
            {
				Log.Info(TAG, "Turning off Battery Optimization is not enabled in the code.");
				return false;
            }
			
			Object? powerManager = MainActivity.GetSystemService(Context.PowerService);
			if (powerManager is null || powerManager.GetType() != typeof(PowerManager))
			{
				Log.Info(TAG, "Could not get PowerManager system service");
				return false;
			}

			if (((PowerManager)powerManager).IsIgnoringBatteryOptimizations(MainActivity.PackageName))
            {
				Log.Info(TAG, "Battery Optimization being ignored.");
				return false;
            }
			else
            {
				Log.Info(TAG, "Battery Optimization is active.");
				return true;
            }
		}

		public void RequestIgnoreBatteryOptimization()
		{
			Log.Info(TAG, "Requesting that Battery Optimziation be ignored.");

			if (ENABLETHISHELPER)
            {
				var builder = new AlertDialog.Builder(MainActivity);
				builder.SetTitle(Resource.String.battery_optimisation_title);
				builder.SetMessage(Resource.String.battery_optimisation_message);
				builder.SetPositiveButton(Resource.String.battery_optimisation_disallow, (sender, args) =>
						{
							Intent intent = new();
							intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
							var uri = Android.Net.Uri.FromParts("package", MainActivity.PackageName, null);
							intent.SetData(uri);
                            MainActivity.StartActivityForResult(intent, 0);
                        });
				builder.Create().Show();
            }

			Log.Info(TAG, "Request made that Battery Optimization be ignored.");
		}
	}
}