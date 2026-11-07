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
	public class ServiceBinder : Binder                     // Class used for the client Binder.  Since this service runs 
															// in the same process as its clients, we don't need to deal with IPC.
	{
		public MainService? Service { set; get; }

		public delegate void DataReceived(List<string> Keys);

		public event DataReceived? OnDataReceived;

		public void DataAvailable(List<string> Keys)
		{
			OnDataReceived?.Invoke(Keys);
		}

		public delegate void DataUploaded();

		public event DataUploaded? OnDataUploaded;

		public void DataUpload()
		{
			OnDataUploaded?.Invoke();
		}

	}
}