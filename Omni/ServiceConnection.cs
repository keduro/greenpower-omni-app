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
    public class ServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(ServiceConnection).FullName ?? "ServiceConnection";

        public MainActivity? Activity { get; set; }

        public ServiceBinder? Binder { get; set; }

        public void OnServiceConnected(ComponentName? name, IBinder? service)
        {
            var binder = service as ServiceBinder;
            this.Binder = binder;

            AttachBinderHandlers(Activity);

            Activity?.Bound = true;

            try
            {
                binder?.Service?.IsBound = true;
                binder?.Service?.Start();
            }
            catch (DeadObjectException ex) 
            {
                Log.Info(TAG, $"Exception starting service connection: {ex}");
            }

            if (Activity != null && !BluetoothHelper.CheckBluetooth(Activity))
            {
                BluetoothHelper.RequestBluetooth(Activity);
            }
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            Activity?.Bound = false;
            DetachBinderHandlers(Activity);
            Binder = null;
        }

        public void AttachBinderHandlers(MainActivity? MainActivity)
        {
            if (MainActivity == null || Binder == null) return;

            Binder?.OnDataReceived -= MainActivity.Binder_OnDataReceived;
            Binder?.OnDataUploaded -= MainActivity.Binder_OnDataUploaded;

            Binder?.OnDataReceived += MainActivity.Binder_OnDataReceived;
            Binder?.OnDataUploaded += MainActivity.Binder_OnDataUploaded;
        }

        public void DetachBinderHandlers(MainActivity? MainActivity)
        {
            if (MainActivity == null || Binder == null) return;

            Binder?.OnDataReceived -= MainActivity.Binder_OnDataReceived;
            Binder?.OnDataUploaded -= MainActivity.Binder_OnDataUploaded;
        }
    }
}