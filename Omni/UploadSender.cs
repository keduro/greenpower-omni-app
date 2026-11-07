
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

using Java.Lang;
using Org.Json;
using System.Net;
using System.Text;
using Exception = Java.Lang.Exception;
using Math = System.Math;

namespace Omni
{
    class UploadSender 
    {
        static readonly string TAG = typeof(UploadSender).FullName ?? "UploadSender";

        public bool IsStarted { get; private set; } = false;   

        readonly MainService Service;                   // service from which sender instantiated
        readonly Timer timer;                           // timer on which sender runs
        readonly SemaphoreSlim uploadGate = new(1, 1);  // semaphore to ensure only one upload request in flight at any one time

        readonly ServiceUpdater serviceUpdater;

        HttpClient? httpClient = null;                  // use a single http client (and thus a single connection) rather than a new client
                                                        // for each time data is uploaded
        bool uploadToNone;
        bool uploadToBanchoryGreenpower;
        bool uploadToEchookLiveData;
        bool uploadToUserDefined;
        bool? uploadIMUdata;

        string? url;
        string? carName;
        string? username;
        string? password;

        string? eChookLiveID;                                   // obtained from echook live data in return from username and password
        const int eChookLiveIDMinBackoff = 2;                   // minimum number of seconds before retyring to get an eChookLiveID
        const int eChookLiveIDMaxBackoff = 60;                  // maximum number of seconds before retyring to get an eChookLiveID
        int eChookLiveIDBackoff = eChookLiveIDMinBackoff;       // number of seconds before retrying to get an eChookLiveID 
        DateTime lastGeteChookLiveIDTime = DateTime.MinValue;   // time of last attempt to get eChookLiveID

        public UploadSender(MainService Service, ServiceUpdater ServiceUpdater)
        {
            this.Service = Service;
            timer = new Timer { Enabled = false };
            timer.Elapsed += TimerElapsed;

            serviceUpdater = ServiceUpdater;
        }

        public void Start()
        {
            Log.Info(TAG, "Upload sender start.");
            IsStarted = true;

            // set up url, car name, username, and password depending on where uploading to

            var Resources = Service.Resources;
            var uploadTo = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload), string.Empty);
            uploadToNone = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_none)) || string.IsNullOrEmpty(uploadTo);
            uploadToBanchoryGreenpower = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_banchory_greenpower));
            uploadToEchookLiveData = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_echook_live_data));
            uploadToUserDefined = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_user_defined));

            // check if uploading acceleromoter and gyro data

            uploadIMUdata = EncryptedPreferenceHelper.DataStore?.GetBoolean(Resources?.GetString(Resource.String.preference_key_upload_IMU), false);

            url = null;
            carName      = null;
            username     = null;
            password     = null;
            eChookLiveID = null;

            if (uploadToBanchoryGreenpower)
            {
                url = Resources?.GetString(Resource.String.upload_banchory_greenpower_url);

                //url = "https://192.168.1.6:45456/api/telemetrydata";   // LOCAL TESTING. N.B. device needs Conveyor certificate, see https://conveyor.cloud/Help/SSL
                //url = "http://data.banchory-greenpower.org/api/telemetrydata";

                carName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), string.Empty);
                username = carName;
                password = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_password), string.Empty);
            }
            else if (uploadToEchookLiveData)
            {
                url = Resources?.GetString(Resource.String.upload_echook_live_data_send_url);
                carName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), "");
                username = carName;
                password = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_password), "");
                eChookLiveID = null;             // will be obtained when uploading telemetry data
            }
            else if (uploadToUserDefined)
            {
                url = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_url), string.Empty);
                carName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_car_name), string.Empty);
                username = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_username), string.Empty);
                password = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_password), string.Empty);
            }

            httpClient ??= new HttpClient();

            // set the timer going

            timer.Interval = Constants.UPLOAD_INTERVAL;
            timer.Start();
        }

        public void Stop()
        {
            Log.Info(TAG, "Upload sender stop.");
            timer.Stop();

            IsStarted = false;

            uploadGate.Wait();

            try
            {
                httpClient?.Dispose();
            }
            finally
            {
                httpClient = null;
                uploadGate.Release();
            }
        }

        public async Task UploadNowAsync()
        {
            if (!uploadToNone) await UploadTelemetryDataAsync();
        }

        private async void TimerElapsed(object? sender, ElapsedEventArgs? e)
        {
            if (!uploadToNone) await UploadTelemetryDataAsync();
        }

        private async Task<string?> GetEChookLiveIDAsync(string url, string? carName, string? password)
        {
            if (!System.Uri.TryCreate(url, UriKind.Absolute, out System.Uri? validUri))                             // check have a valid url
            {
                Log.Info(TAG, $"Get eChook Live ID has invalid url '{url}'");
                return null;
            }
            if (!string.Equals(validUri.Scheme, System.Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))     // check if url starts with https://
            {
                Log.Info(TAG, $"Get eChook Live ID url does not start with https:// '{url}'");
                return null;
            }

            var data = new JSONObject();
            data.Put("username", carName);
            data.Put("password", password);

            if (httpClient == null) return null;

            var (statuscode, result) = await PostDataAsync(httpClient, validUri, null, null, data);

            if (statuscode == HttpStatusCode.OK && !string.IsNullOrEmpty(result))
            {
                                                    // catch non JSON e.g. "car not found" or "incorrect password", 
                if (!result.StartsWith('{'))        // as JSONTokener below does not raise exception for invalid JSON!!!      
                {
#if DEBUG
                    Log.Info(TAG, $"Get eChook Live ID returned '{result}'");
#endif
                    return null;
                }
                try
                {
                    var obj = (JSONObject?)new JSONTokener(result).NextValue();
                    var id = obj?.GetString("id");
                    if (!string.IsNullOrEmpty(id)) return id;
                }
                catch (Exception ex)
                {
                    Log.Info(TAG, "Get eChook Live ID exception: " + ex.Message ?? "");
                }
            }

            return null;
        }

        private async Task UploadTelemetryDataAsync()
        {
            if (Service.Resources == null) return;

            if (!await uploadGate.WaitAsync(0)) return;     // try to set semaphore to proceed with upload request
                                                            // if already set, previous upload request not complete, so return
            try
            {
                var baseUrl = url;
                if (string.IsNullOrEmpty(carName)) return;
                if (uploadToBanchoryGreenpower && string.IsNullOrEmpty(password)) return;
                if (uploadToEchookLiveData && !string.IsNullOrEmpty(carName) && !string.IsNullOrEmpty(password) && string.IsNullOrEmpty(eChookLiveID))
                {
                    // if have car name and password for eChook live, but don't have an eChook live ID, then query for the live ID
                    // rather than continue to query every time that upload telemetry data async is called (every 1.5s)
                    // implemenent an exponential backoff to reduce number of requests to eChook live server

                    var now = DateTime.UtcNow;
                    if (now.Subtract(lastGeteChookLiveIDTime).TotalSeconds < eChookLiveIDBackoff) return;

                    lastGeteChookLiveIDTime = now;
                    eChookLiveID = await GetEChookLiveIDAsync(Service.Resources.GetString(Resource.String.upload_echook_live_data_getid_url), username, password);
                    if (string.IsNullOrEmpty(eChookLiveID))
                    {
                        eChookLiveIDBackoff *= 2;
                        eChookLiveIDBackoff = Math.Min(eChookLiveIDBackoff, eChookLiveIDMaxBackoff);
                        return;
                    }
                    else
                    {
                        eChookLiveIDBackoff = eChookLiveIDMinBackoff;
                    }
                }
                if (uploadToEchookLiveData)
                {
                    if (string.IsNullOrEmpty(eChookLiveID)) return;
                    baseUrl += eChookLiveID;
                }
                if (!System.Uri.TryCreate(baseUrl, UriKind.Absolute, out System.Uri? validUri))                         // check have a valid url
                {
                    Log.Info(TAG, $"Upload telemetry data has invalid url '{url}'");
                    return;
                }
                if (!string.Equals(validUri.Scheme, System.Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))     // check if url starts with https://
                {
                    Log.Info(TAG, $"Upload telemetry data url does not start with https:// '{url}'");
                    return;
                }

                var data = new JSONObject();

                data.Put("Timestamp", JavaSystem.CurrentTimeMillis());

                if (uploadToBanchoryGreenpower || uploadToUserDefined)
                {
                    data.Put("LatestTelemetry", Shared.Data.GetLong(Constants.SHARED_DATA_LATEST_TELEMETRY_KEY, 0));
                    data.Put("BluetoothConnected", Shared.Data.GetInt(Constants.SHARED_DATA_BLUETOOTH_CONNECTED_KEY, 0));
                    data.Put("CarName", carName);
                    data.Put("Type", 0);
                    data.Put("Source", "omni");
                    data.Put("RaceSeconds", Shared.Data.GetInt(Constants.SHARED_DATA_ELAPSED_TIME_KEY));
                }

                data.Put("Vt", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_VTOTAL_KEY, 0), 2));
                data.Put("V1", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_VLOWER_KEY, 0), 2));
                data.Put("A", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CURRENT_KEY, 0), 2));
                data.Put("RPM", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_RPM_KEY, 0), 2));
                data.Put("Spd", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_SPEED_KEY, 0), 2));

                if (uploadToBanchoryGreenpower || uploadToUserDefined)
                {
                    data.Put("InputThrtl", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_INPUT_THROTTLE_KEY, 0), 2));
                    data.Put("ActualThrtl", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_ACTUAL_THROTTLE_KEY, 0), 2));
                    data.Put("J", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_J_KEY, 0), 2));
                }
                else
                {
                    data.Put("Thrtl", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_INPUT_THROTTLE_KEY, 0), 2));
                }

                data.Put("AH", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_AS_KEY, 0) / 3600, 2));
                data.Put("Lap", Shared.Data.GetInt(Constants.SHARED_DATA_LAP_KEY, 0));
                data.Put("Tmp1", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_TEMPERATURE_1_KEY, 0), 2));
                data.Put("Tmp2", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_TEMPERATURE_2_KEY, 0), 2));
                data.Put("Brk", Shared.Data.GetInt(Constants.SHARED_DATA_BRAKE_PRESSED_KEY, 0));

                if (uploadToBanchoryGreenpower || uploadToUserDefined)
                    data.Put("Gear", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_3_KEY, 0), 2));     // special case, upload actual gear selected rather than ratio
                else
                    data.Put("Gear", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_GEAR_RATIO_KEY, 0), 2));

                data.Put("Distance", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_DISTANCE_KEY, 0), 2));

                if (uploadToBanchoryGreenpower || uploadToUserDefined)
                    data.Put("Efficiency", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_EFFICIENCY_JPM_KEY, 0), 2));

                data.Put("Custom0", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_0_KEY, 0), 3));
                data.Put("Custom1", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_1_KEY, 0), 3));
                data.Put("Custom2", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_2_KEY, 0), 3));
                data.Put("Custom3", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_3_KEY, 0), 3));
                data.Put("Custom4", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_4_KEY, 0), 3));
                data.Put("Custom5", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_5_KEY, 0), 3));
                data.Put("Custom6", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_6_KEY, 0), 3));
                data.Put("Custom7", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_7_KEY, 0), 3));
                data.Put("Custom8", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_8_KEY, 0), 3));
                data.Put("Custom9", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_9_KEY, 0), 3));

                if (uploadToBanchoryGreenpower || uploadToUserDefined)
                {
                    data.Put("CustomW", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_W_KEY, 0), 3));
                    data.Put("CustomX", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_X_KEY, 0), 3));
                    data.Put("CustomY", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_Y_KEY, 0), 3));
                    data.Put("CustomZ", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_CUSTOM_DATA_Z_KEY, 0), 3));
                }

                if (uploadToBanchoryGreenpower || uploadToEchookLiveData || uploadToUserDefined)
                {
                    var location = LocationHelper.GetLocationFromBundle(Shared.Data, Constants.SHARED_DATA_LOCATION_KEY);
                    if (location != null)
                    {
                        data.Put("Lat", location.Latitude);
                        data.Put("Lon", location.Longitude);
                        if (!uploadToEchookLiveData)
                            data.Put("GPSSpd", location.HasSpeed ? location.Speed : 0);
                        data.Put("Alt", location.Altitude);
                    }
                    else
                    {
                        data.Put("Lat", 0.0);
                        data.Put("Lon", 0.0);
                        if (!uploadToEchookLiveData)
                            data.Put("GPSSpd", 0.0);
                        data.Put("Alt", 0.0);
                    }

                    if (uploadIMUdata ?? false)
                    {
                        data.Put("AccelerationX", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELX_KEY, 0), 3));
                        data.Put("AccelerationY", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELY_KEY, 0), 3));
                        data.Put("AccelerationZ", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_ACCELZ_KEY, 0), 3));
                        data.Put("GyroscopeX", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_GYROX_KEY, 0), 3));
                        data.Put("GyroscopeY", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_GYROY_KEY, 0), 3));
                        data.Put("GyroscopeZ", Math.Round(Shared.Data.GetDouble(Constants.SHARED_DATA_GYROZ_KEY, 0), 3));
                    }
                }

                if (httpClient != null)
                {
                    try
                    {
                        HttpStatusCode statuscode;
                        //string result;

                        if (uploadToEchookLiveData)
                        {
                            (statuscode, _ /*result*/ ) = await PostDataAsync(httpClient, validUri, null, null, null, null, data);
                        }
                        else
                        {
                            (statuscode, _ /*result*/ ) = await PostDataAsync(httpClient, validUri, username, password, data);
                        }

                        // check for a positive response from the server, and let the main service know to update the main activity
                        if (statuscode == HttpStatusCode.OK || statuscode == HttpStatusCode.Created)
                        {
                            Intent intent = new(Constants.UPLOAD_DATA_BROADCAST_ACTION);
                            serviceUpdater.Post(intent);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info(TAG, "Upload telemetry data exception: " + ex.Message ?? "");
                    }
                }
            }
            finally
            {
                uploadGate.Release();                       // release semaphore once upload request is complete
                                                            // ready for next upload request
            }
        }

        private async static Task<(HttpStatusCode StatusCode, string? Result)> PostDataAsync(HttpClient httpClient, System.Uri? uri, string? username, string? password, JSONObject data)
        {
            return await PostDataAsync(httpClient, uri, username, password, null, null, data);
        }

        private async static Task<(HttpStatusCode StatusCode, string? Result)> PostDataAsync(HttpClient httpClient, System.Uri? uri, string? username, string? password, string? headerKey, string? headerValue, JSONObject data)
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    httpRequestMessage.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($"{username}:{password}")));
                }
                if (!string.IsNullOrEmpty(headerKey) && !string.IsNullOrEmpty(headerValue))
                {
                    httpRequestMessage.Headers.Add(headerKey, headerValue);
                }
                var response = await httpClient.SendAsync(httpRequestMessage) ?? throw new System.Exception("Null response from HttpClient.PostAsync");
                if (response.StatusCode == HttpStatusCode.NoContent) return (response.StatusCode, null);
                var result = await response.Content.ReadAsStringAsync();
#if DEBUG
                Log.Info(TAG, "HttpClient Http POST response status: " + response.StatusCode.ToString() + ", result: " + result);
#else
                Log.Info(TAG, "HttpClient Http POST response status: " + response.StatusCode.ToString());
#endif
                return (response.StatusCode, result);
            }
            catch (System.Exception ex)
            {
                Log.Info(TAG, "HttpClient Http POST exception: " + ex.Message ?? "");
                return (HttpStatusCode.BadRequest, null);       // didn't really get a bad request from server, but need to return something :-).
            }
        }

    }
}