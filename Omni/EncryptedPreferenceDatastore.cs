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
    public class EncryptedPreferenceDatastore(ISharedPreferences? sharedPreferences) : PreferenceDataStore
    {
        static readonly string TAG = typeof(EncryptedPreferenceDatastore).FullName ?? "EncryptedPreferenceDatastore";

        private readonly ISharedPreferences? sharedPreferences = sharedPreferences;

        public event EventHandler<EncryptedPreferenceChangedEventArgs>? EncryptedPreferenceChanged;

        protected virtual void OnEncryptedPreferenceChanged(EncryptedPreferenceChangedEventArgs e)
        {
            EncryptedPreferenceChanged?.Invoke(this, e);
        }

        public override void PutString(string? key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (sharedPreferences == null) throw new Exception("EncryptedPreferenceDatastore SharedPreferences data store not defined");

            var encryptedValue = EncryptionHelper.EncryptString(value);

            var editor = sharedPreferences?.Edit();
            editor?.PutString(key, encryptedValue);
            _ = editor?.Commit();

            OnEncryptedPreferenceChanged(new EncryptedPreferenceChangedEventArgs { SharedPreferences = sharedPreferences, Key = key });
        }

        public override string? GetString(string? key, string? defaultValue)
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;

            var encryptedValue = sharedPreferences?.GetString(key, null);           // check if encrpyted value is in shared preferences
            if (encryptedValue == null)             
            {
                if (string.IsNullOrWhiteSpace(defaultValue)) return defaultValue;
                this.PutString(key, defaultValue);                                  // encrypt default value and add to shared preferences
                encryptedValue = (sharedPreferences?.GetString(key, null)) ?? throw new Exception("EncryptedPreferenceDatastore Failed to store encrypted value");
            }

            try
            {
                var value = EncryptionHelper.DecryptString(encryptedValue);
                return value;
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"Exception decrypting value for key {key}, returning default value {defaultValue}: {ex}");
                return defaultValue;
            }

        }

        public override void PutBoolean(string? key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (sharedPreferences == null) throw new Exception("SharedPreferences data store not defined");
            var editor = sharedPreferences.Edit();
            editor?.PutBoolean(key, value);
            _ = editor?.Commit();
            OnEncryptedPreferenceChanged(new EncryptedPreferenceChangedEventArgs { SharedPreferences = sharedPreferences, Key = key });
        }
        public override bool GetBoolean(string? key, bool defValue)
        {
            if (string.IsNullOrWhiteSpace(key)) return defValue;
            if (sharedPreferences == null) throw new Exception("SharedPreferences data store not defined");
            return sharedPreferences.GetBoolean(key, defValue);
        }
    }

    public class EncryptedPreferenceChangedEventArgs : EventArgs
    {
        public ISharedPreferences? SharedPreferences { get; set; }
        public string? Key { get; set; }
    }

}
