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

using Java.Nio.Channels;

namespace Omni
{
    public class LoggingSender
    {
        static readonly string TAG = typeof(LoggingSender).FullName ?? "LoggingSender";

        public bool IsStarted { get; private set; } = false;

        readonly MainService Service;           // service from which sender instantiated
        readonly Timer? Timer;                  // timer on which sender runs

        readonly string? LogFilePath;           // path to log file 
        bool HeadersOutput;                     // true when CSV column headers have been output

        readonly C[]? csv;                      // array containing definition of what goes in the CSV file

        public LoggingSender(MainService Service)
        {
            this.Service = Service;

            if (Service != null)
            {
                var logFileName = "Omni-Log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
                var exportPath = new Java.IO.File(Service.GetExternalFilesDir(null), "LoggedData");
                if (!Directory.Exists(exportPath.AbsolutePath)) Directory.CreateDirectory(exportPath.AbsolutePath);
                var fileExportPath = new Java.IO.File(exportPath, logFileName);
                LogFilePath = fileExportPath.AbsolutePath;
                Shared.LogFilePath = LogFilePath;                    // also save in global shared to allow  use of 'share' from main activity

                HeadersOutput = false;

                Timer = new Timer { Interval = Constants.LOGGING_INTERVAL, Enabled = false };
                Timer.Elapsed += Timer_Elapsed;

                // Define what's included in the CSV file

                csv =
                [
                new C("Timestamp",            (now, location) => { return (now - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds.ToString("0.000", CultureInfo.InvariantCulture); }),
                new C("DateTime (date)",      (now, location) => { return now.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture); }),
                new C("DateTime (ms)",        (now, location) => { return now.ToLocalTime().ToString("fff", CultureInfo.InvariantCulture); }),
                new C("Vehicle name",         (now, location) => { if (Service.Resources != null) 
                                                                   { 
                                                                        return EncryptedPreferenceHelper.DataStore?.GetString(Service.Resources.GetString(Resource.String.preference_key_car_name), "Car") ?? "Car"; 
                                                                   } else { 
                                                                        return "Car"; 
                                                                   } }),
                new C("BluetoothConnected",   typeof(int),    Constants.SHARED_DATA_BLUETOOTH_CONNECTED_KEY, "0"),
                new C("RaceTime (s)",         typeof(int),    Constants.SHARED_DATA_ELAPSED_TIME_KEY, "0"),
                new C("Speed (m/s)",          typeof(double), Constants.SHARED_DATA_SPEED_KEY, "0.00"),
                new C("Speed (mph)",          typeof(double), Constants.SHARED_DATA_SPEED_KEY, Constants.MPS2MPH, "0.00"),
                new C("GPS speed (m/s)",      (now, location) => { return location != null && location.HasSpeed ? location.Speed.ToString("0.00", CultureInfo.InvariantCulture) : String.Empty;  }),
                new C("Distance (m)",         typeof(double), Constants.SHARED_DATA_DISTANCE_KEY, "0"),
                new C("Distance (miles)",     typeof(double), Constants.SHARED_DATA_DISTANCE_KEY, Constants.M2MILES, "0.0"),
                new C("Lap",                  typeof(int),    Constants.SHARED_DATA_LAP_KEY, "0"),
                new C("Total voltage (V)",    typeof(double), Constants.SHARED_DATA_VTOTAL_KEY, "0.0"),
                new C("Lower voltage (V)",    typeof(double), Constants.SHARED_DATA_VLOWER_KEY, "0.0"),
                new C("Upper voltage (V)",    typeof(double), Constants.SHARED_DATA_VUPPER_KEY, "0.0"),
                new C("Current (A)",          typeof(double), Constants.SHARED_DATA_CURRENT_KEY, "0.0"),
                new C("AmpHours (Ah)",        typeof(double), Constants.SHARED_DATA_AS_KEY, Constants.AS2AH, "0.0"),
                new C("Energy (Wh)",          typeof(double), Constants.SHARED_DATA_J_KEY, Constants.J2WH, "0.0"),
                new C("Energy (kJ)",          typeof(double), Constants.SHARED_DATA_J_KEY, 0.001, "0.0"),
                new C("Efficiency (Ah/mile)", typeof(double), Constants.SHARED_DATA_EFFICIENCY_ASPM_KEY, Constants.ASPM2AHPMILE, "0.000"),
                new C("Efficiency (kJ/mile)", typeof(double), Constants.SHARED_DATA_EFFICIENCY_JPM_KEY,  Constants.JPM2KJPMILE,  "0.000"),
                new C("Input throttle (%)",   typeof(double), Constants.SHARED_DATA_INPUT_THROTTLE_KEY, "0"),
                new C("Actual throttle (%)",  typeof(double), Constants.SHARED_DATA_ACTUAL_THROTTLE_KEY, "0"),
                new C("Brake (%)",            typeof(int),    Constants.SHARED_DATA_BRAKE_PRESSED_KEY, "0"),
                new C("Motor speed (RPM)",    typeof(double), Constants.SHARED_DATA_RPM_KEY, "0"),
                new C("Gear ratio",           typeof(double), Constants.SHARED_DATA_GEAR_RATIO_KEY, "0.00"),
                new C("Temperature 1 (°C)",   typeof(double), Constants.SHARED_DATA_TEMPERATURE_1_KEY, "0.0"),
                new C("Temperature 2 (°C)",   typeof(double), Constants.SHARED_DATA_TEMPERATURE_2_KEY, "0.0"),
                new C("GPS latitude (°)",     (now, location) => { return location != null ? location.Latitude.ToString("0.000000", CultureInfo.InvariantCulture)  : String.Empty ; }),
                new C("GPS longitude (°)",    (now, location) => { return location != null ? location.Longitude.ToString("0.000000", CultureInfo.InvariantCulture) : String.Empty ; }),
                new C("GPS altitude (m)",     (now, location) => { return location != null && location.HasAltitude ? location.Altitude.ToString("0", CultureInfo.InvariantCulture) : String.Empty; }),
                new C("GPS accuracy (m)",     (now, location) => { return location != null && location.HasAccuracy ? location.Accuracy.ToString("0", CultureInfo.InvariantCulture) : String.Empty; }),
                new C("GPS time",             (now, location) => { return location != null ? location.Time.ToString(CultureInfo.InvariantCulture) : String.Empty; }),
                new C("Custom 0",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_0_KEY, "0.00"),
                new C("Custom 1",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_1_KEY, "0.00"),
                new C("Custom 2",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_2_KEY, "0.00"),
                new C("Custom 3",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_3_KEY, "0.00"),
                new C("Custom 4",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_4_KEY, "0.00"),
                new C("Custom 5",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_5_KEY, "0.00"),
                new C("Custom 6",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_6_KEY, "0.00"),
                new C("Custom 7",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_7_KEY, "0.00"),
                new C("Custom 8",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_8_KEY, "0.00"),
                new C("Custom 9",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_9_KEY, "0.00"),
                new C("Custom W",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_W_KEY, "0.00"),
                new C("Custom X",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_X_KEY, "0.00"),
                new C("Custom Y",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_Y_KEY, "0.00"),
                new C("Custom Z",             typeof(double), Constants.SHARED_DATA_CUSTOM_DATA_Z_KEY, "0.00"),
                new C("Acceleration X (m/s/s)",typeof(double), Constants.SHARED_DATA_ACCELX_KEY, "0.000"),
                new C("Acceleration Y (m/s/s)",typeof(double), Constants.SHARED_DATA_ACCELY_KEY, "0.000"),
                new C("Acceleration Z (m/s/s)",typeof(double), Constants.SHARED_DATA_ACCELZ_KEY, "0.000"),
                new C("Ang velocity X (rad/s)",          typeof(double), Constants.SHARED_DATA_GYROX_KEY, "0.000"),
                new C("Ang velocity Y (rad/s)",          typeof(double), Constants.SHARED_DATA_GYROY_KEY, "0.000"),
                new C("Ang velocity Z (rad/s)",          typeof(double), Constants.SHARED_DATA_GYROZ_KEY, "0.000")
                ];
            }
        }

        internal class C(string Header, Type? Type, string? SharedDataKey, double Multiplier, string? Format, Func<DateTime, Location?, string?>? OtherSource)
        {
            public string Header { get; } = Header;
            public Type? Type { get; } = Type;
            public string? SharedDataKey { get; } = SharedDataKey;
            public double Multiplier { get; } = Multiplier;
            public string? Format { get; } = Format;
            public Func<DateTime, Location?, string?>? OtherSource { get; } = OtherSource;

            public C(string Header, Type Type, string SharedDataKey, string Format) : this(Header, Type, SharedDataKey, 1.0, Format, null) { }
            public C(string Header, Type Type, string SharedDataKey, double Multiplier, string Format) : this(Header, Type, SharedDataKey, Multiplier, Format, null) { }
            public C(string Header, Func<DateTime, Location?, string?> OtherSource) : this(Header, null, null, 0.0, null, OtherSource) { }
        }

        private void OutputHeaders()
        {
            if (csv == null ||  csv.Length == 0 || LogFilePath == null) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < csv.Length; i++)
            {
                sb.Append('"');
                sb.Append(csv[i].Header);
                sb.Append('"');
                if (i != csv.Length - 1) sb.Append(',');
            }
            sb.Append(System.Environment.NewLine);

            File.AppendAllText(LogFilePath, sb.ToString());
        }

        private void OutputValues()
        {
            if (csv == null || csv.Length == 0 || LogFilePath == null) return;

            DateTime now = DateTime.UtcNow;
            var location = LocationHelper.GetLocationFromBundle(Shared.Data, Constants.SHARED_DATA_LOCATION_KEY);
            var sb = new System.Text.StringBuilder();
            for (int i=0; i < csv.Length; i++)
            {
                var c = csv[i];
                if (c.OtherSource != null)
                {
                    sb.Append(c.OtherSource(now, location) ?? string.Empty);
                }
                else if (c.Type == typeof(int))
                {
                    sb.Append((Shared.Data.GetInt(c.SharedDataKey, 0) * c.Multiplier).ToString(c.Format, CultureInfo.InvariantCulture));
                }
                else if (c.Type == typeof(long))
                {
                    sb.Append((Shared.Data.GetLong(c.SharedDataKey, 0) * c.Multiplier).ToString(c.Format, CultureInfo.InvariantCulture));
                }
                else if (c.Type == typeof(double))
                {
                    sb.Append((Shared.Data.GetDouble(c.SharedDataKey, 0) * c.Multiplier).ToString(c.Format, CultureInfo.InvariantCulture));
                }
                else if (c.Type == typeof(string))
                {
                    sb.Append('"');
                    sb.Append(Shared.Data.GetString(c.SharedDataKey, ""));
                    sb.Append('"');
                }
                if (i != csv.Length - 1) sb.Append(',');
            }
            sb.Append(System.Environment.NewLine);
            File.AppendAllText(LogFilePath, sb.ToString());
        }

        public void Start()
        {
            Log.Info(TAG, "Logging sender start.");
            IsStarted = true;
            var _ = Service;    // workaround to avoid "Private member 'LoggingSender.Service' can be removed as the value assigned to it is never read"
            Timer?.Start();
        }

        public void Stop()
        {
            Log.Info(TAG, "Logging sender stop.");
            IsStarted = false;
            Timer?.Stop();
        }

        private  void Timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            if (!HeadersOutput)
            {
                OutputHeaders();
                HeadersOutput = true;
            }
            OutputValues();
        }

    }
}