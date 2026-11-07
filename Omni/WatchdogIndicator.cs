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
    public class WatchdogIndicator
    {
        readonly int watchdogInterval;
        readonly Activity activity;
        readonly ImageView? view;
        readonly int okImageResourceId;
        readonly int failImageResourceId;

        Timer? watchdogTimer = null;

        public WatchdogIndicator(int WatchdogInterval, Activity Activity, ImageView? View, int OkImageResourceId, int FailImageResourceId)
        {
            watchdogInterval = WatchdogInterval;
            activity = Activity;
            view = View;
            okImageResourceId = OkImageResourceId;
            failImageResourceId = FailImageResourceId;

            SetImage(false);

            watchdogTimer = new Timer(watchdogInterval);
            watchdogTimer.Elapsed += WatchdogTimer_Elapsed;
            watchdogTimer.Enabled = true;
        }

        public void Kick()
        {
            SetImage(true);
            watchdogTimer?.Interval = watchdogInterval;
        }

        public void Stop()
        {
            SetImage(false);
            watchdogTimer?.Stop();
            watchdogTimer?.Dispose();
            watchdogTimer = null;
        }

        private void WatchdogTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs? e)
        {
            SetImage(false);
        }

        private void SetImage(bool Ok)
        {
            activity.RunOnUiThread(() =>
            {
                view?.SetImageResource(Ok ? okImageResourceId : failImageResourceId);
            });
        }
    }
}