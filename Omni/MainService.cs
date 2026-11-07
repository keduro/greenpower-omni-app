using Android.Hardware;
using Java.Lang;
using Java.Util;
using static Android.OS.PowerManager;
using Exception = System.Exception;
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
    public enum State { WaitingForThrottleOrCurrent, Running }

    [Service(Label = "OmniService", Enabled = true, Exported = false,
             ForegroundServiceType = Android.Content.PM.ForegroundService.TypeConnectedDevice | Android.Content.PM.ForegroundService.TypeLocation)]
    public class MainService : Service, ISharedPreferencesOnSharedPreferenceChangeListener, ISensorEventListener
    {
        static readonly string TAG = typeof(MainService).FullName ?? "MainService";

        WakeLock? wakeLock;                                         // use wakelock to prevent service being disabled when screen blanks

        public ServiceBinder ServiceBinder { get; }                 // for binding this main service

        State state;                                                // telemetry state - used for lap counting
        readonly LapCounter lapCounter;                             // for keeping track of which lap we're on
        int PreviousLap = 0;                                        // to allow checking when lap increments

        public bool IsBound = false;                                // set true when service bound by main activity
        public bool IsForegroundStarted = false;                    // set true when service is started by main activity
        public bool IsStarted = false;                              // track if already started, don't restart if already started

        bool simulation = false;                                    // indicates whether to use simulated eChook and location data

        SensorManager? sensorManager;                               // for accessing accelerometer and gyroscope
        static readonly Lock syncLock = new();						// used when reading changed sensor data

        readonly ServiceUpdater serviceUpdater;                     // for handling passing back of data from listeners, providers, and senders

        LocationListener? locationListener;                         // for listening for location (GPS position) updates
        BluetoothListener? bluetoothListener;                       // for listening for data sent over bluetooth from the ehooks
        SimulationProvider? simulationProvider;                     // for providing simulated eChook and location data in lieu of above two listeners
        LoggingSender? loggingSender;                               // for writing data to the log file
        UploadSender? uploadSender;                                 // for uploading data over the mobile network

        public MainService()
        {
            ServiceBinder = new ServiceBinder { Service = this };

            serviceUpdater = new ServiceUpdater(Looper.MyLooper(), intent => OnServiceUpdate(intent));

            lapCounter = new LapCounter();
            SetState(State.WaitingForThrottleOrCurrent);
        }

        public override void OnCreate()
        {
            Log.Info(TAG, "onCreate.");

            base.OnCreate();

            locationListener = new LocationListener(this, serviceUpdater);
            bluetoothListener = new BluetoothListener(this, serviceUpdater);
            loggingSender = new LoggingSender(this);
            uploadSender = new UploadSender(this, serviceUpdater);
            simulationProvider = new SimulationProvider(this, serviceUpdater);

            sensorManager = (SensorManager?)GetSystemService(SensorService);

            string name = GetString(Resource.String.app_name);
            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            var notificationChannel = new NotificationChannel(Constants.NOTIFICATION_CHANNEL_ID, name, NotificationImportance.Default);
            notificationManager?.CreateNotificationChannel(notificationChannel);

            PreferenceManager.GetDefaultSharedPreferences(this)?.RegisterOnSharedPreferenceChangeListener(this);

            var powerManager = (PowerManager?)GetSystemService(Context.PowerService);
            wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "MainServiceWakeLock");
            wakeLock?.Acquire();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            Log.Info(TAG, "onStartCommand.");

            if (intent == null) return StartCommandResult.NotSticky;

            simulation = intent.GetBooleanExtra(Constants.SIMULATION_NAME, false);

            if (IsForegroundStarted)
            {
                Log.Info(TAG, "Foreground service already running");
                return StartCommandResult.NotSticky;
            }

            Log.Info(TAG, "Starting foreground service.");

            // Allow foreground service to be started whether or not have all the necessary permissions to allow simulation.

            var haveLocationPermissions = PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions);
            var haveBluetoothPermissions = PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions);
            //var haveNotificationsPermissions = PermissionsHelper.CheckPermissions(PermissionsHelper.NotificationPermissions);

            try
            {
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Tiramisu)
                {
                    StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, GetNotification());
                    IsForegroundStarted = true;
                }
                else
                {
                    ForegroundService foregroundServiceType = 0;
                    if (haveLocationPermissions) foregroundServiceType |= ForegroundService.TypeLocation;
                    if (haveBluetoothPermissions) foregroundServiceType |= ForegroundService.TypeConnectedDevice;

                    StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, GetNotification(), foregroundServiceType);
                    IsForegroundStarted = true;
                }
            }
            catch (Exception ex)
            {
                Log.Info(TAG, "Exception starting in foreground" + ex.Message != null ? ": " + ex.Message : "");
            }

            if (IsForegroundStarted) Start();

            // Tells the system to not try to recreate the service after it has been killed.
            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind(Intent? intent)
        {
            Log.Info(TAG, "onBind.");

            return ServiceBinder;
        }

        public override void OnRebind(Intent? intent)
        {
            Log.Info(TAG, "onRebind.");
            base.OnRebind(intent);
        }

        public override bool OnUnbind(Intent? intent)
        {
            Log.Info(TAG, "onUnbind.");
            // Called when the client (MainActivity in our case) unbinds from this service. 
            return true; // Ensures onRebind() is called when a client re-binds.
        }

        public override void OnDestroy()
        {
            Log.Info(TAG, "onDestroy.");

            base.OnDestroy();

            PreferenceManager.GetDefaultSharedPreferences(this)?.UnregisterOnSharedPreferenceChangeListener(this);

            wakeLock?.Release();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
        {
            // if bluetooth device changed, stop and restart bluetoothlistener

            if (!IsStarted) return;
            if (string.IsNullOrEmpty(key)) return;

            if (key.Equals(GetString(Resource.String.preference_key_bluetooth_device)) && !simulation && bluetoothListener != null)
            {
                if (bluetoothListener.IsStarted) bluetoothListener.Stop();
                if (PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions))
                {
                    bluetoothListener.Start();
                }
                return;
            }

            // if upload target or any upload parameter changed, stop and restart upload sender

            List<int> uploadKeys =
            [
                Resource.String.preference_key_upload,
                Resource.String.preference_key_upload_banchory_greenpower_car_name,
                Resource.String.preference_key_upload_banchory_greenpower_password,
                Resource.String.preference_key_upload_echook_live_data_car_name,
                Resource.String.preference_key_upload_echook_live_data_password,
                Resource.String.preference_key_upload_user_defined_car_name,
                Resource.String.preference_key_upload_user_defined_url,
                Resource.String.preference_key_upload_user_defined_username,
                Resource.String.preference_key_upload_user_defined_password,
                Resource.String.preference_key_upload_IMU
            ];

            foreach (var uploadKey in uploadKeys)
            {
                if (key.Equals(GetString(uploadKey)) && uploadSender != null)
                {
                    if (uploadSender.IsStarted) uploadSender.Stop();
                    uploadSender.Start();
                    return;
                }
            }
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
        {
            // do nothing, but need to implement for the interface
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            const double SensorPeriod = 0.2;        // SensorDelay.Normal when registering listener gives updates every 200ms

            if (e == null || e.Sensor == null || e.Values == null) return;

            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                double currentAccelerationX;
                double currentAccelerationY;
                double currentAccelerationZ;
                lock (syncLock)
                {
                    currentAccelerationX = e.Values[0];
                    currentAccelerationY = e.Values[1];
                    currentAccelerationZ = e.Values[2];
                }
                double previousAccelerationX = Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELX_KEY, 0.0);
                double previousAccelerationY = Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELY_KEY, 0.0);
                double previousAccelerationZ = Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELZ_KEY, 0.0);
                currentAccelerationX = DigitalFilter.LowPass(currentAccelerationX, previousAccelerationX, SensorPeriod, Constants.ACCELEROMETER_FILTER_CUTOFF_FREQ);
                currentAccelerationY = DigitalFilter.LowPass(currentAccelerationY, previousAccelerationY, SensorPeriod, Constants.ACCELEROMETER_FILTER_CUTOFF_FREQ);
                currentAccelerationZ = DigitalFilter.LowPass(currentAccelerationZ, previousAccelerationZ, SensorPeriod, Constants.ACCELEROMETER_FILTER_CUTOFF_FREQ);
                Shared.Data.PutDouble(Constants.SHARED_DATA_ACCELX_KEY, currentAccelerationX);
                Shared.Data.PutDouble(Constants.SHARED_DATA_ACCELY_KEY, currentAccelerationY);
                Shared.Data.PutDouble(Constants.SHARED_DATA_ACCELZ_KEY, currentAccelerationZ);
            }
            else if (e.Sensor.Type == SensorType.Gyroscope)
            {
                double currentAngularVelocityX;
                double currentAngularVelocityY;
                double currentAngularVelocityZ;
                lock (syncLock)
                {
                    currentAngularVelocityX = e.Values[0];
                    currentAngularVelocityY = e.Values[1];
                    currentAngularVelocityZ = e.Values[2];
                }
                double previousAngularVelocityX = Shared.Data.GetDouble(Constants.SHARED_DATA_GYROX_KEY, 0.0);
                double previousAngularVelocityY = Shared.Data.GetDouble(Constants.SHARED_DATA_GYROY_KEY, 0.0);
                double previousAngularVelocityZ = Shared.Data.GetDouble(Constants.SHARED_DATA_GYROZ_KEY, 0.0);
                currentAngularVelocityX = DigitalFilter.LowPass(currentAngularVelocityX, previousAngularVelocityX, SensorPeriod, Constants.GYROSCOPE_FILTER_CUTOFF_FREQ);
                currentAngularVelocityY = DigitalFilter.LowPass(currentAngularVelocityY, previousAngularVelocityY, SensorPeriod, Constants.GYROSCOPE_FILTER_CUTOFF_FREQ);
                currentAngularVelocityZ = DigitalFilter.LowPass(currentAngularVelocityZ, previousAngularVelocityZ, SensorPeriod, Constants.GYROSCOPE_FILTER_CUTOFF_FREQ);
                Shared.Data.PutDouble(Constants.SHARED_DATA_GYROX_KEY, currentAngularVelocityX);
                Shared.Data.PutDouble(Constants.SHARED_DATA_GYROY_KEY, currentAngularVelocityY);
                Shared.Data.PutDouble(Constants.SHARED_DATA_GYROZ_KEY, currentAngularVelocityZ);
            }

        }

        Notification GetNotification()
        {
            // Took a loooong time to find correct combination of flags to prevent a second activity being opened
            // https://stackoverflow.com/questions/19039189/intent-if-activity-is-running-bring-it-to-front-else-start-a-new-one-from-n

            Intent intent = new(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            // The PendingIntent to launch activity.
            PendingIntent? activityPendingIntent;
            activityPendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var text = "Touch to open app";
            Notification.Builder builder = new(this, Constants.NOTIFICATION_CHANNEL_ID);
            builder.SetContentTitle("OMNI telemetry");
            builder.SetContentText(text);
            builder.SetContentIntent(activityPendingIntent);
            builder.SetOngoing(true);
            builder.SetSmallIcon(Resource.Drawable.ic_stat_name);
            builder.SetCategory(Notification.CategoryNavigation);       // ensures notification is immediate i.e. not delayed
                                                                        // by 10 seconds
            return builder.Build();
        }

        public void Start()
        {
            if (!IsBound)
            {
                Log.Info(TAG, "Waiting for service to be bound before starting the real work.");
                return;
            }
            if (!IsForegroundStarted)
            {
                Log.Info(TAG, "Waiting for foreground service to be started before starting the real work.");
                return;
            }

            Log.Info(TAG, "Starting the real work.");

            sensorManager?.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Normal);
            sensorManager?.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Normal);

            IsStarted = true;
            if (!simulation)
            {
                if (locationListener != null)
                {
                    if (locationListener.IsStarted) locationListener.Stop();
                    if (PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions)) locationListener.Start();
                }
                if (bluetoothListener != null)
                {
                    if (bluetoothListener.IsStarted) bluetoothListener.Stop();
                    if (PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions)) bluetoothListener.Start();
                }
            }
            else
            {
                simulationProvider?.Start();
            }
            if (loggingSender != null)
            {
                if (loggingSender.IsStarted) loggingSender.Stop();
                loggingSender.Start();
            }
            if (uploadSender != null)
            {
                if (uploadSender.IsStarted) uploadSender.Stop();
                uploadSender.Start();
            }
        }

        public void Stop()
        {
            Log.Info(TAG, "Stopping the real work");

            if (!simulation)
            {
                if (locationListener != null && locationListener.IsStarted) locationListener.Stop();
                if (bluetoothListener != null && bluetoothListener.IsStarted) bluetoothListener.Stop();
            }
            else
            {
                simulationProvider?.Stop();
            }
            if (loggingSender != null && loggingSender.IsStarted) loggingSender.Stop();
            if (uploadSender != null && uploadSender.IsStarted) uploadSender.Stop();

            sensorManager?.UnregisterListener(this);

            IsStarted = false;
        }

        public void CloseService()
        {
            if (IsForegroundStarted)
            {
                Log.Info(TAG, "Stopping foreground service");
                Stop();
            }

            Log.Info(TAG, "Killing foreground service");
            StopSelf();
            System.Diagnostics.Process.GetCurrentProcess().Kill();      // heavy handed way to ensure service terminates!!!
                                                                        // otherwise it may continue running as a background service (no notification)
                                                                        // maybe should be concerned about memory leakage here
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "<Pending>")]
        private void OnServiceUpdate(Intent? Intent)
        {
            if (Intent != null && Intent.Action != null)
            {
                if (Intent.Action.Equals(Constants.BLUETOOTH_DATA_BROADCAST_ACTION))
                {
                    var echookData = Intent.GetBundleExtra("data");
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
                        DataAvailable([.. echookDataKeySet]);
                    }
                }
                else if (Intent.Action.Equals(Constants.BLUETOOTH_STATUS_BROADCAST_ACTION))
                {
                    var isConnected = Intent.GetIntExtra("isconnected", 0);
                    Shared.Data.PutInt(Constants.SHARED_DATA_BLUETOOTH_CONNECTED_KEY, isConnected);
                }
                else if (Intent.Action.Equals(Constants.LOCATION_DATA_BROADCAST_ACTION))
                {
                    if (Intent.HasExtra("location"))
                    {
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                            Shared.Data.PutParcelable(Constants.SHARED_DATA_LOCATION_KEY, (Location?)Intent.GetParcelableExtra("location", Class.FromType(typeof(Location))));
                        else
                            Shared.Data.PutParcelable(Constants.SHARED_DATA_LOCATION_KEY, (Location?)Intent.GetParcelableExtra("location"));
                        DataAvailable([Constants.SHARED_DATA_LOCATION_KEY]);
                    }
                    if (Intent.HasExtra("address"))
                    {
                        Shared.Data.PutString(Constants.SHARED_DATA_ADDRESS_KEY, Intent.GetStringExtra("address"));
                        DataAvailable([Constants.SHARED_DATA_ADDRESS_KEY]);
                    }
                }
                else if (Intent.Action.Equals(Constants.UPLOAD_DATA_BROADCAST_ACTION))
                {
                    ServiceBinder.DataUpload();
                }
            }
        }

        public void DataAvailable(List<string> Keys)
        {
            var keys = Keys.ToList();
            var now = new Date();

            // Calculate the 'upper' voltage, i.e. of the second battery, each time we get updated total voltage of both batteries

            if (Keys.Contains(Constants.SHARED_DATA_VTOTAL_KEY))
            {
                Shared.Data.PutDouble(Constants.SHARED_DATA_VUPPER_KEY, Shared.Data.GetDouble(Constants.SHARED_DATA_VTOTAL_KEY, 0)
                                                                        - Shared.Data.GetDouble(Constants.SHARED_DATA_VLOWER_KEY, 0));
                keys.Add(Constants.SHARED_DATA_VUPPER_KEY);
            }

            // Calculate As and J each time we get an updated current

            if (Keys.Contains(Constants.SHARED_DATA_CURRENT_KEY))
            {
                var amps = Shared.Data.GetDouble(Constants.SHARED_DATA_CURRENT_KEY, 0);
                var watts = amps * Shared.Data.GetDouble(Constants.SHARED_DATA_VTOTAL_KEY, 0);
                if ((CalculatedData.previousAmpsTime != null) && (CalculatedData.previousAmpsTime != now))
                {
                    var deltaTime = (now.Time - CalculatedData.previousAmpsTime.Time) / 1000.0;
                    var deltaAs = (amps + CalculatedData.previousAmps) / 2.0 * deltaTime;
                    var deltaJ = (watts + CalculatedData.previousWatts) / 2.0 * deltaTime;
                    CalculatedData.calculatedAs += deltaAs;
                    CalculatedData.calculatedJ += deltaJ;
                    Shared.Data.PutDouble(Constants.SHARED_DATA_AS_KEY, CalculatedData.calculatedAs);
                    Shared.Data.PutDouble(Constants.SHARED_DATA_J_KEY, CalculatedData.calculatedJ);
                    keys.Add(Constants.SHARED_DATA_AS_KEY);
                    keys.Add(Constants.SHARED_DATA_J_KEY);
                }
                CalculatedData.previousAmps = amps;
                CalculatedData.previousWatts = watts;
                CalculatedData.previousAmpsTime = now;
            }

            // Calculate distance each time we get an updated speed

            if (Keys.Contains(Constants.SHARED_DATA_SPEED_KEY))
            {
                var speed = Shared.Data.GetDouble(Constants.SHARED_DATA_SPEED_KEY, 0);
                if ((CalculatedData.previousSpeedTime != null) && (CalculatedData.previousSpeedTime != now))
                {
                    var deltaTime = now.Time - CalculatedData.previousSpeedTime.Time;
                    var deltaDistance = (speed + CalculatedData.previousSpeed) / 2.0 * deltaTime / 1000;
                    CalculatedData.calculatedDistance += deltaDistance;
                    Shared.Data.PutDouble(Constants.SHARED_DATA_DISTANCE_KEY, CalculatedData.calculatedDistance);
                    keys.Add(Constants.SHARED_DATA_DISTANCE_KEY);
                }
                CalculatedData.previousSpeed = speed;
                CalculatedData.previousSpeedTime = now;
            }

            // Calculate effciency at regular intervals

            if ((CalculatedData.previousEfficiencyTime == null) || ((now.Time - CalculatedData.previousEfficiencyTime.Time) >= Constants.CALCULATE_EFFICIENCY_INTERVAL))
            {
                var As = Shared.Data.GetDouble(Constants.SHARED_DATA_AS_KEY, 0);
                var J = Shared.Data.GetDouble(Constants.SHARED_DATA_J_KEY, 0);
                var distance = Shared.Data.GetDouble(Constants.SHARED_DATA_DISTANCE_KEY, 0);
                if (CalculatedData.previousEfficiencyTime != null)
                {
                    var deltaAs = As - CalculatedData.previousEfficiencyAs;
                    var deltaJ = J - CalculatedData.previousEfficiencyJ;
                    var deltaDistance = distance - CalculatedData.previousEfficiencyDistance;
                    double efficiencyAs = 0;
                    double efficiencyJ = 0;
                    if (deltaDistance > 0)
                    {
                        efficiencyAs = deltaAs / deltaDistance;
                        efficiencyJ = deltaJ / deltaDistance;
                    }
                    Shared.Data.PutDouble(Constants.SHARED_DATA_EFFICIENCY_ASPM_KEY, efficiencyAs);
                    Shared.Data.PutDouble(Constants.SHARED_DATA_EFFICIENCY_JPM_KEY, efficiencyJ);
                    keys.Add(Constants.SHARED_DATA_EFFICIENCY_ASPM_KEY);
                    keys.Add(Constants.SHARED_DATA_EFFICIENCY_JPM_KEY);
                }
                CalculatedData.previousEfficiencyAs = As;
                CalculatedData.previousEfficiencyJ = J;
                CalculatedData.previousEfficiencyDistance = distance;
                CalculatedData.previousEfficiencyTime = now;
            }

            // Handle launch state

            if (Keys.Contains(Constants.SHARED_DATA_LAUNCH_BUTTON_KEY) && (state == State.WaitingForThrottleOrCurrent))
            {
                SetState(State.WaitingForThrottleOrCurrent);        // continue waiting for throttle, however reset state
            }
            if ((Keys.Contains(Constants.SHARED_DATA_INPUT_THROTTLE_KEY)
                || Keys.Contains(Constants.SHARED_DATA_CURRENT_KEY))
                && (state == State.WaitingForThrottleOrCurrent))
            {
                var throttle = Shared.Data.GetDouble(Constants.SHARED_DATA_INPUT_THROTTLE_KEY);
                var current = Shared.Data.GetDouble(Constants.SHARED_DATA_CURRENT_KEY);
                if (throttle >= Constants.LAUNCH_THROTTLE || current >= Constants.LAUNCH_CURRENT) SetState(State.Running);
            }
            keys.Add(Constants.SHARED_DATA_STATE_KEY);  // send every time to catch changes

            // Calculated elapsed race time

            if (Shared.Data.ContainsKey(Constants.SHARED_DATA_START_TIME_KEY))
            {
                long raceStartTime = Shared.Data.GetLong(Constants.SHARED_DATA_START_TIME_KEY);
                int raceSeconds = (raceStartTime > 0 ? Convert.ToInt32((new Java.Util.Date().Time - raceStartTime)) / 1000 : 0);
                Shared.Data.PutInt(Constants.SHARED_DATA_ELAPSED_TIME_KEY, raceSeconds);
                keys.Add(Constants.SHARED_DATA_ELAPSED_TIME_KEY);
            }

            // Handle lap counting when get an updated location

            if (Keys.Contains(Constants.SHARED_DATA_LOCATION_KEY))
            {
                var location = LocationHelper.GetLocationFromBundle(Shared.Data, Constants.SHARED_DATA_LOCATION_KEY);
                if (location != null)
                {
                    var lap = lapCounter.PositionUpdate(location);
                    Shared.Data.PutInt(Constants.SHARED_DATA_LAP_KEY, lap);
                    keys.Add(Constants.SHARED_DATA_LAP_KEY);
                    if (lap != PreviousLap)                 // Force an immediate upload of data with timestamp of when lap incremented
                    {                                       // rather than wait for the next timed upload of data
                        uploadSender?.UploadNowAsync();     // Conciously not awaiting the upload to complete
                        PreviousLap = lap;
                    }
                }
            }

            // Finally let the main activity know what additional data is now available

            this.ServiceBinder.DataAvailable(keys);
        }

        public void ResetDistance()
        {
            CalculatedData.calculatedDistance = 0.0;
            Shared.Data.PutDouble(Constants.SHARED_DATA_DISTANCE_KEY, CalculatedData.calculatedDistance);
            this.ServiceBinder.DataAvailable([Constants.SHARED_DATA_DISTANCE_KEY]);
        }

        public void ResetEnergy()
        {
            CalculatedData.calculatedAs = 0.0;
            CalculatedData.calculatedJ = 0.0;
            Shared.Data.PutDouble(Constants.SHARED_DATA_AS_KEY, CalculatedData.calculatedAs);
            Shared.Data.PutDouble(Constants.SHARED_DATA_J_KEY, CalculatedData.calculatedJ);
            this.ServiceBinder.DataAvailable([Constants.SHARED_DATA_AS_KEY, Constants.SHARED_DATA_J_KEY]);
        }

        public void ResetElapsedTime()
        {
            Shared.Data.PutLong(Constants.SHARED_DATA_START_TIME_KEY, (new Date()).Time);
            Shared.Data.PutInt(Constants.SHARED_DATA_ELAPSED_TIME_KEY, 0);
            this.ServiceBinder.DataAvailable([Constants.SHARED_DATA_START_TIME_KEY, Constants.SHARED_DATA_ELAPSED_TIME_KEY]);
        }

        public void ResetLap()
        {
            lapCounter.ResetLapNumber();
            var lap = lapCounter.PositionUpdate(null);
            Shared.Data.PutInt(Constants.SHARED_DATA_LAP_KEY, lap);
            this.ServiceBinder.DataAvailable([Constants.SHARED_DATA_LAP_KEY]);
        }

        public void SetState(State state)
        {
            this.state = state;
            Shared.Data.PutInt(Constants.SHARED_DATA_STATE_KEY, (int)state);
            switch (state)
            {
                case State.WaitingForThrottleOrCurrent:
                    lapCounter.Reset();
                    var lap = lapCounter.PositionUpdate(null);
                    Shared.Data.PutInt(Constants.SHARED_DATA_LAP_KEY, lap);
                    lapCounter.AcquireStartLocation();
                    break;
                case State.Running:
                    lapCounter.AcquireLapCount();
                    Shared.Data.PutLong(Constants.SHARED_DATA_START_TIME_KEY, new Date().Time);
                    Shared.Data.PutInt(Constants.SHARED_DATA_ELAPSED_TIME_KEY, 0);
                    break;
            }
        }
    }

}
