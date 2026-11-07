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
    public class DeveloperFragment : Fragment, IFragmentServiceUpdates
    {
        static readonly string TAG = typeof(DashboardFragment).FullName ?? "DashboardFragment";

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new NotImplementedException();
        }

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.developer_fragment, container, false);

            if (view != null)
            {
                view.FindViewById<Button>(Resource.Id.set_state_1_button)?.Click += SetStateButton_Click;
                view.FindViewById<Button>(Resource.Id.set_state_2_button)?.Click += SetStateButton_Click;
            }

            return view;
        }

        private void SetStateButton_Click(object? sender, EventArgs e)
        {
            if (Activity == null) return;
            try
            {
                var mainActivity = (MainActivity)Activity;
                var service = mainActivity.MainServiceConnection?.Binder?.Service;
                switch (((Button?)sender)?.Id)
                {
                    case Resource.Id.set_state_1_button:
                        service?.SetState(State.WaitingForThrottleOrCurrent);
                        break;
                    case Resource.Id.set_state_2_button:
                        service?.SetState(State.Running);
                        break;
                }
            }
            catch (DeadObjectException ex)
            {
                Log.Info(TAG, $"Exception setting state as connetion to service lost: {ex}");
            }
        }

    }
}