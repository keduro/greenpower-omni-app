using Android.Graphics;
using Android.Text;
using System.Text;
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
    internal class ShareSettingsFragment : Fragment, IFragmentServiceUpdates
    {
        static readonly string TAG = typeof(ShareSettingsFragment).FullName ?? "ShareSettingsFragment";

        TextView? shareSettingsText;
        ImageView? QRcodeImageView;
        SurfaceView? scanSurfaceView;
        Button? shareQRcodeButton;
        Button? scanQRcodeButton;
        Button? cancelScanQRcodeButton;

        string scannedBarcode = string.Empty;
        static bool savedIsScanning = false;        // static to ensure saved across orientation changes
                                                    // that may result in OnCreateView being called twice

        public override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (savedInstanceState != null)
            {
                savedIsScanning = savedInstanceState.GetBoolean(Constants.SCANNING_QR_CODE, false);
            }
        }

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new System.NotImplementedException();
        }

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.share_settings_fragment, container, false);

            shareSettingsText = view?.FindViewById<TextView>(Resource.Id.share_settings_text);
            var plainText = GetString(Resource.String.share_settings_text);
            var styledText = Html.FromHtml(plainText, FromHtmlOptions.ModeLegacy);
            shareSettingsText?.SetText(styledText, TextView.BufferType.Normal);

            QRcodeImageView = view?.FindViewById<ImageView>(Resource.Id.QRcode_image);
            var bitmap = CreateQRCode();
            if (bitmap != null && QRcodeImageView != null) QRcodeImageView?.SetImageBitmap(bitmap);

            scanSurfaceView = view?.FindViewById<SurfaceView>(Resource.Id.scan_surfaceview);

            shareQRcodeButton = view?.FindViewById<Button>(Resource.Id.shareQRcode_button);
            shareQRcodeButton?.Click += (sender, e) =>
            {
                ShareQRcode(CreateQRCode());
            };

            scanQRcodeButton = view?.FindViewById<Button>(Resource.Id.scanQRcode_button);
            scanQRcodeButton?.Click += (sender, e) =>
            {
                StartScanningQRcode(scanSurfaceView);
            };

            cancelScanQRcodeButton = view?.FindViewById<Button>(Resource.Id.cancelScanQRcode_button);
            cancelScanQRcodeButton?.Click += (sender, e) =>
            {
                StopScanningQRcode();
            };

            UpdateUI(false);

            if (savedIsScanning)
            {
                StartScanningQRcode(scanSurfaceView);
            }

            return view;
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            savedIsScanning = QRcodeHelper.IsScanning;
            StopScanningQRcode();
            outState.PutBoolean(Constants.SCANNING_QR_CODE, savedIsScanning);
            base.OnSaveInstanceState(outState);
        }

        private void UpdateUI(bool isScanning)
        {
            if (!isScanning)
            {
                QRcodeImageView?.Visibility = ViewStates.Visible;
                scanSurfaceView?.Visibility = ViewStates.Gone;
                shareQRcodeButton?.Visibility = ViewStates.Visible;
                scanQRcodeButton?.Visibility = ViewStates.Visible;
                cancelScanQRcodeButton?.Visibility = ViewStates.Gone;
            }
            else
            {
                QRcodeImageView?.Visibility = ViewStates.Gone;
                scanSurfaceView?.Visibility = ViewStates.Visible;
                shareQRcodeButton?.Visibility = ViewStates.Gone;
                scanQRcodeButton?.Visibility = ViewStates.Gone;
                cancelScanQRcodeButton?.Visibility = ViewStates.Visible;
            }
        }

        private void StartScanningQRcode(SurfaceView? scanSurfaceView)
        {
            QRcodeHelper.StartScanQRcode(Context, scanSurfaceView, Looper.MyLooper(), (barCode) =>
            {
                CheckAndAcceptBarcode(barCode);
            }, UpdateUI);
        }

        private void StopScanningQRcode()
        {
            QRcodeHelper.StopScanQRcode(UpdateUI);
        }

        private Bitmap? CreateQRCode()
        {
            var carName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_car_name), string.Empty);
            var uploadTo = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload), string.Empty);

            var uploadToBanchoryGreenpower = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_banchory_greenpower));
            var uploadToEchookLiveData = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_echook_live_data));
            var uploadToUserDefined = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_user_defined));
            var telemetryCarName = string.Empty;
            var url = string.Empty;
            var username = string.Empty;
            var password = string.Empty;

            var preferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);

            if (uploadToBanchoryGreenpower)
            {
                url = Resources?.GetString(Resource.String.upload_banchory_greenpower_url);
                telemetryCarName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), string.Empty);
                username = telemetryCarName;
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_password), string.Empty);
            }
            else if (uploadToEchookLiveData)
            {
                url = Resources?.GetString(Resource.String.upload_echook_live_data_send_url);
                telemetryCarName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), string.Empty);
                username = telemetryCarName;
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_password), string.Empty);
            }
            else if (uploadToUserDefined)
            {
                url = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_url), string.Empty);
                telemetryCarName = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_car_name), string.Empty);
                username = EncryptedPreferenceHelper.DataStore?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_username), string.Empty);
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_password), string.Empty);
            }

            var sb = new StringBuilder();
            sb.AppendLine("[OMNI]");
            sb.AppendLine(carName);
            sb.AppendLine(uploadTo);
            sb.AppendLine(telemetryCarName);
            sb.AppendLine(url);
            sb.AppendLine(username);
            sb.AppendLine(password);
            sb.AppendLine("[OMNI]");

            return QRcodeHelper.CreateQRCode(sb.ToString());
        }

        private void CheckAndAcceptBarcode(string barCode)
        {
            var lines = barCode.Split(System.Environment.NewLine);
            if (lines.Length != 9 || !lines[0].Equals("[OMNI]") || !lines[7].Equals("[OMNI]")) return;

            if (barCode.Equals(scannedBarcode)) return;     // to avoid reprocessing the same barcode being 
            scannedBarcode = barCode;                       // repeatedly scanned

            var passwordMessage = (!string.IsNullOrWhiteSpace(lines[6]) ? "(Password is set)" : "(Password not set)");

            var sb = new StringBuilder();
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_title_car_name)}: {lines[1]}");
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_title_upload_target)}: {lines[2]}");
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_dialog_upload_user_defined_car_name)}: {lines[3]}");
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_dialog_upload_user_defined_url)}: {lines[4]}");
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_dialog_upload_user_defined_username)}: {lines[5]}");
            sb.AppendLine($"{Resources.GetString(Resource.String.preference_dialog_upload_user_defined_password)}: {passwordMessage}");

            if (Context != null)
            {
                var alert = new AlertDialog.Builder(Context);
                alert.SetTitle(Resource.String.share_settings_alert)?
                    .SetMessage(sb.ToString())?
                    .SetPositiveButton(Resource.String.dialog_ok, (sender, args) => { UpdatePreferences(lines); })
                    .SetNegativeButton(Resource.String.dialog_cancel, (sender, args) => { scannedBarcode = string.Empty; });
                alert.Create().Show();
            }

        }

        private void UpdatePreferences(string[] barCodeLines)
        {
            StopScanningQRcode();

            if (barCodeLines.Length >= 8)
            {
                var uploadTo = barCodeLines[2];
                EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload), uploadTo);

                var uploadToBanchoryGreenpower = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_banchory_greenpower));
                var uploadToEchookLiveData = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_echook_live_data));
                var uploadToUserDefined = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_user_defined));

                string? password = string.Empty;
                string? encryptedPassword = barCodeLines[6]; 
                if (!string.IsNullOrEmpty(encryptedPassword))
                {
                    try
                    {
                        password = EncryptionHelper.DecryptString(encryptedPassword);
                    }
                    catch (Exception ex)       // If fail to decode password, simply leave as blank
                    {
                        Log.Info(TAG, $"Exception decrypting password from QR code, setting password to blank: {ex}");
                    }
                }

                EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_car_name), barCodeLines[1]);

                if (uploadToBanchoryGreenpower)
                {
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), barCodeLines[3]);
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_password), password);
                }
                else if (uploadToEchookLiveData)
                {
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), barCodeLines[3]);
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_password), password);
                }
                else if (uploadToUserDefined)
                {
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_car_name), barCodeLines[3]);
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_url), barCodeLines[4]);
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_username), barCodeLines[5]);
                    EncryptedPreferenceHelper.DataStore?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_password), password);
                }

                var bitmap = CreateQRCode();
                if (bitmap != null && QRcodeImageView != null) QRcodeImageView?.SetImageBitmap(bitmap);
            }
        }
    
        private void ShareQRcode(Bitmap? bitmap)
        {
            if (this.Context == null || bitmap == null) return;

            var qrFileName = "Omni-QRcode-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
            var exportPath = new Java.IO.File(Context.GetExternalFilesDir(null), "QRCodes");
            if (!Directory.Exists(exportPath.AbsolutePath)) Directory.CreateDirectory(exportPath.AbsolutePath);
            var fileExportPath = new Java.IO.File(exportPath, qrFileName);
            var qrCodePath = fileExportPath.AbsolutePath;
            Shared.QRcodePath = qrCodePath;

            try
            {
                var stream = new FileStream(Shared.QRcodePath, FileMode.Create, FileAccess.Write);
                var format = Bitmap.CompressFormat.Png;
                if (format == null) return;
                bitmap.Compress(format, 90, stream);
                stream.Flush();
                stream.Close();

                Uri? fileUri = FileProvider.GetUriForFile(Context, Constants.FILE_PROVIDER_AUTHORITY, new Java.IO.File(Shared.QRcodePath));
                if (fileUri != null)
                {
                    var alert = new AlertDialog.Builder(this.Context);
                    alert.SetTitle(Resources?.GetString(Resource.String.dialog_share_qrcode))?
                    .SetMessage(Shared.QRcodePath)?
                    .SetPositiveButton(GetString(Resource.String.dialog_share), (sender, args) =>
                    {
                        var shareIntent = new Intent(Intent.ActionSend);
                        shareIntent.SetType("image/png");
                        shareIntent.PutExtra(Intent.ExtraStream, fileUri);
                        shareIntent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
                        var intent = Intent.CreateChooser(shareIntent, Resources?.GetString(Resource.String.dialog_share_qrcode));
                        if (intent != null) StartActivity(intent);
                    })?
                    .SetNegativeButton(GetString(Resource.String.dialog_cancel), (sender, args) => { });
                    alert.Create().Show();
                }
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"Exception sharing QR code bitmap: {ex}");
            }
        }
    }
}
