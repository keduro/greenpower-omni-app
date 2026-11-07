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
    public class SimulationProvider
    {
        static readonly string TAG = typeof(SimulationProvider).FullName ?? "SimulationProvider";

        const string csvFileName = "SimulationData.csv";        // name of data containing simulation data in Assets

        readonly MainService Service;                           // service from which sender instantiated
        readonly Timer timer;                                   // timer on which simulation data provider runs

        readonly ServiceUpdater serviceUpdater;

        bool isStarted = false;                                 // keep track of whether started or not
        readonly List<double[]> data = [];                      // the data!
        int dataIndex;                                          // index of the last row of data provided
        Location? previousLocation = null;                      // previous location - only send updated locations
        Location? addressLocation = null;                       // last location at which address was looked up

        readonly string[] keys =
        [
            Constants.SHARED_DATA_INPUT_THROTTLE_KEY,   Constants.SHARED_DATA_ACTUAL_THROTTLE_KEY,  Constants.SHARED_DATA_VTOTAL_KEY, 
            Constants.SHARED_DATA_VLOWER_KEY,           Constants.SHARED_DATA_CURRENT_KEY,          Constants.SHARED_DATA_RPM_KEY,
            Constants.SHARED_DATA_SPEED_KEY,            Constants.SHARED_DATA_TEMPERATURE_1_KEY,    Constants.SHARED_DATA_TEMPERATURE_2_KEY,
            Constants.SHARED_DATA_GEAR_RATIO_KEY,       Constants.SHARED_DATA_BRAKE_PRESSED_KEY
        ];

        public SimulationProvider(MainService Service, ServiceUpdater ServiceUpdater)
        {
            this.Service = Service;
            timer = new Timer { Enabled = false };
            timer.Elapsed += Timer_Elapsed;

            serviceUpdater = ServiceUpdater;
        }

        public void Start()
        {
            Log.Info(TAG, "Starting simulation provider timer");

            if (isStarted) return;

            data.Clear();
            previousLocation = null;
            addressLocation = null;

            // read the data in from the csv file

            if (Service.Assets != null)
            {
                using StreamReader reader = new(Service.Assets.Open(csvFileName));
                var firstLine = true;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!firstLine && line != null)
                    {
                        var stringColumns = line.Split(',');
                        var doubleColumns = new double[stringColumns.Length];
                        for (int i = 0; i < stringColumns.Length; i++)
                        {
                            _ = double.TryParse(stringColumns[i], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleColumns[i]);
                        }
                        data.Add(doubleColumns);
                    }
                    firstLine = false;
                }
            }
            dataIndex = data.Count - 1;

            // set the timer off 

            timer.Interval = 250.0;
            timer.Start();

            isStarted = true;

            // set the lapcounter state 

            Service.SetState(State.Running);
        }

        public void Stop()
        {
            Log.Info(TAG, "Stopping simulation provider timer");
            timer.Stop();
            isStarted = false;
        }

        private async void Timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            dataIndex++;
            if (dataIndex >= data.Count)
                dataIndex = 0;

            // data for all values received from the eChook

            var readData = new Bundle();

            for (int i = 0; i < keys.Length; i++)
                readData.PutDouble(keys[i], data[dataIndex][i]);

            Intent intent = new(Constants.BLUETOOTH_DATA_BROADCAST_ACTION);
            intent.PutExtra("data", readData);
            serviceUpdater.Post(intent);

            // latitude and longitude -> location and address

            var location = new Location("")
            {
                Latitude = data[dataIndex][keys.Length + 0],
                Longitude = data[dataIndex][keys.Length + 1],
                Accuracy = 10F
            };

            if ((previousLocation != null) && (location.Latitude == previousLocation.Latitude) && (location.Longitude == previousLocation.Longitude)) return;
            previousLocation = location;

            // broadcast location to the main service

            Intent locationIntent = new(Constants.LOCATION_DATA_BROADCAST_ACTION);
            locationIntent.PutExtra("location", location);
            serviceUpdater.Post(locationIntent);

            bool doGeocode;
            if (addressLocation == null)
            {
                doGeocode = true;
            }
            else
            {
                float[] distanceResults = new float[3];
                Android.Locations.Location.DistanceBetween(addressLocation.Latitude, addressLocation.Longitude,
                                                           location.Latitude, location.Longitude, distanceResults);
                doGeocode = (distanceResults[0] > Constants.REDO_GEOCODE_DISTANCE);
            }
            if (doGeocode)
            {
                try
                {
                    var geocoder = new Geocoder(Service);
                    var addresses = await geocoder.GetFromLocationAsync(location.Latitude, location.Longitude, 1);
                    if (addresses != null && addresses.Count > 0)
                    {
                        var address = addresses[0];
                        if (address != null)
                        {
                            // broadcast address to the main service
                            Intent addressIntent = new(Constants.LOCATION_DATA_BROADCAST_ACTION);
                            addressIntent.PutExtra("address", address.GetAddressLine(0));
                            serviceUpdater.Post(addressIntent);

                            addressLocation = location;
                        }
                    }
                }
                catch (System.Exception)
                {
                    Log.Info(TAG, "SimulationProvider: geocoder exception");
                }
            }
        }


    }
}