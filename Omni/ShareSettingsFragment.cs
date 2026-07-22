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
        TextView? shareSettingsText;
        ImageView? QRcodeImageView;
        SurfaceView? scanSurfaceView;
        Button? shareQRcodeButton;
        Button? scanQRcodeButton;
        Button? cancelScanQRcodeButton;

        string scannedBarcode = string.Empty;
        bool savedIsScanning = false;

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
                StartScanningQRcode(Context, scanSurfaceView, Looper.MyLooper());
                UpdateUI(QRcodeHelper.IsScanning);
            };

            cancelScanQRcodeButton = view?.FindViewById<Button>(Resource.Id.cancelScanQRcode_button);
            cancelScanQRcodeButton?.Click += (sender, e) =>
            {
                StopScanningQRcode();
                UpdateUI(QRcodeHelper.IsScanning);
            };

            if (savedIsScanning)
            {
                StartScanningQRcode(Context, scanSurfaceView, Looper.MyLooper());
            }

            UpdateUI(QRcodeHelper.IsScanning);

            return view;
        }

        public override void OnPause()
        {
            base.OnPause();
            savedIsScanning = QRcodeHelper.IsScanning;
            QRcodeHelper.StopScanQRcode();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean(Constants.SCANNING_QR_CODE, savedIsScanning);
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

        private void StartScanningQRcode(Context? context, SurfaceView? scanSurfaceView, Looper? looper)
        {
            QRcodeHelper.StartScanQRcode(context, scanSurfaceView, looper, (barCode) =>
            {
                CheckAndAcceptBarcode(barCode);
            });
        }

        private void StopScanningQRcode()
        {
            QRcodeHelper.StopScanQRcode();

            UpdateUI(QRcodeHelper.IsScanning);
        }

        private Bitmap? CreateQRCode()
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            var carName = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_car_name), string.Empty);
            var uploadTo = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload), string.Empty);

            var uploadToBanchoryGreenpower = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_banchory_greenpower));
            var uploadToEchookLiveData = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_echook_live_data));
            var uploadToUserDefined = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_user_defined));
            var telemetryCarName = string.Empty;
            var url = string.Empty;
            var username = string.Empty;
            var password = string.Empty;

            if (uploadToBanchoryGreenpower)
            {
                url = Resources?.GetString(Resource.String.upload_banchory_greenpower_url);
                telemetryCarName = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), string.Empty);
                username = telemetryCarName;
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_password), string.Empty);
            }
            else if (uploadToEchookLiveData)
            {
                url = Resources?.GetString(Resource.String.upload_echook_live_data_send_url);
                telemetryCarName = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), string.Empty);
                username = telemetryCarName;
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_password), string.Empty);
            }
            else if (uploadToUserDefined)
            {
                url = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_url), string.Empty);
                telemetryCarName = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_car_name), string.Empty);
                username = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_username), string.Empty);
                password = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_password), string.Empty);
            }

            if (!string.IsNullOrEmpty(password))
                password = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

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

            var password = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(lines[6]));

            var sb = new StringBuilder();
            sb.AppendLine($"{Resource.String.preference_title_car_name}: {lines[1]}");
            sb.AppendLine($"{Resource.String.preference_title_upload_target}: {lines[2]}");
            sb.AppendLine($"{Resource.String.preference_dialog_upload_user_defined_car_name}: {lines[3]}");
            sb.AppendLine($"{Resource.String.preference_dialog_upload_user_defined_url}: {lines[4]}");
            sb.AppendLine($"{Resource.String.preference_dialog_upload_user_defined_username}: {lines[5]}");
            sb.AppendLine($"{Resource.String.preference_dialog_upload_user_defined_password}: {password}");

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
            var preferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            var editor = preferences?.Edit();

            StopScanningQRcode();

            if (editor != null && barCodeLines.Length >= 8)
            {
                var uploadTo = preferences?.GetString(Resources?.GetString(Resource.String.preference_key_upload), string.Empty);

                var uploadToBanchoryGreenpower = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_banchory_greenpower));
                var uploadToEchookLiveData = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_echook_live_data));
                var uploadToUserDefined = (uploadTo == Resources?.GetString(Resource.String.preference_option_upload_user_defined));

                string password = string.Empty;
                if (!string.IsNullOrEmpty(barCodeLines[6]))
                    password = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(barCodeLines[6]));

                editor?.PutString(Resources?.GetString(Resource.String.preference_key_car_name), barCodeLines[1]);
                editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload), barCodeLines[2]);

                if (uploadToBanchoryGreenpower)
                {
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), barCodeLines[3]);
                    editor?.PutString(Resources?.GetString(Resource.String.upload_banchory_greenpower_url), barCodeLines[4]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_car_name), barCodeLines[5]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_banchory_greenpower_password), password);
                }
                else if (uploadToEchookLiveData)
                {
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), barCodeLines[3]);
                    editor?.PutString(Resources?.GetString(Resource.String.upload_echook_live_data_send_url), barCodeLines[4]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_car_name), barCodeLines[5]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_echook_live_data_password), password);
                }
                else if (uploadToUserDefined)
                {
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_car_name), barCodeLines[3]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_url), barCodeLines[4]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_username), barCodeLines[5]);
                    editor?.PutString(Resources?.GetString(Resource.String.preference_key_upload_user_defined_password), password);
                }

                var committed = editor?.Commit() ?? false;
                if (committed)
                {
                    // refresh the displayed QR code
                    
                    var bitmap = CreateQRCode();
                    if (bitmap != null && QRcodeImageView != null) QRcodeImageView?.SetImageBitmap(bitmap);

                }
            }
        }
    
        private void ShareQRcode(Bitmap? bitmap)
        {
            if (this.Context == null || bitmap == null) return;

            if (Shared.QRcodePath == null)
            {
                var folder = Activity?.GetExternalFilesDir(null)?.AbsolutePath;
                folder ??= string.Empty;
                var fileName = "Omni-QRcode-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
                var qrCodePath = System.IO.Path.Combine(folder, fileName);
                Shared.QRcodePath = qrCodePath;
            }

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
            catch (Exception)
            {

            }
        }
    }
}
