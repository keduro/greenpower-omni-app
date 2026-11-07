using AndroidX.Core.OS;
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
    public class ServiceUpdater(Looper? Looper, Action<Intent>? OnUpdate)
    {
        readonly Action<Intent>? onUpdate = OnUpdate;
        readonly Handler? handler = HandlerCompat.CreateAsync(Looper);

        public void Post(Intent? Intent)
        {
            if (handler != null && onUpdate != null && Intent != null)
            {
                handler.Post(() => onUpdate(Intent));
            }
        }
    }
}
