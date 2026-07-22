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
    public class SettingsFragment : PreferenceFragmentCompat, IFragmentServiceUpdates, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
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
            ISharedPreferences? sharedPreferences = null;
            if (Activity != null) sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this.Activity);
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

        public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
        {
            SetBluetoothPreferencesVisibility();
            SetUploadPreferencesVisibility();

            if (sharedPreferences != null && sharedPreferences.All != null)
            {
                // Ignore the specific preference changed, update all edit text preference summaries with the preference value
                foreach ((string k, object _) in sharedPreferences.All)
                {
                    var preference = FindPreference(k);
                    if (preference is EditTextPreference)
                    {
                        var editTextPreference = preference as EditTextPreference;
                        preference.Summary = editTextPreference?.Text;
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

            var uploadTo = string.Empty;
            if (Activity != null) uploadTo = PreferenceManager.GetDefaultSharedPreferences(Activity)?.GetString(Resources.GetString(Resource.String.preference_key_upload), string.Empty);
            bool uploadToBanchoryGreenpower = (uploadTo == GetString(Resource.String.preference_option_upload_banchory_greenpower));
            bool uploadToEchookLiveData     = (uploadTo == GetString(Resource.String.preference_option_upload_echook_live_data));
            bool uploadToUserDefined        = (uploadTo == GetString(Resource.String.preference_option_upload_user_defined));

            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name)))?.Visible = uploadToBanchoryGreenpower;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_banchory_greenpower_password)))?.Visible = uploadToBanchoryGreenpower;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_echook_live_data_car_name   )))?.Visible = uploadToEchookLiveData;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_echook_live_data_password   )))?.Visible = uploadToEchookLiveData;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_car_name       )))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_url            )))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_username       )))?.Visible = uploadToUserDefined;
            ((EditTextPreference?)FindPreference(GetString(Resource.String.preference_key_upload_user_defined_password       )))?.Visible = uploadToUserDefined;

            ((SwitchPreferenceCompat?)FindPreference(GetString(Resource.String.preference_key_upload_IMU                     )))?.Visible = uploadToBanchoryGreenpower
                                                                                                                                         || uploadToEchookLiveData
                                                                                                                                         || uploadToUserDefined;
        }
    }
}