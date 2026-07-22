using Java.Util;
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
    //  Code loosely based on:
    //      https://acaliaro.wordpress.com/2017/02/07/connect-a-barcode-reader-to-a-xamarin-forms-app-via-bluetooth/

    public class BluetoothListener(MainService Service, ServiceUpdater ServiceUpdater)
    {
        static readonly string TAG = typeof(BluetoothListener).FullName ?? "BluetoothListener";

        public bool IsStarted { get; private set; } = false;

        private CancellationTokenSource? CancellationTokenSource;

        private readonly MainService Service = Service;                            // service from which listener instantiated

        readonly ServiceUpdater serviceUpdater = ServiceUpdater;

        private string? BluetoothDevice = null;
        private Task? ListenerTask = null;

        public void Start()
        {
            Log.Info(TAG, "Starting bluetooth listener");
            IsStarted = true;

            if (!BluetoothHelper.CheckBluetooth(Service)) return;               // return if bluetooth not enabled (should have been enabled in main activity

            var Resources = Service.Resources;
            var preferences = PreferenceManager.GetDefaultSharedPreferences(Service);

            if (Resources is null || preferences is null) return;

            BluetoothDevice = preferences.GetString(Resources.GetString(Resource.String.preference_key_bluetooth_device), "");
            if (string.IsNullOrWhiteSpace(BluetoothDevice)) return;			// return if no device specificied

            CancellationTokenSource = new CancellationTokenSource();

            if (ListenerTask != null && !ListenerTask.IsCompleted) ListenerTask.Wait();

            ListenerTask = Task.Run(async () => await RequestBluetoothDataAsync(CancellationTokenSource), CancellationTokenSource.Token);
        }

        public void Stop()
        {
            Log.Info(TAG, "Stopping bluetooth listener");
            IsStarted = false;

            CancellationTokenSource?.Cancel();
        }

        private async Task RequestBluetoothDataAsync(CancellationTokenSource cts)
        {
            BluetoothSocket? socket = null;
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    BroadcastBluetoothStatus(false);
                    // open a bluetooth socket
                    while (socket == null && !cts.IsCancellationRequested)
                    {
                        socket = await GetBluetoothSocketAsync();
                        if (socket == null && !cts.IsCancellationRequested)
                        {
                            Log.Info(TAG, "Could not open bluetooth socket, delaying before trying again.");
                            await Task.Delay(Constants.BLUETOOTH_RETRY_DELAY);
                        }
                    }
                    BroadcastBluetoothStatus(socket != null);
                    // get here when socket is open
                    // now read data from the socket
                    var eChookBuffer = new EChookBuffer();
                    while (socket != null && !cts.IsCancellationRequested)
                    {
                        var echookData = await ReadEChookDataAsync(socket, cts, eChookBuffer);
                        if (echookData == null)       // exception reading bluetooth socket
                        {
                            CloseBluetoothSocket(socket);
                            socket = null;
                            break;
                        }
                        if (!echookData.IsEmpty)
                        {
                            Intent intent = new(Constants.BLUETOOTH_DATA_BROADCAST_ACTION);
                            intent.PutExtra("data", echookData);
                            serviceUpdater.Post(intent);
                        }
                    }
                    BroadcastBluetoothStatus(socket != null);
                }
                BroadcastBluetoothStatus(false);
            }
            catch (System.Exception ex)
            {
                Log.Info(TAG, "Request bluetooth data exception: " + ex.Message ?? "");
            }
        }

        private void BroadcastBluetoothStatus(bool isConnected)
        {
            Intent intent = new(Constants.BLUETOOTH_STATUS_BROADCAST_ACTION);
            intent.PutExtra("isconnected", isConnected ? 1 : 0);
            serviceUpdater.Post(intent);
        }

        private async Task<BluetoothSocket?> GetBluetoothSocketAsync()
        {
            Log.Info(TAG, "Finding bluetooth device and opening socket for: " + BluetoothDevice);

            var bluetoothManager = Service.GetSystemService(Context.BluetoothService) as BluetoothManager;
            BluetoothAdapter? bluetoothAdapter = bluetoothManager?.Adapter;
            BluetoothDevice? bluetoothDevice = null;
            BluetoothSocket? bluetoothSocket = null;

            if (bluetoothAdapter == null) return null;                                      // return if there is no bluetooth adapter
            if (!bluetoothAdapter.IsEnabled) return null;                                   // return if the bluetooth adapter is not enabled
            if (bluetoothAdapter.BondedDevices is null) return null;                        // return if no bonded devices
            foreach (var bondedDevice in bluetoothAdapter.BondedDevices)
            {
                if (bondedDevice.Name is not null && BluetoothDevice is not null && bondedDevice.Name.ToUpper().Contains(BluetoothDevice.ToUpper(), StringComparison.CurrentCulture))
                {
                    bluetoothDevice = bondedDevice;
                    break;
                }
            }
            if (bluetoothDevice == null) return null;                                       // return if named device not found

            try
            {
                UUID? uuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");
                if (uuid is not null)
                    bluetoothSocket = bluetoothDevice.CreateInsecureRfcommSocketToServiceRecord(uuid);
                if (bluetoothSocket == null) return null;                                   // return if unable to create socket

                await bluetoothSocket.ConnectAsync();

                if (!bluetoothSocket.IsConnected) return null;                              // return if unable to connect 
            }
            catch (System.Exception ex)
            {
                Log.Info(TAG, "Exception whilst finding bluetooth device and opening socket: " + ex.Message ?? "");
                return null;
            }

            return bluetoothSocket;
        }

        private static void CloseBluetoothSocket(BluetoothSocket socket)
        {
            if (socket != null)
            {
                if (socket.IsConnected)
                {
                    socket.InputStream?.Close();
                    socket.Close();
                }
            }
        }

        private static async Task<Bundle?> ReadEChookDataAsync(BluetoothSocket socket, CancellationTokenSource cts, EChookBuffer eChookBuffer)
        {
            var readData = new Bundle();

            if (socket == null || !socket.IsConnected) return null;

            cts.Token.Register(() => CloseBluetoothSocket(socket));   // force socket input stream to close on cancellation.

            try
            {
                int receivedCount;

                if (socket.InputStream is null) return null;

                receivedCount = await socket.InputStream.ReadAsync(eChookBuffer.ReceivedBytes, eChookBuffer.Offset, 
                                                                   eChookBuffer.ReceivedBytes.Length - eChookBuffer.Offset, cts.Token);

                eChookBuffer.BytesLeft = eChookBuffer.Offset + receivedCount;

                var done = cts.IsCancellationRequested;
                while (!done)
                {
                    int ix = 0;
                    while ((eChookBuffer.ReceivedBytes[ix] != '{') && (ix < eChookBuffer.BytesLeft)) ix++;
                    if (ix >= eChookBuffer.BytesLeft)
                    {
                        eChookBuffer.Offset = eChookBuffer.BytesLeft;
                        done = true;
                    }
                    else
                    {
                        if (ix > 0)
                        {
                            eChookBuffer.BytesLeft -= ix;
                            Array.Copy(eChookBuffer.ReceivedBytes, ix, eChookBuffer.ReceivedBytes, 0, eChookBuffer.BytesLeft);
                            ix = 0;
                        }
                        while ((eChookBuffer.ReceivedBytes[ix] != '}') && (ix < eChookBuffer.BytesLeft)) ix++;
                        if (ix >= eChookBuffer.BytesLeft)
                        {
                            eChookBuffer.Offset = eChookBuffer.BytesLeft;
                            done = true;
                        }
                        else
                        {
                            if (ix == 4)
                            {
                                if (eChookBuffer.TryParseEChookData(eChookBuffer.ReceivedBytes[2], eChookBuffer.ReceivedBytes[3], out double data))
                                {
                                    string key = Convert.ToChar(eChookBuffer.ReceivedBytes[1]).ToString();
                                    readData.PutDouble(key, data);
                                }
                            }
                            eChookBuffer.BytesLeft -= ix;
                            Array.Copy(eChookBuffer.ReceivedBytes, ix, eChookBuffer.ReceivedBytes, 0, eChookBuffer.BytesLeft);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Info(TAG, "Exception reading from bluetooth socket. Possibly lost connection. " + (ex.Message ?? ""));
                return null;
            }

            return readData;
        }

        private class EChookBuffer
        {
            public byte[] ReceivedBytes { get; set; }
            public int Offset { get; set; }
            public int BytesLeft { get; set; }

            public EChookBuffer()
            {
                ReceivedBytes = new byte[100];
                Offset = 0;
                BytesLeft = 0;
            }
            public bool TryParseEChookData(byte b1, byte b2, out double value)
            {
                value = 0.0F;
                try
                {
                    if (b1 == 0xFF) b1 = 0;
                    if (b2 == 0xFF) b2 = 0;
                    if (b1 < 0x80)                  // float value
                    {
                        value = (double)b1 + ((double)b2 / 100.0F);
                    }
                    else
                    {                               // int value
                        b1 &= 0x7F;
                        value = ((double)b1 * 100.0F) + (double)b2;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Info(TAG, "Exception: " + ex.Message);
                    return false;
                }
                return true;
            }
        }


    }

}