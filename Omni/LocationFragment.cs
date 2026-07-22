using Android.Gms.Maps;
using Android.Gms.Maps.Model;
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
    // References: 
    //  https://docs.microsoft.com/en-us/xamarin/android/platform/maps-and-location/maps/maps-api
    //  https://docs.microsoft.com/en-us/xamarin/android/platform/maps-and-location/maps/obtaining-a-google-maps-api-key?tabs=windows

    public class LocationFragment : Fragment, IFragmentServiceUpdates, IOnMapReadyCallback
    {
        static readonly string TAG = typeof(LocationFragment).FullName ?? "LocationFragment";

        GoogleMap? map;
        Marker? marker;
        Circle? circle = null;
        readonly float initialZoom = 14;

        ImageButton? mapTypeButton;

        public override void OnCreate(Bundle? savedInstanceState)
        {
            Log.Info(TAG, "OnCreate.");
            base.OnCreate(savedInstanceState);
        }

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            Log.Info(TAG, "OnCreateView.");

            var view = inflater.Inflate(Resource.Layout.location_fragment, container, false);

            mapTypeButton = view?.FindViewById<ImageButton>(Resource.Id.maptype);
            mapTypeButton?.Click += MapTypeButton_Click;

            //var mapFragment = (SupportMapFragment)ChildFragmentManager.FindFragmentById(Resource.Id.map);
            //mapFragment.GetMapAsync(this);

            // see: https://github.com/xamarin/AndroidX/issues/52
            var mapFragment1 = ChildFragmentManager.FindFragmentById(Resource.Id.map).JavaCast<SupportMapFragment>();
            mapFragment1?.GetMapAsync(this);

            return view;
        }


        public void OnMapReady(GoogleMap map)
        {
            this.map = map;
            this.map.MapType = GoogleMap.MapTypeHybrid;
            map.UiSettings.ZoomGesturesEnabled = true;
            map.UiSettings.ZoomControlsEnabled = true;

            LatLng latLng = new(57.053372, -2.491336);     // location of Banchory Academy
            map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(latLng, initialZoom));

            MarkerOptions opt = new();
            opt.SetPosition(latLng);
            marker = map.AddMarker(opt);
        }

        public void OnDataReceived(List<string> Keys)
        {
            if (Keys.Contains(Constants.SHARED_DATA_LOCATION_KEY))
            {
                var location = LocationHelper.GetLocationFromBundle(Shared.Data, Constants.SHARED_DATA_LOCATION_KEY);
                if (location != null && location.Latitude != 0 && location.Longitude != 0 && map != null)
                {
                    LatLng latLng = new(location.Latitude, location.Longitude);
                    map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(latLng, map.CameraPosition.Zoom));
                    DrawAccuracyCircle(location);
                }
            }
        }

        private void DrawAccuracyCircle(Location location)
        {
            try
            {
                LatLng latLng = new(location.Latitude, location.Longitude);
                marker?.Position = latLng;

                if (location.HasAccuracy && location.Accuracy > 0)
                {
                    circle?.Remove();
                    CircleOptions opt = new();
                    opt.InvokeCenter(latLng);
                    opt.InvokeRadius(location.Accuracy);
                    opt.InvokeStrokeWidth(4);
                    if (Activity != null)
                    {
                        opt.InvokeStrokeColor(Activity.GetColor(Resource.Color.location_accuracy_solid));
                        opt.InvokeFillColor(Activity.GetColor(Resource.Color.location_accuracy_fill));
                        circle = map?.AddCircle(opt);
                    }
                }
            }
            catch (System.Exception) { }
        }

        private void MapTypeButton_Click(object? sender, EventArgs? e)
        {
            switch(map?.MapType)
            {
                case GoogleMap.MapTypeHybrid:
                    map.MapType = GoogleMap.MapTypeNormal;
                    break;
                case GoogleMap.MapTypeNormal:
                    map.MapType = GoogleMap.MapTypeSatellite;
                    break;
                case GoogleMap.MapTypeSatellite:
                    map.MapType = GoogleMap.MapTypeTerrain;
                    break;
                case GoogleMap.MapTypeTerrain:
                    map.MapType = GoogleMap.MapTypeHybrid;
                    break;
            }
        }

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new NotImplementedException();
        }
    }

}