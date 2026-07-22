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
    public static class Constants
    {
        public const double MPS2MPH  = 2.23693629;                      // multiplier from m/s to mph
        public const double MPS2KMPH = 3.6;                             // multiplier from m/s to km/h
 
        public const double M2MILES  = 0.000621371;                     // multiplier from m to miles

        public const double AS2AH    = 1.0 / 3600;                      // multiplier from ampseconds to amphours
        public const double J2WH     = 1.0 / 3600;                      // multipler from joules (wattseconds) to watthours

        public const double ASPM2AHPMILE = AS2AH / M2MILES;             // multiplier from ampseconds per meter to amphours per mile
        public const double JPM2KJPMILE = 0.001 / M2MILES;              // multiplier from joules per meter to kilojoules per mile

        public const int    BLUETOOTH_RETRY_DELAY = 2000;               // ms
        public const int    REDO_GEOCODE_DISTANCE = 100;                // m
        public const double LAUNCH_THROTTLE       = 30.0;               // % of throttle indicating car started
        public const double LAUNCH_CURRENT        = 50.0;               // current in amps indicating car started

        public const int    TIME_OF_DAY_UPDATE_INTERVAL      = 1000;    // ms
        public const int    SPLASH_SCREEN_INTERVAL           = 2000;    // ms
        public const long   LOCATION_UPDATE_DESIRED_INTERVAL = 1000;    // ms
        public const double ACCELEROMETER_FILTER_CUTOFF_FREQ =  0.5;    // Hz
        public const double GYROSCOPE_FILTER_CUTOFF_FREQ     =  0.5;    // Hz
        public const long   CALCULATE_EFFICIENCY_INTERVAL    = 2000;    // ms
        public const double UPLOAD_INTERVAL                  = 1500;    // ms
        public const int    UPLOAD_WATCHDOG_INTERVAL         = 5000;    // ms
        public const int    BLUETOOTH_WATCHDOG_INTERVAL      = 2000;    // ms
        public const double LOGGING_INTERVAL                 =  250;    // ms

        public const int    SERVICE_RUNNING_NOTIFICATION_ID   = 10000;
        public const int    BLUETOOTH_ENABLE_REQUEST_CODE     =  1200;

        public const string NOTIFICATION_CHANNEL_ID = "omni.channel_01";
        public const string MAINSERVICE_ACTION = "net.keduro.omni.omniservice";
        public const string BLUETOOTH_DATA_BROADCAST_ACTION = "omni.broadcast.bluetooth.data";
        public const string BLUETOOTH_STATUS_BROADCAST_ACTION = "omni.broadcast.bluetooth.status";
        public const string LOCATION_DATA_BROADCAST_ACTION = "omni.broadcast.location.data";
        public const string UPLOAD_DATA_BROADCAST_ACTION = "omni.broadcast.upload.data";

        public const string FRAGMENT_TYPE_KEY = "fragment";
        public const string FRAGMENT_TAG_KEY  = "fragment.tag";
        public const string DEVELOPER_KEY = "developer";
        public const string PERMISSIONS_KEY = "permissions";

        public const string SIMULATION_NAME = "simulation";
        public const string SCANNING_QR_CODE = "scanning_qr_code";

        public const string FILE_PROVIDER_AUTHORITY = "net.keduro.omni.provider";

        // shared data keys for data from eChook bluetooth listener

        public const string SHARED_DATA_SPEED_KEY = "s";
        public const string SHARED_DATA_RPM_KEY = "m";
        public const string SHARED_DATA_CURRENT_KEY = "i";
        public const string SHARED_DATA_VTOTAL_KEY = "v";
        public const string SHARED_DATA_VLOWER_KEY = "w";
        public const string SHARED_DATA_INPUT_THROTTLE_KEY = "t";
        public const string SHARED_DATA_ACTUAL_THROTTLE_KEY = "d";
        public const string SHARED_DATA_TEMPERATURE_1_KEY = "a";
        public const string SHARED_DATA_TEMPERATURE_2_KEY = "b";
        public const string SHARED_DATA_TEMPERATURE_3_KEY = "c";
        public const string SHARED_DATA_LAUNCH_BUTTON_KEY = "L";
        public const string SHARED_DATA_CYCLE_VIEW_BUTTON_KEY = "C";
        public const string SHARED_DATA_GEAR_RATIO_KEY = "r";
        public const string SHARED_DATA_BRAKE_PRESSED_KEY = "B";
        public const string SHARED_DATA_CUSTOM_DATA_0_KEY = "0";
        public const string SHARED_DATA_CUSTOM_DATA_1_KEY = "1";
        public const string SHARED_DATA_CUSTOM_DATA_2_KEY = "2";
        public const string SHARED_DATA_CUSTOM_DATA_3_KEY = "3";
        public const string SHARED_DATA_CUSTOM_DATA_4_KEY = "4";
        public const string SHARED_DATA_CUSTOM_DATA_5_KEY = "5";
        public const string SHARED_DATA_CUSTOM_DATA_6_KEY = "6";
        public const string SHARED_DATA_CUSTOM_DATA_7_KEY = "7";
        public const string SHARED_DATA_CUSTOM_DATA_8_KEY = "8";
        public const string SHARED_DATA_CUSTOM_DATA_9_KEY = "9";
        public const string SHARED_DATA_CUSTOM_DATA_W_KEY = "W";
        public const string SHARED_DATA_CUSTOM_DATA_X_KEY = "X";
        public const string SHARED_DATA_CUSTOM_DATA_Y_KEY = "Y";
        public const string SHARED_DATA_CUSTOM_DATA_Z_KEY = "Z";

        // shared data keys for data from location listener

        public const string SHARED_DATA_LOCATION_KEY = "location";
        public const string SHARED_DATA_ADDRESS_KEY = "address";

        // shared data keys for data from phone accelerometer and gyroscope

        public const string SHARED_DATA_ACCELX_KEY = "accelx";
        public const string SHARED_DATA_ACCELY_KEY = "accely";
        public const string SHARED_DATA_ACCELZ_KEY = "accelz";
        public const string SHARED_DATA_GYROX_KEY = "gyrox";
        public const string SHARED_DATA_GYROY_KEY = "gyroy";
        public const string SHARED_DATA_GYROZ_KEY = "gyroz";

        // shared data keys for calculated values

        public const string SHARED_DATA_VUPPER_KEY = "vupper";
        public const string SHARED_DATA_AS_KEY = "ampseconds";
        public const string SHARED_DATA_J_KEY = "joules";
        public const string SHARED_DATA_DISTANCE_KEY = "distance";
        public const string SHARED_DATA_LAP_KEY = "lap";
        public const string SHARED_DATA_EFFICIENCY_ASPM_KEY = "efficiency_as_per_m";
        public const string SHARED_DATA_EFFICIENCY_JPM_KEY = "efficiency_j_per_m";
        public const string SHARED_DATA_START_TIME_KEY = "start_time";
        public const string SHARED_DATA_ELAPSED_TIME_KEY = "elapsed_time";
        public const string SHARED_DATA_STATE_KEY = "state";

        public const string SHARED_DATA_LATEST_TELEMETRY_KEY = "latest_telemetry";
        public const string SHARED_DATA_BLUETOOTH_CONNECTED_KEY = "bluetooth_connected";
    }
}