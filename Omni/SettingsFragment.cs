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

namespace Omni
{
    public class SettingsFragment : PreferenceFragmentCompat, IFragmentServiceUpdates, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private readonly int[] maskedPreferenceIntKeys = [  Resource.String.preference_key_upload_banchory_greenpower_password,
                                                            Resource.String.preference_key_upload_echook_live_data_password,
                                                            Resource.String.preference_key_upload_user_defined_password ];
        private string[]? maskedPreferenceKeys;

        public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            // Set custom data store which encrypts all strings
            PreferenceManager?.PreferenceDataStore = EncryptedPreferenceHelper.DataStore;
            EncryptedPreferenceHelper.DataStore?.EncryptedPreferenceChanged += (s, e) => { OnSharedPreferenceChanged(e.SharedPreferences, e.Key); };

            // Build preferences screen from settings
            AddPreferencesFromResource(Resource.Xml.settings);

            ListPreference? bluetoothDeviceListPreference = (ListPreference?)FindPreference(Resources.GetString(Resource.String.preference_key_bluetooth_device));
            Preference? bluetoothNoPermissionPreference = (Preference?)FindPreference(Resources.GetString(Resource.String.preference_key_bluetooth_nopermission));
            Preference? bluetoothNoDevicesPreference = (Preference?)FindPreference(Resources.GetString(Resource.String.preference_key_bluetooth_nodevices));

            if (Context is not null && PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions))
            {
                var pd = BluetoothHelper.PairedDevices(Context);
                if (pd != null && pd.Count > 0)
                {
                    var pairedDevices = pd.ToArray();
                    bluetoothDeviceListPreference?.SetEntries(pairedDevices);
                    bluetoothDeviceListPreference?.SetEntryValues(pairedDevices);
                    bluetoothDeviceListPreference?.Visible = true;
                    bluetoothNoPermissionPreference?.Visible = false;
                    bluetoothNoDevicesPreference?.Visible = false;
                }
                else
                {
                    bluetoothDeviceListPreference?.Visible = false;
                    bluetoothNoPermissionPreference?.Visible = false;
                    bluetoothNoDevicesPreference?.Visible = true;
                }
            }
            else
            {
                bluetoothDeviceListPreference?.Visible = false;
                bluetoothNoPermissionPreference?.Visible = true;
                bluetoothNoDevicesPreference?.Visible = false;
            }

            // Find the keys for preferences which will have their input masked (i.e. passwords)
            List<string> maskedPrefKeys = [];
            foreach (var key in maskedPreferenceIntKeys)
            {
                var maskedPrefKey = GetString(key);
                if (maskedPrefKey != null) maskedPrefKeys.Add(maskedPrefKey);
            }
            maskedPreferenceKeys = [.. maskedPrefKeys];

            // Add on edit bind listeners to settings to mask the password values
            foreach (var key in maskedPreferenceKeys)
            {
                var maskedPref = (EditTextPreference?)FindPreference(key);
                maskedPref?.SetOnBindEditTextListener(new PasswordOnEditBindListener());
            }

            ISharedPreferences? sharedPreferences = null;
            if (Activity != null) sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Activity);
            if (sharedPreferences != null) OnSharedPreferenceChanged(sharedPreferences, Resources.GetString(Resource.String.preference_key_car_name));
        }

        public override void OnResume()
        {
            base.OnResume();
            PreferenceScreen?.SharedPreferences?.RegisterOnSharedPreferenceChangeListener(this);
        }

        public override void OnPause()
        {
            base.OnPause();
            PreferenceScreen?.SharedPreferences?.UnregisterOnSharedPreferenceChangeListener(this);
        }

        public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? changedPreference)
        {
            if (IsAdded && Context != null)
            {
                SetBluetoothPreferencesVisibility();
                SetUploadPreferencesVisibility();
            }

            if (sharedPreferences != null && sharedPreferences.All != null)
            {
                // Ignore the specific preference changed, update all edit text preference summaries with the preference value
                foreach ((string key, object _) in sharedPreferences.All)
                {
                    var preference = FindPreference(key);
                    if (preference is EditTextPreference)
                    {
                        var editTextPreference = preference as EditTextPreference;
                        if (maskedPreferenceKeys.Contains(key))
                        {
                            // update the summary with dots to mask password settings values
                            var dotText = string.Empty;
                            if (editTextPreference != null && editTextPreference.Text != null && editTextPreference.Text.Length > 0)
                            {
                                StringBuilder sb = new();
                                for (var i = 0; i < editTextPreference.Text.Length; i++) sb.Append("●");
                                dotText = sb.ToString();
                            }
                            preference.Summary = dotText;
                        }
                        else
                        {
                            // update the summary
                            preference.Summary = editTextPreference?.Text;
                        }
                    }
                }
            }

            if (sharedPreferences != null && !string.IsNullOrWhiteSpace(changedPreference) && changedPreference.Equals("upload_user_defined_url"))
            {
                var url = (FindPreference(changedPreference) as EditTextPreference)?.Text;                              // get url user defined upload url
                if (   !System.Uri.TryCreate(url, UriKind.Absolute, out System.Uri? validUri)                           // check if its valid
                    || !string.Equals(validUri.Scheme, System.Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))  // check if the scheme is https
                {                                                                                                       // if neither let user know its unacceptable
                    var activityMainView = Activity?.FindViewById(Resource.Id.activity_main);
                    if (activityMainView != null)
                    {
                        Snackbar.Make(activityMainView,
                                      Resources.GetString(Resource.String.snackbar_upload_https),
                                      Snackbar.LengthIndefinite)
                            .SetAction(Resources.GetString(Resource.String.snackbar_dismiss), (o) => { })
                            .Show();
                    }
                }
            }
        }

        private void SetBluetoothPreferencesVisibility()
        {
            var bluetoothPreferenceCategory = (PreferenceCategory?)FindPreference(Resources.GetString(Resource.String.preference_key_bluetooth_category));
            var bluetoothManager = Context?.GetSystemService(Context.BluetoothService) as BluetoothManager;
            var adapter = bluetoothManager?.Adapter;
            bluetoothPreferenceCategory?.Visible = (adapter != null);
        }

        private void SetUploadPreferencesVisibility()
        {
            var uploadListPreference = (ListPreference?)FindPreference(Resources.GetString(Resource.String.preference_key_upload));
            int optionsId = Resource.Array.preference_options_upload;
            uploadListPreference?.SetEntries(optionsId);
            uploadListPreference?.SetEntryValues(optionsId);

            var uploadTo = uploadListPreference?.Value ?? string.Empty;
            bool uploadToBanchoryGreenpower = (uploadTo == GetString(Resource.String.preference_option_upload_banchory_greenpower));
            bool uploadToEchookLiveData = (uploadTo == GetString(Resource.String.preference_option_upload_echook_live_data));
            bool uploadToUserDefined = (uploadTo == GetString(Resource.String.preference_option_upload_user_defined));

            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name)))?.Visible = uploadToBanchoryGreenpower;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_banchory_greenpower_password)))?.Visible = uploadToBanchoryGreenpower;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_echook_live_data_car_name)))?.Visible = uploadToEchookLiveData;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_echook_live_data_password)))?.Visible = uploadToEchookLiveData;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_car_name)))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_url)))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_username)))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_password)))?.Visible = uploadToUserDefined;

            ((SwitchPreferenceCompat?)FindPreference(GetString(Resource.String.preference_key_upload_IMU)))?.Visible = uploadToBanchoryGreenpower
                                                                                                                                            || uploadToEchookLiveData
                                                                                                                                            || uploadToUserDefined;
        }
    }

    public class PasswordOnEditBindListener : Java.Lang.Object, EditTextPreference.IOnBindEditTextListener
    {
        public void OnBindEditText(EditText editText)
        {
            editText.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPassword;
        }
    }

}