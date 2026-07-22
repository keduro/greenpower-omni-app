using Java.Lang;
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
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter([Constants.BLUETOOTH_DATA_BROADCAST_ACTION, Constants.BLUETOOTH_STATUS_BROADCAST_ACTION, Constants.LOCATION_DATA_BROADCAST_ACTION, Constants.UPLOAD_DATA_BROADCAST_ACTION])]
    public class MainServiceBroadcastReceiver : BroadcastReceiver
    {
        public MainService? Service { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "<Pending>")]
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (Service == null || intent == null || intent.Action == null) return;

            if (intent.Action.Equals(Constants.BLUETOOTH_DATA_BROADCAST_ACTION))
            {
                var echookData = intent.GetBundleExtra("data");
                var echookDataKeySet = echookData?.KeySet();
                if (echookData != null && echookDataKeySet != null)
                {
                    foreach (var key in echookDataKeySet)
                    {
                        if (key.IndexOfAny(['L', 'C', 'B']) >= 0)
                            Shared.Data.PutInt(key, Convert.ToInt32(echookData.GetDouble(key)));
                        else
                            Shared.Data.PutDouble(key, echookData.GetDouble(key));
                    }
                    Shared.Data.PutLong(Constants.SHARED_DATA_LATEST_TELEMETRY_KEY, JavaSystem.CurrentTimeMillis());
                    Service.DataAvailable([.. echookDataKeySet]);
                }
            }
            else if (intent.Action.Equals(Constants.BLUETOOTH_STATUS_BROADCAST_ACTION))
            {
                var isConnected = intent.GetIntExtra("isconnected", 0);
                Shared.Data.PutInt(Constants.SHARED_DATA_BLUETOOTH_CONNECTED_KEY, isConnected);
            }
            else if (intent.Action.Equals(Constants.LOCATION_DATA_BROADCAST_ACTION))
            {
                if (intent.HasExtra("location"))
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                        Shared.Data.PutParcelable(Constants.SHARED_DATA_LOCATION_KEY, (Location?)intent.GetParcelableExtra("location", Class.FromType(typeof(Location))));
                    else
                        Shared.Data.PutParcelable(Constants.SHARED_DATA_LOCATION_KEY, (Location?)intent.GetParcelableExtra("location"));
                    Service.DataAvailable([Constants.SHARED_DATA_LOCATION_KEY]);
                }
                if (intent.HasExtra("address"))
                {
                    Shared.Data.PutString(Constants.SHARED_DATA_ADDRESS_KEY, intent.GetStringExtra("address"));
                    Service.ServiceBinder.DataAvailable([Constants.SHARED_DATA_ADDRESS_KEY]);
                }
            }
            else if (intent.Action.Equals(Constants.UPLOAD_DATA_BROADCAST_ACTION))
            {
                Service.ServiceBinder.DataUpload();
            }
        }
    }
}