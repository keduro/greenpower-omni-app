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
    public static class BluetoothHelper
    {
        public static bool CheckBluetooth(Context context)
        {
            var bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            var adapter = bluetoothManager?.Adapter;

            return (adapter != null) && (adapter.IsEnabled);
        }

        public static void RequestBluetooth(Activity activity)
        { 
            if (activity == null) return;
            var bluetoothManager = activity.GetSystemService(Context.BluetoothService) as BluetoothManager;
            var adapter = bluetoothManager?.Adapter;
            if (adapter==null)
            {
                var mainActivityView = activity.FindViewById(Resource.Id.activity_main);
                if (mainActivityView is not null)
                {
                    Snackbar.Make(mainActivityView,
                                  "This device does not support Bluetooth!",
                                  Snackbar.LengthIndefinite)
                            .SetAction("Dismiss", (o) => { })
                            .Show();
                }
                return;
            }
            var dialog = new AlertDialog.Builder(activity);
            var alert = dialog.Create();
            alert.SetTitle("Enable Bluetooth");
            alert.SetMessage("Bluetooth is needed for correct operation and is currently disabled");
            alert.SetButton((int)DialogButtonType.Positive, "Enable", (c, ev) =>
            {
                //adapter.Enable();     // Deprecated!
                var intent = new Intent(BluetoothAdapter.ActionRequestEnable);
                activity.StartActivityForResult(intent, Constants.BLUETOOTH_ENABLE_REQUEST_CODE);
            });
            alert.SetButton((int)DialogButtonType.Negative, "Cancel", (c, ev) => { });
            alert.Show();
        }

        public static List<string>? PairedDevices(Context context)
        {
            var bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            var adapter = bluetoothManager?.Adapter;
            if (adapter == null) return null;                   // emulator does not have bluetooth!
            var devices = new List<string>();

            var bondedDevices = adapter.BondedDevices;
            if (bondedDevices is not null)
            {
                foreach (var bd in bondedDevices)
                    if (bd.Name is not null) devices.Add(bd.Name);
            }

            return devices;
        }

    }
}