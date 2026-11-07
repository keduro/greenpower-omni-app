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
    // To clear permissions flags whilst testing use the following commands in Tools -> Android -> Android Adb Command Prompt
    //        adb shell pm clear-permission-flags net.keduro.omni <permission> user-set user-fixed
    /*   e.g.
       adb shell pm clear-permission-flags net.keduro.omni android.permission.ACCESS_FINE_LOCATION user-set user-fixed
       adb shell pm clear-permission-flags net.keduro.omni android.permission.ACCESS_BACKGROUND_LOCATION user-set user-fixed
       adb shell pm clear-permission-flags net.keduro.omni android.permission.POST_NOTIFICATIONS user-set user-fixed
       adb shell pm clear-permission-flags net.keduro.omni android.permission.BLUETOOTH_CONNECT user-set user-fixed
    or
       adb shell pm reset-permissions net.keduro.omni
    */

    public static class PermissionsHelper
    {
        private static readonly string TAG = typeof(PermissionsHelper).FullName ?? "PermissionHelper";

        private static MainActivity? mainActivity = null;
        private static Action? requestPermissionCallback = null;
        private static ActivityResultLauncher? requestPermissionLauncher = null;

        // Groups of permissions:
        // - locationPermissions            : all permissions required to access fine location in background
        // - bluetoothPermissions           : all permissions required to connect to a bluetooth device
        // - notificationPermissions        : all permissions required to post notifications
        // - cameraNotifications            : all permissions required to use camera
        // - miscellaneousPermissions       : all other permissions required
        // The AndroidManifest should contain all of the permissions defined in the groups, and no more!
        // Note that the permissions in each group depends on which are needed for the version of android 
        // Details of permissions and which Android API they were introduced in here:
        //      https://developer.android.com/reference/android/Manifest.permission

        public static List<string> LocationPermissions { get; } = [
                Manifest.Permission.AccessFineLocation,                         // API level 1
                Manifest.Permission.AccessBackgroundLocation                    // API level 29
            ];
        public static List<string> BluetoothPermissions { get; } = [
                Manifest.Permission.Bluetooth,                                  // API level 1
                Manifest.Permission.BluetoothAdmin                              // API level 1
            ];
        public static List<string> NotificationPermissions { get; } = [
            ];
        public static List<string> CameraPermissions { get; } = [
                Manifest.Permission.Camera                                      // API level 1
            ];
        private static readonly List<string> miscellaneousPermissions = [
                Manifest.Permission.AccessNetworkState,                         // API level 1
                Manifest.Permission.ForegroundService,                          // API level 28
                Manifest.Permission.Internet,                                   // API level 1
                Manifest.Permission.RequestIgnoreBatteryOptimizations,          // API level 23
                Manifest.Permission.WakeLock                                    // API level 1
            ];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void Init(MainActivity MainActivity)
        {
            Log.Info(TAG, "Initialisation.");

            mainActivity = MainActivity;

            requestPermissionLauncher = mainActivity.RegisterForActivityResult(
                new ActivityResultContracts.RequestPermission(),
                new ActivityResultCallback(activityResult =>
                {
                    requestPermissionCallback?.Invoke();
                }));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)                // API level 31
            {
                BluetoothPermissions.Add(Manifest.Permission.BluetoothConnect);
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)         // API level 33
            {
                NotificationPermissions.Add(Manifest.Permission.PostNotifications);
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)   // API level 34
            {
                LocationPermissions.Add(Manifest.Permission.ForegroundServiceLocation);
                miscellaneousPermissions.Add(Manifest.Permission.ForegroundServiceLocation);
            }

            var allIMiscellaneousPermissionsGranted = CheckPermissions(miscellaneousPermissions);
            if (!allIMiscellaneousPermissionsGranted) throw new Exception("Not all miscellaneous permissions were granted");
        }


        public static bool CheckPermission(string Permission)
        {
            Log.Info(TAG, $"Checkpermission {Permission}.");

            if (mainActivity == null) return false;
            var granted = mainActivity.CheckSelfPermission(Permission).Equals(Android.Content.PM.Permission.Granted);
            Log.Info(TAG, $"Permission {Permission} granted: {granted}.");
            
            return granted;
        }

        public static bool CheckPermissions(List<string> Permissions)
        {
            Log.Info(TAG, $"Checkpermissions {string.Join(", ", Permissions)}.");
            
            var allGranted = true;
            foreach (var permission in Permissions)
            {
                var granted = CheckPermission(permission);
                allGranted = allGranted && granted;
            }

            Log.Info(TAG, $"Checkpermissions { string.Join(", ", Permissions)}, all granted: { allGranted}.");

            return allGranted;
        }

        public static List<string>? DeniedPermissions(List<string> Permissions)
        {
            Log.Info(TAG, $"DeniedPermissions {string.Join(", ", Permissions)}.");

            var denied = new List<string>();
            foreach (var permission in Permissions)
            {
                var granted = CheckPermission(permission);
                if (!granted) denied.Add(permission);
            }

            Log.Info(TAG, $"DeniedPermissions {string.Join(", ", Permissions)}, denied: {string.Join(", ", denied)}");

            if (denied.Count == 0) return null;

            return denied;
        }

        public static bool CheckPermissionRationaleRequired(string Permission)
        {
            Log.Info(TAG, $"CheckPermissionRationaleRequired {Permission}.");

            bool rationaleRequired;
            var granted = CheckPermission(Permission);
            if (!granted)
            {
                rationaleRequired = (mainActivity != null && mainActivity.ShouldShowRequestPermissionRationale(Permission));
            }
            else
            {
                rationaleRequired = false;
            }

            Log.Info(TAG, $"CheckPermissionRationaleRequired {Permission} rationale required: {rationaleRequired}");

            return rationaleRequired;
        }

        public static void RequestPermission(string Permission, Action? Callback = null)
        {
            Log.Info(TAG, $"Requestpermission {Permission}.");

            if (mainActivity == null) return;
            if (CheckPermission(Permission)) return;        // already granted

            if (!string.IsNullOrEmpty(Permission))
            {
                requestPermissionCallback = Callback;
                requestPermissionLauncher?.Launch(Permission);
            }
        }
    }
}