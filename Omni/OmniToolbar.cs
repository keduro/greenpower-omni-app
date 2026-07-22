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
    public class OmniToolbar()
    {
        public static AndroidX.AppCompat.Widget.Toolbar? Create(RelativeLayout parentView, int toolbarId, int toolbarImageId, EventHandler toolbarImageClick)
        {
            var context = parentView.Context;
            if (context is null) return null;

            var imageViewLayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            var imageView = new ImageView(context)
            {
                Id = toolbarImageId,
                LayoutParameters = imageViewLayoutParameters
            };
            imageView.SetImageResource(Resource.Drawable.toolbar_logo);
            imageView.Click += toolbarImageClick;

            var toolbarLayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
            var toolbar = new AndroidX.AppCompat.Widget.Toolbar(context)
            {
                Id = toolbarId,
                Title = "",
                LayoutParameters = toolbarLayoutParameters,
            };
            toolbar.SetBackgroundResource(Resource.Color.colorPrimary);
            toolbar.OverflowIcon?.SetTint(context.GetColor(Resource.Color.colorOverflow));

            toolbar.AddView(imageView);
                        
            parentView.AddView(toolbar);

            return toolbar;
        }

        public static void AddToolbarMenuItem(IMenu menu, int id, string title, int iconId, ShowAsAction show, bool visible)
        {
            var menuItem = menu.Add(0, id, 0, title);
            menuItem?.SetIcon(iconId);
            menuItem?.SetShowAsActionFlags(show);
            menuItem?.SetVisible(visible);
        }
    }
}
