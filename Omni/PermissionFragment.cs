using Android.Text;
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
    public class PermissionFragment : Fragment, IFragmentServiceUpdates
    {
        static readonly string TAG = typeof(DashboardFragment).FullName ?? "PermissionFragment";

        TextView? permissionsText;
        TextView? allowText;
        Button? allowButton;
        Button? continueButton;

        string lastPermissionRequested = string.Empty;

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new NotImplementedException();
        }

        public override View? OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater?.Inflate(Resource.Layout.permission_fragment, container, false);

            permissionsText = view?.FindViewById<TextView>(Resource.Id.permissions_text);
            allowText = view?.FindViewById<TextView>(Resource.Id.allow_text);
            allowButton = view?.FindViewById<Button>(Resource.Id.allow_button);
            continueButton = view?.FindViewById<Button>(Resource.Id.permissions_continue_button);

            var plainText = GetString(Resource.String.permissions_text);
            var styledText = Html.FromHtml(plainText, FromHtmlOptions.ModeLegacy);
            permissionsText?.SetText(styledText, TextView.BufferType.Normal);
            allowButton?.Click += AllowButton_Click;
            continueButton?.Click += ContinueButton_Click;

            UpdateUI();

            return view;
        }

        private static bool GotAllPermissions()
        {
            var gotLocationPermission = PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions);
            var gotBluetoothPermission = PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions);
            var gotNotificationsPermission = PermissionsHelper.CheckPermissions(PermissionsHelper.NotificationPermissions);

            return gotLocationPermission && gotBluetoothPermission && gotNotificationsPermission;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private bool ConfirmPermission(string permission)
        {
            if (lastPermissionRequested == permission)          // if permission to be confirmed is the last requested then the permission had been denied
            {
                var permissionText = string.Empty;
                switch (permission)
                {
                    case Manifest.Permission.AccessFineLocation: 
                        permissionText = GetString(Resource.String.permissions_allow_fine_location);
                        break;
                    case Manifest.Permission.AccessBackgroundLocation:
                        permissionText = GetString(Resource.String.permissions_allow_background_location);
                        break;
                    case Manifest.Permission.BluetoothConnect:
                        permissionText = GetString(Resource.String.permissions_allow_bluetooth);
                        break;
                    case Manifest.Permission.PostNotifications:
                        permissionText = GetString(Resource.String.permissions_allow_notifications);
                        break;
                }
                var requireRationale = PermissionsHelper.CheckPermissionRationaleRequired(permission);
                if (requireRationale)
                {
                    permissionText =   GetString(Resource.String.permissions_permission_to) + permissionText 
                                     + GetString(Resource.String.permissions_rationale_1);
                }
                else
                {
                    permissionText =   GetString(Resource.String.permissions_permission_to) + permissionText
                                     + GetString(Resource.String.permissions_rationale_2);
                }
                if (Activity != null)
                {
                    var alert = new AlertDialog.Builder(Activity);
                    alert.SetTitle(GetString(Resource.String.permissions_premission_required))?
                        .SetMessage(permissionText)?
                        .SetPositiveButton(Resource.String.dialog_ok, (sender, args) => { });
                    alert.Create().Show();
                }
                return false;
            }
            lastPermissionRequested = permission;
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void RequestAllPermissions()
        {
            if (!PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions))
            {
                var denied = PermissionsHelper.DeniedPermissions(PermissionsHelper.LocationPermissions);
                if (denied != null)
                {
                    if (denied.Contains(Manifest.Permission.AccessFineLocation))
                    {
                        if (!ConfirmPermission(Manifest.Permission.AccessFineLocation)) return;
                        PermissionsHelper.RequestPermission(Manifest.Permission.AccessFineLocation, RequestPermissionCallback);
                    }
                    else if (denied.Contains(Manifest.Permission.AccessBackgroundLocation))
                    {
                        if (!ConfirmPermission(Manifest.Permission.AccessBackgroundLocation)) return;
                        if (Activity != null)
                        {
                            var alert = new AlertDialog.Builder(Activity);
                            if (Activity != null)
                            {
                                alert.SetTitle(Resource.String.permissions_location_alert_title)?
                                    .SetMessage(Resource.String.permissions_location_alert_message)?
                                    .SetPositiveButton(Resource.String.dialog_ok,
                                                       (sender, args) =>
                                                           PermissionsHelper.RequestPermission(Manifest.Permission.AccessBackgroundLocation, RequestPermissionCallback));
                                alert.Create().Show();
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Not expecting to have to grant permissions for: {string.Join(", ", denied)}.");
                    }
                }
            }
            else if (!PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions))
            {
                var denied = PermissionsHelper.DeniedPermissions(PermissionsHelper.BluetoothPermissions);
                if (denied != null)
                {
                    if (denied.Contains(Manifest.Permission.BluetoothConnect))
                    {
                        if (!ConfirmPermission(Manifest.Permission.BluetoothConnect)) return;
                        PermissionsHelper.RequestPermission(Manifest.Permission.BluetoothConnect, RequestPermissionCallback);
                    }
                    else
                    {
                        throw new Exception($"Not expecting to have to grant permissions for: {string.Join(", ", denied)}.");
                    }
                }
            }
            else if (!PermissionsHelper.CheckPermissions(PermissionsHelper.NotificationPermissions))
            {
                var denied = PermissionsHelper.DeniedPermissions(PermissionsHelper.NotificationPermissions);
                if (denied != null)
                {
                    if (denied.Contains(Manifest.Permission.PostNotifications))
                    {
                        if (!ConfirmPermission(Manifest.Permission.PostNotifications)) return;
                        PermissionsHelper.RequestPermission(Manifest.Permission.PostNotifications, RequestPermissionCallback);
                    }
                    else
                    {
                        throw new Exception($"Not expecting to have to grant permissions for: {string.Join(", ", denied)}.");
                    }
                }
            }
        }

        private void AllowButton_Click(object? sender, EventArgs e)
        {
            lastPermissionRequested = string.Empty;

            RequestAllPermissions();
        }

        private void ContinueButton_Click(object? sender, EventArgs e)
        {
            Activity?.SupportFragmentManager.SetFragmentResult(Constants.PERMISSIONS_KEY, new Bundle());
        }

        private void RequestPermissionCallback()
        {
            Log.Info(TAG, "RequestPermissionCallback.");

            RequestAllPermissions();

            UpdateUI();

            if (GotAllPermissions() && Activity != null && Activity.GetType() == typeof(MainActivity)) ((MainActivity)Activity).StartService();
        }

        private void UpdateUI()
        {
            var gotAllPermissions = GotAllPermissions();

            allowText?.Visibility = gotAllPermissions ? ViewStates.Visible : ViewStates.Gone;
            allowButton?.Visibility = gotAllPermissions ? ViewStates.Gone : ViewStates.Visible;
        }

    }
}
