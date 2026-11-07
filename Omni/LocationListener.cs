using Android.Gms.Location;
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
    public class LocationListener
    {
        static readonly string TAG = typeof(LocationListener).FullName ?? "LocationListener";

        public bool IsStarted { get; private set; } = false;

        readonly Android.Gms.Location.LocationRequest LocationRequest;	// parameters used by FusedLocationProvider
		readonly IFusedLocationProviderClient FusedLocationClient;		// FusedLocationProvider client
		readonly LocationCallback LocationCallback;						// callback for changes in location

		readonly MainService Service;                                   // service from which listener instantiated

		readonly ServiceUpdater serviceUpdater;
		public ServiceUpdater ServiceUpdater { get { return serviceUpdater; } }

        public LocationListener(MainService Service, ServiceUpdater ServiceUpdater)
		{
			this.Service = Service;

            serviceUpdater = ServiceUpdater;

            FusedLocationClient = LocationServices.GetFusedLocationProviderClient(this.Service);

			LocationCallback = new FusedLocationProviderCallback { LocationListener = this };

			var builder = new Android.Gms.Location.LocationRequest.Builder(Priority.PriorityHighAccuracy, Constants.LOCATION_UPDATE_DESIRED_INTERVAL);
			builder.SetWaitForAccurateLocation(true);
			LocationRequest = builder.Build();
        }

        class FusedLocationProviderCallback : LocationCallback
		{
			Location? addressLocation = null;

			public LocationListener? LocationListener { get; set; }

            public override async void OnLocationResult(LocationResult result)
			{
				base.OnLocationResult(result);

				if (result.Locations.Any())
				{
					var location = result.Locations.First();
#if DEBUG
					Log.Info(TAG, "OnLocationResult: new location available: " + location);
#else
					Log.Info(TAG, "OnLocationResult: location updated");
#endif
                    if ((location.Latitude!=0) && (location.Longitude!=0) && LocationListener != null)
					{
						// broadcast location to the main service
						Intent locationIntent = new(Constants.LOCATION_DATA_BROADCAST_ACTION);
						locationIntent.PutExtra("location", location);
                        LocationListener.ServiceUpdater.Post(locationIntent);

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
								var geocoder = new Geocoder(LocationListener.Service);
								var addresses = await geocoder.GetFromLocationAsync(location.Latitude, location.Longitude, 1);
								if (addresses != null && addresses.Count > 0)
								{
									var address = addresses[0];
									if (address != null)
									{
										// broadcast address to the main service
										Intent addressIntent = new(Constants.LOCATION_DATA_BROADCAST_ACTION);
										addressIntent.PutExtra("address", address.GetAddressLine(0));
                                        LocationListener.ServiceUpdater.Post(addressIntent);

                                        addressLocation = location;
									}
								}
							}
							catch (System.Exception)
							{
								Log.Info(TAG, "OnLocationResult: geocoder exception");
							}
						}
					}
					else
					{
						Log.Info(TAG, "OnLocationResult: location had zero lat or lon!!");
					}
				}
				else
				{
					Log.Info(TAG, "OnLocationResult: location not available.");
				}

			}
		}

		public void Start()
		{
			Log.Info(TAG, "Starting location listener, requesting location updates.");
			IsStarted = true;

            _ = FusedLocationClient.RequestLocationUpdates(LocationRequest, LocationCallback, Looper.MyLooper());
		}

		public void Stop()
		{
			Log.Info(TAG, "Stopping location listener, removing location updates.");
			IsStarted= false;

			_ = FusedLocationClient.RemoveLocationUpdates(LocationCallback);
		}

	}
}