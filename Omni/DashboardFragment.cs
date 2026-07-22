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
    public class DashboardFragment : Fragment, IFragmentServiceUpdates, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        static readonly string TAG = typeof(DashboardFragment).FullName ?? "DashboardFragment";

        ImageView? bluetoothIndicatorImageView = null;
        ImageView? uploadIndicatorImageView = null;

        P[]? allViews;                               // details on all text views that may appear in the fragment
        Dictionary<int, P>? fragmentViews;           // details for just those text views that do appear in the fragment
        Dictionary<string, P>? fragmentData;         // details by source of data for those text views that do appear in the fragment

        Timer? timeOfDayTimer;
        WatchdogIndicator? bluetoothWatchdog;
        WatchdogIndicator? uploadWatchdog;

        State previousState;

        public override void OnCreate(Bundle? savedInstanceState)
        {
            Log.Info(TAG, "OnCreate.");
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            Log.Info(TAG, "OnCreateView.");

            int layoutResourceId = Resource.Layout.dashboard0_fragment;
            _ = int.TryParse(this.Tag, out layoutResourceId);                   // get layout from the fragment tag, set in mainactivity.cs

            View? view = inflater.Inflate(layoutResourceId, container, false) ?? throw new Exception("Exception inflating dashboard fragment view");

            allViews = [
                new P(view, Resource.Id.speedtextview,            
                            Resource.Id.speedunittextview,           typeof(double), Constants.SHARED_DATA_SPEED_KEY,               1, "0",   null, 
                                                                                                                                              [ new Poption(Resource.String.preference_option_speed_mph,      Resource.String.dashboard_unit_speed_mph,      Constants.SHARED_DATA_SPEED_KEY,    Constants.MPS2MPH,  "0"),
                                                                                                                                                new Poption(Resource.String.preference_option_speed_m_s,      Resource.String.dashboard_unit_speed_m_s,      Constants.SHARED_DATA_SPEED_KEY,    1,                  "0"),
                                                                                                                                                new Poption(Resource.String.preference_option_speed_km_h,     Resource.String.dashboard_unit_speed_km_h,     Constants.SHARED_DATA_SPEED_KEY,    Constants.MPS2KMPH, "0")]),
                new P(view, Resource.Id.distancetextview,         
                            Resource.Id.distanceunittextview,        typeof(double), Constants.SHARED_DATA_DISTANCE_KEY,            1, "0.0", (() => ((MainActivity?)Activity)?.MainServiceConnection?.Binder?.Service?.ResetDistance()),
                                                                                                                                              [ new Poption(Resource.String.preference_option_distance_km,    Resource.String.dashboard_unit_distance_km,    Constants.SHARED_DATA_DISTANCE_KEY, 0.001,              "0.0"),
                                                                                                                                                new Poption(Resource.String.preference_option_distance_miles, Resource.String.dashboard_unit_distance_miles, Constants.SHARED_DATA_DISTANCE_KEY, Constants.M2MILES,  "0.0")]),
                new P(view, Resource.Id.laptextview,              0, typeof(int),    Constants.SHARED_DATA_LAP_KEY,                 1, "0",   (() => ((MainActivity?)Activity)?.MainServiceConnection?.Binder?.Service?.ResetLap()),
                                                                                                                                              null),
                new P(view, Resource.Id.currenttextview,          0, typeof(double), Constants.SHARED_DATA_CURRENT_KEY,             1, "0.0", null, null),
                new P(view, Resource.Id.energytextview,           
                            Resource.Id.energyunittextview,          typeof(double), Constants.SHARED_DATA_AS_KEY,                  1, "0.0", (() => ((MainActivity?)Activity)?.MainServiceConnection?.Binder?.Service?.ResetEnergy()),
                                                                                                                                              [ new Poption(Resource.String.preference_option_energy_ah,     Resource.String.dashboard_unit_energy_ah,       Constants.SHARED_DATA_AS_KEY, Constants.AS2AH, "0.0"),
                                                                                                                                                new Poption(Resource.String.preference_option_energy_wh,     Resource.String.dashboard_unit_energy_wh,       Constants.SHARED_DATA_J_KEY,  Constants.J2WH,  "0"),
                                                                                                                                                new Poption(Resource.String.preference_option_energy_kj,     Resource.String.dashboard_unit_energy_kj,       Constants.SHARED_DATA_J_KEY,  0.001,           "0")]),
                new P(view, Resource.Id.vtotaltextview,           0, typeof(double), Constants.SHARED_DATA_VTOTAL_KEY,              1, "0.0", null, null),
                new P(view, Resource.Id.vlowertextview,           0, typeof(double), Constants.SHARED_DATA_VLOWER_KEY,              1, "0.0", null, null),
                new P(view, Resource.Id.vuppertextview,           0, typeof(double), Constants.SHARED_DATA_VUPPER_KEY,              1, "0.0", null, null),
                new P(view, Resource.Id.geartextview,             0, typeof(double), Constants.SHARED_DATA_GEAR_RATIO_KEY,          1, "0.0", null, null),
                new P(view, Resource.Id.rpmtextview,              0, typeof(double), Constants.SHARED_DATA_RPM_KEY,                 1, "0",   null, null),
                new P(view, Resource.Id.inputthrottletextview,    0, typeof(double), Constants.SHARED_DATA_INPUT_THROTTLE_KEY,      1, "0",   null, null),
                new P(view, Resource.Id.actualthrottletextview,   0, typeof(double), Constants.SHARED_DATA_ACTUAL_THROTTLE_KEY,     1, "0",   null, null),
                new P(view, Resource.Id.brakepressedtextview,     0, typeof(int),    Constants.SHARED_DATA_BRAKE_PRESSED_KEY,       1, "0",   null, null),
                new P(view, Resource.Id.temperature1textview,     0, typeof(double), Constants.SHARED_DATA_TEMPERATURE_1_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.temperature2textview,     0, typeof(double), Constants.SHARED_DATA_TEMPERATURE_2_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.temperature3textview,     0, typeof(double), Constants.SHARED_DATA_TEMPERATURE_3_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.launchbuttontextview,     0, typeof(int),    Constants.SHARED_DATA_LAUNCH_BUTTON_KEY,       1, "0",   null, null),
                new P(view, Resource.Id.cycleviewbuttontextview,  0, typeof(int),    Constants.SHARED_DATA_CYCLE_VIEW_BUTTON_KEY,   1, "0",   null, null),
                new P(view, Resource.Id.gearratiotextview,        0, typeof(double), Constants.SHARED_DATA_GEAR_RATIO_KEY,          1, "0.0", null, null),
                new P(view, Resource.Id.customdata0textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_0_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata1textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_1_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata2textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_2_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata3textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_3_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata4textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_4_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata5textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_5_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata6textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_6_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata7textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_7_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata8textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_8_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdata9textview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_9_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdatawtextview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_W_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdataxtextview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_X_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdataytextview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_Y_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.customdataztextview,      0, typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_Z_KEY,       1, "0.0", null, null),
                new P(view, Resource.Id.ahtextview,               0, typeof(double), Constants.SHARED_DATA_AS_KEY,                  Constants.AS2AH,        "0.0", null, null),   // convert from ampseconds to display as Ah
                new P(view, Resource.Id.kjtextview,               0, typeof(double), Constants.SHARED_DATA_J_KEY,                   0.001,                  "0",   null, null),   // convert from joules to display as Wh
                new P(view, Resource.Id.efficiencyatextview,      0, typeof(double), Constants.SHARED_DATA_EFFICIENCY_ASPM_KEY,     Constants.ASPM2AHPMILE, "0.0", null, null),   // convert from ampseconds per meter to display as amphours per mile
                new P(view, Resource.Id.efficiencyjtextview,      0, typeof(double), Constants.SHARED_DATA_EFFICIENCY_JPM_KEY,      Constants.JPM2KJPMILE,  "0.0", null, null),   // convert from joules per meter to watthours per mile
                new P(view, Resource.Id.elapsedtimetextview2,     0, typeof(int),    Constants.SHARED_DATA_ELAPSED_TIME_KEY,        1, "0",   null, null),
                new P(view, Resource.Id.statetextview,            0, typeof(int),    Constants.SHARED_DATA_STATE_KEY,               1, "0",   null, null),
                new P(view, Resource.Id.locationaccuracytextview, 0, null,           Constants.SHARED_DATA_LOCATION_KEY,            1, "",    null, null ),
                new P(view, Resource.Id.addresstextview,          0, typeof(string), Constants.SHARED_DATA_ADDRESS_KEY,             1, "",    null, null ),
                new P(view, Resource.Id.timeofdaytextview,        0, null,           null,                                          1, "",    null, null ),
                new P(view, Resource.Id.elapsedtimetextview,      0, null,           null,                                          1, "",    (() => ((MainActivity?)Activity)?.MainServiceConnection?.Binder?.Service?.ResetElapsedTime()), null )
            ];

            fragmentViews = [];
            fragmentData = [];

            foreach (var p in allViews)
            {
                if (p != null && p.View != null)
                {
                    fragmentViews.Add(p.ResourceId, p);
                    try
                    {
                        if (p.SharedDataSourceKey != null) fragmentData.Add(p.SharedDataSourceKey, p);     // try to add, duplicate keys ignored!
                    }
                    catch (System.Exception) { }
                }
            }

            UpdateValuesAndUnits();

            bluetoothIndicatorImageView = view.FindViewById<ImageView>(Resource.Id.bluetoothindicatorimageview);
            uploadIndicatorImageView = view.FindViewById<ImageView>(Resource.Id.uploadindicatorimageview);

            timeOfDayTimer = new Timer(Constants.TIME_OF_DAY_UPDATE_INTERVAL);    // 1s tick
            timeOfDayTimer.Elapsed += TimeOfDayTimer_Elapsed;
            timeOfDayTimer.Enabled = true;

            if (Activity != null && Activity.GetType() == typeof(MainActivity))
            {
                bluetoothWatchdog = new WatchdogIndicator(Constants.BLUETOOTH_WATCHDOG_INTERVAL, (MainActivity)Activity, bluetoothIndicatorImageView,
                                                          Resources.GetIdentifier("round_bluetooth_connected_green_20dp", "drawable", Activity.PackageName),
                                                          Resources.GetIdentifier("round_bluetooth_connected_red_20dp", "drawable", Activity.PackageName));
                uploadWatchdog = new WatchdogIndicator(Constants.UPLOAD_WATCHDOG_INTERVAL, (MainActivity)Activity, uploadIndicatorImageView,
                                                       Resources.GetIdentifier("round_cloud_upload_green_20dp", "drawable", Activity.PackageName),
                                                       Resources.GetIdentifier("round_cloud_upload_red_20dp", "drawable", Activity.PackageName));
            }

            previousState = State.Running;      // initialise to running to trigger 'waiting for launch' snackbar when current state is retrieved for first time

            return view;
        }

        public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
        {
            UpdateValuesAndUnits();
        }

        private void TimeOfDayTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
        {
            string? currentTime = null;
            string? elapsedTime = null;
            DateTime now = DateTime.Now;
            var currentState = (State)Shared.Data.GetInt(Constants.SHARED_DATA_STATE_KEY, 0);

            // update current time

            if (fragmentViews != null && fragmentViews.ContainsKey(Resource.Id.timeofdaytextview))
            {
                currentTime = now.ToString("HH:mm:ss");
            }

            // update elapsed (race) time

            if (fragmentViews != null && fragmentViews.ContainsKey(Resource.Id.elapsedtimetextview))
            {
                elapsedTime = "00:00:00";
                if (currentState == State.Running)
                {
                    int t = Shared.Data.GetInt(Constants.SHARED_DATA_ELAPSED_TIME_KEY, 0);
                    var ts = new TimeSpan(0, 0, t);
                    elapsedTime = ts.ToString(@"hh\:mm\:ss");
                }
            }

            // make the actual text view updates

            if ((currentTime != null) || (elapsedTime != null))
            {
                if (Activity != null && fragmentViews != null)
                {
                    Activity.RunOnUiThread(() =>
                    {
                        if (currentTime != string.Empty && fragmentViews.TryGetValue(Resource.Id.timeofdaytextview, out P? value1))
                            value1.View?.Text = currentTime;
                        if (elapsedTime != string.Empty && fragmentViews.TryGetValue(Resource.Id.elapsedtimetextview, out P? value2))
                            value2.View?.Text = elapsedTime;
                    });
                }
            }

            // check for state transitions and communicate via snackbar

            if (currentState != previousState)
            {
                previousState = currentState;

                if (Activity != null)
                {
                    var activityMainView = Activity.FindViewById(Resource.Id.activity_main);
                    if (activityMainView != null)
                    {
                        switch (currentState)
                        {
                            case State.WaitingForThrottleOrCurrent:
                                Snackbar.Make(activityMainView,
                                              Resources.GetString(Resource.String.snackbar_waiting_for)
                                                + Convert.ToInt32(Constants.LAUNCH_THROTTLE).ToString()
                                                + Resources.GetString(Resource.String.snackbar_percent_throttle)
                                                + Convert.ToInt32(Constants.LAUNCH_CURRENT).ToString()
                                                + Resources.GetString(Resource.String.snackbar_current),
                                              Snackbar.LengthIndefinite)
                                        .SetAction(Resources.GetString(Resource.String.snackbar_dismiss), (o) => { })
                                        .Show();
                                break;
                            case State.Running:
                                Snackbar.Make(activityMainView,
                                              Resources.GetString(Resource.String.snackbar_lap_counting_started),
                                              Snackbar.LengthShort).Show();
                                break;
                        }
                    }
                }

            }
        }

        public void OnDataReceived(List<string> Keys)
        {
            foreach (var key in Keys)
            {
                if (fragmentData == null || !fragmentData.ContainsKey(key))
                    continue;
                UpdateTextView(fragmentData[key]);
            }
            if (Keys.Contains(Constants.SHARED_DATA_CURRENT_KEY)) bluetoothWatchdog?.Kick();
        }

        public void OnDataUploaded()
        {
            uploadWatchdog?.Kick();
        }

        private void UpdateTextView(P p)
        {
            if (p.Type==typeof(double))
            {
                double value = 0;
                if (Shared.Data.ContainsKey(p.SharedDataSourceKey)) value = Shared.Data.GetDouble(p.SharedDataSourceKey);
                value *= p.Multiplier;
                p.View?.Text = value.ToString(p.Format);
            }
            else if (p.Type==typeof(int))
            {
                int value = 0;
                if (Shared.Data.ContainsKey(p.SharedDataSourceKey)) value = Shared.Data.GetInt(p.SharedDataSourceKey);
                value *= Convert.ToInt32(p.Multiplier);
                p.View?.Text = value.ToString(p.Format);
            }
            else if (p.Type==typeof(string))
            {
                string? value = string.Empty;
                if (Shared.Data.ContainsKey(p.SharedDataSourceKey)) value = Shared.Data.GetString(p.SharedDataSourceKey);
                p.View?.Text = value;
            }
            else if (p.Type==null)
            {
                switch (p.ResourceId)
                {
                    case Resource.Id.locationaccuracytextview:
                        {
                            string value = "±?" + Resources.GetString(Resource.String.dashboard_unit_plusminus_m);
                            if (Shared.Data.ContainsKey(p.SharedDataSourceKey))
                            {
                                var location = LocationHelper.GetLocationFromBundle(Shared.Data, Constants.SHARED_DATA_LOCATION_KEY);
                                var accuracy = location?.Accuracy;
                                if (accuracy != null && accuracy > 0)
                                    value = "±" + ((float)accuracy).ToString("0") + Resources.GetString(Resource.String.dashboard_unit_plusminus_m);
                            }
                            p.View?.Text = value;
                        }
                        break;
                }
            }
        }

        private void UpdateValuesAndUnits()
        {
            if (Activity == null) return;
            if (fragmentViews == null) return;
            if (fragmentData == null) return;

            // get unit preferences

            var preferences = PreferenceManager.GetDefaultSharedPreferences(Activity);
            if (preferences == null) return; 

            string? speedUnits = preferences.GetString(Resources.GetString(Resource.String.preference_key_speed), Resources.GetString(Resource.String.preference_option_speed_mph));
            string? distanceUnits = preferences.GetString(Resources.GetString(Resource.String.preference_key_distance), Resources.GetString(Resource.String.preference_option_distance_miles));
            string? energyUnits = preferences.GetString(Resources.GetString(Resource.String.preference_key_energy), Resources.GetString(Resource.String.preference_option_energy_ah));
            // string efficiencyUnits = preferences.GetString(Resources.GetString(Resource.String.preference_key_efficiency), Resources.GetString(Resource.String.preference_option_efficiency_kj_mile));  // efficiency and units not shown on any fragment at present

            // table of textviews which have unit dependent displays

            var table = new (int textviewResourceId, string? units) [] {
                ( Resource.Id.speedtextview,    speedUnits),
                ( Resource.Id.distancetextview, distanceUnits),
                ( Resource.Id.energytextview,   energyUnits )
            };

            foreach (var (textviewResourceId, units) in table)
            {
                if (fragmentViews.TryGetValue(textviewResourceId, out P? p))
                {
                    if (p.Options != null)
                    {
                        foreach (var option in p.Options)
                        {
                            if (Resources.GetString(option.OptionStringId).Equals(units))
                            {
                                // update associated unit view
                                p.UnitView?.Text = Resources.GetString(option.UnitStringId);
                                // update shared data source key, multiplier, and format
                                if (p.SharedDataSourceKey != null && !p.SharedDataSourceKey.Equals(option.SharedDataSourceKey))
                                {
                                    if (fragmentData.ContainsKey(p.SharedDataSourceKey))
                                    {
                                        fragmentData.Remove(p.SharedDataSourceKey);
                                        fragmentData.Add(option.SharedDataSourceKey, p);
                                    }
                                    p.SharedDataSourceKey = option.SharedDataSourceKey;
                                }
                                p.Multiplier = option.Multiplier;
                                p.Format = option.Format;
                                // update what's displayed
                                UpdateTextView(p);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new NotImplementedException();
        }
    }

    class P
    {
        public int ResourceId { get; }
        public int UnitResourceId { get; }
        public Type? Type { get; }
        public string? SharedDataSourceKey { get; set; }
        public double Multiplier { get; set; }
        public string Format { get; set; }
        public Poption[]? Options { get; }
        public TextView? View { get; }
        public TextView? UnitView { get;  }

        public P(View ParentView, int ResourceId, int UnitResourceId, Type? Type, string? SharedDataSourceKey, double Multiplier, string Format, Action? LongClickAction, Poption[]? Options)
        {
            this.ResourceId = ResourceId;
            this.Type = Type;
            this.SharedDataSourceKey = SharedDataSourceKey;
            this.Multiplier = Multiplier;
            this.Format = Format;
            this.Options = Options;

            View = ParentView.FindViewById<TextView>(ResourceId);
            UnitView = null;
            if (UnitResourceId > 0)
                UnitView = ParentView.FindViewById<TextView>(UnitResourceId);

            if (View != null && LongClickAction != null)
                View.LongClick += ((o, e) =>
                {
                    try
                    {
                        LongClickAction();
                    }
                    catch (DeadObjectException) { }     // Ignore lost connection to service
                });
        }
    }

    class Poption(int OptionResourceId, int UnitResourceId, string SharedDataSourceKey, double Multiplier, string Format)
    {
        public int OptionStringId { get; } = OptionResourceId;
        public int UnitStringId { get; } = UnitResourceId;
        public string SharedDataSourceKey { get; } = SharedDataSourceKey;
        public double Multiplier { get; } = Multiplier;
        public string Format { get; } = Format;
    }

}