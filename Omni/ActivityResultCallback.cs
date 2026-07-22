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
    internal class ActivityResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        readonly Action<Java.Lang.Object?> _callback;
        public ActivityResultCallback(Action<Java.Lang.Object?> callback) => _callback = callback;
        public ActivityResultCallback(TaskCompletionSource<Java.Lang.Object?> tcs) => _callback = tcs.SetResult;
        public void OnActivityResult(Java.Lang.Object? p0) => _callback(p0);
    }
}
