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
        public MainActivity? Activity { get; set; }

        public ServiceBinder? Binder { get; set; }

        public void OnServiceConnected(ComponentName? name, IBinder? service)
        {
            ServiceBinder? binder = (ServiceBinder?)service;
            this.Binder = binder;
            if (Activity != null)
            {
                binder?.OnDataReceived += Activity.Binder_OnDataReceived;
                binder?.OnDataUploaded += Activity.Binder_OnDataUploaded;
                Activity.Bound = true;
                if (!BluetoothHelper.CheckBluetooth(Activity))
                {
                    BluetoothHelper.RequestBluetooth(Activity);
                }
                else
                {
                    try
                    {
                        Binder?.Service?.IsBound = true;
                        Binder?.Service?.Start();
                    }
                    catch (DeadObjectException) { }     // ignore lost connection to service!
                }
            }
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            Activity?.Bound = false;
        }
    }
}