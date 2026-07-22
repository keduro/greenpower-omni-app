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
    public class InformationFragment : Fragment, IFragmentServiceUpdates
    {
        TextView? informationText;

        public override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            throw new System.NotImplementedException();
        }

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.information_fragment, container, false);

            informationText = view?.FindViewById<TextView>(Resource.Id.information_text);
            var plainText = GetString(Resource.String.information_text);
            var styledText = Html.FromHtml(plainText, FromHtmlOptions.ModeLegacy);
            informationText?.SetText(styledText, TextView.BufferType.Normal);

            return view;
        }

    }
}