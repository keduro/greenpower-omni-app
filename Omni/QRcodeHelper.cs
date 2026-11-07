using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using AndroidX.Core.OS;
using QRCoder;
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

/*
    This class uses the QRCoder package: https://www.nuget.org/packages/QRCoder/1.7.0#show-readme-container
    QRCoder is released under the MIT licence.
*/

namespace Omni
{
    public static class QRcodeHelper
    {
        static readonly string TAG = typeof(QRcodeHelper).FullName ?? "QRcodeHelper";

        private static BarcodeDetector? barcodeDetector;
        private static CameraSource? cameraSource;

        public static bool IsScanning { get; private set; } = false;

        public static Bitmap? CreateQRCode(string data)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeAsBitmapByteArr = qrCode.GetGraphic(15);

            var bitmap = BitmapFactory.DecodeByteArray(qrCodeAsBitmapByteArr, 0, qrCodeAsBitmapByteArr.Length);

            return bitmap;
        }

        public static void StartScanQRcode(Context? context, SurfaceView? scanSurfaceView, Looper? looper, Action<string>? barCodeAction, Action<bool> UIUpdater)
        {
            IsScanning = false;
            UIUpdater(false);

            if (context == null || scanSurfaceView == null || looper == null) return;

            if (!PermissionsHelper.CheckPermissions(PermissionsHelper.CameraPermissions))           // check camera if don't have camera permission
            {
                PermissionsHelper.RequestPermission(Manifest.Permission.Camera, () =>               // request permission if don't already have it
                {
                    if (PermissionsHelper.CheckPermissions(PermissionsHelper.CameraPermissions))    // check again after request made
                    {
                        StartScan(context, scanSurfaceView, looper, barCodeAction, UIUpdater);
                    }
                    else                                                                            
                    {
                        var permissionText = Resource.String.share_settings_permission_rationale;
                        var alert = new AlertDialog.Builder(context);
                        alert.SetTitle(context.GetString(Resource.String.permissions_premission_required))?
                            .SetMessage(permissionText)?
                            .SetPositiveButton(Resource.String.dialog_ok, (sender, args) => { });
                        alert.Create().Show();
                    }
                });
            }
            else
            {
                StartScan(context, scanSurfaceView, looper, barCodeAction, UIUpdater);
            }
        }

        private static void StartScan(Context? context, SurfaceView? scanSurfaceView, Looper? looper, Action<string>? barCodeAction, Action<bool> UIUpdater)
        {
            IsScanning = false;
            UIUpdater(false);

            if (context == null || scanSurfaceView == null || looper == null) return;

            try
            {
                var handler = HandlerCompat.CreateAsync(looper);

                barcodeDetector = new BarcodeDetector.Builder(context)
                    .SetBarcodeFormats(BarcodeFormat.AllFormats)
                    .Build();

                cameraSource = new CameraSource.Builder(context, barcodeDetector)
                    .SetRequestedPreviewSize(300, 300)
                    .SetAutoFocusEnabled(true)
                    .Build();

                scanSurfaceView?.Holder?.AddCallback(new SurfaceHolderCallback(() =>   // callback when surface createed
                {
                    try
                    {
                        cameraSource.Start(scanSurfaceView.Holder);
                    }
                    catch (Exception ex)
                    {
                        if (ex != null)
                        {
                            Log.Info(TAG, $"Exception starting camera scan: {ex}");
                            return;
                        }
                    }
                }));

                barcodeDetector.SetProcessor(new Processor((barCodes) =>                // callback for barcodes recognised
                {
                    if (barCodeAction != null && barCodes.Size() > 0)
                    {
                        if (barCodes.ValueAt(0) is Barcode barCode)
                            handler?.Post(() => barCodeAction(barCode.DisplayValue));
                    }
                }));
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"Exception starting camera scan: {ex}");
                IsScanning = false;
                UIUpdater(false);
                return;
            }

            IsScanning = true;
            UIUpdater(true);
        }

        public static void StopScanQRcode(Action<bool> UIUpdater)
        {
            cameraSource?.Stop();
            IsScanning = false;
            UIUpdater(false);
        }

    }

    internal class Processor(Action<SparseArray> action) : Java.Lang.Object, Detector.IProcessor
    {
        private readonly Action<SparseArray> action = action;

        public void ReceiveDetections(Detector.Detections detections)
        {
            var barCodes = detections.DetectedItems;
            if (barCodes.Size() != 0)
            {
                action(barCodes);
            }
        }

        public void Release() { }
    }

    internal class SurfaceHolderCallback(Action? onSurfaceCreated) : Java.Lang.Object, ISurfaceHolderCallback
    {
        private readonly Action? onSurfaceCreated = onSurfaceCreated;

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Android.Graphics.Format format, int width, int height)
        {
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            onSurfaceCreated?.Invoke();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            
        }
    }
}

