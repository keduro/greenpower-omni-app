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

    //=======================================================================================================================
    // Banchory Greenpower Telemetry
    //
    //	Data logging and telemetry app for Banchory Greenpower electric cars built to participate in the 
    //	Greenpower Trust challenge. Receives sensor data using bluetooth, tracks GSP position, logs data
    //	on phone SD card, sends data to telemetry data website for pit display.
    //	Makes use of foreground service to ensure data continues to be captured even if the user 
    //  navigates away from the app.
    //	Code is based on the following:
    //		https://docs.microsoft.com/en-gb/samples/xamarin/monodroid-samples/android-o-androidplaylocation-locupdfgservice/
    //
    //	Version	Date		Author		Comments
    //		1	Apr-2020	K.Roberts	Original
    //			Feb-2022	K.Roberts	Added privacy policy fragment
    //          Dec-2023	K.Roberts	Removed 'WaitingForLaunchButton' state. Now only two states i.e. 'WaitingForThrottle'
    //									and 'Running'. Receipt of launch button (race start) press for car now simply resets
    //									lap counting rather than being a necessary intermediate step. This change implemented
    //									to support cars which do not have race start button. 
    //									Change specifically requested by Southleigh racing, for their car Apex.
    //			Dec-2023	K.Roberts	Use StartForegroundService for >= Android 8 / API 26  / Oreo
    //									Add permissions 'POST_NOTIFICATIONS' & 'ACCESS_BACKGROUND_LOCATION' based on API level
    //									Various refactoring / tidy up
    //			Dec-2023	K.Roberts	StartForeground only once when service is started rather than each time the main
    //									activity is (re)bound. 
    //									Updates to use of notification channel for >= Android 8 / API 26  / Oreo
    //									Various refactoring / tidy up
    //			Dec-2023	K.Roberts	Rework permissions requesting to meet requirements for prominent in-app disclosure of 
    //									use of location in the background.
    //			Sep-2024	K.Roberts	Changes required by Android 14 for foreground service permissions
    //			Mar-2026	K.Roberts	Overhaul to use AndroidX and migrate from Xamarin.Android to Android .net app
	//									Update to target API 36 (Android 16), changed to minimum API 29 (Android 10)
    //									Overhaul of permissions UI - removed PermissionsActivity added PermissionsFragment
	//									Reinstated checking for battery optimisation and allowing setting of it.
	//									Record phone accelerometer and gyro data as requested by George Watson's
	//									Add abiilty to share / transfer key settings via QR code
    //
    //=======================================================================================================================

    [Activity(Label = "OMNI telemetry", Theme = "@style/AppTheme", MainLauncher = false)]
	public class MainActivity : AppCompatActivity, ISharedPreferencesOnSharedPreferenceChangeListener
	{
		static readonly string TAG = typeof(MainActivity).FullName ?? "MainActivity";

        bool isSimulation = false;												// indicates whether to use simulated eChook and location data
        public bool Bound = false;												// true if service is bound

		IMenuItem? developerMenuItem;                                           // developer menu item which can shown
		bool developerMenuItemVisible = false;									// by clicking on 7 times in quick succession
		DateTime developerLogoClickStart = DateTime.MinValue;
		int developerLogoClickCount;

        BatteryOptimizationHelper?  batteryOptimizationHelper;					// separate out battery optimization checking / requesting

		readonly List<int> dashboardFragmentLayoutResourceIds =                 // list of different layouts cycled between in 
		[																		// dashboard fragment
			Resource.Layout.dashboard0_fragment
			,Resource.Layout.dashboard1_fragment
			,Resource.Layout.dashboard2_fragment
			//,Resource.Layout.dashboard3_fragment
			//,Resource.Layout.dashboard4_fragment
			//,Resource.Layout.dashboard5_fragment
		];

		public ServiceConnection? MainServiceConnection;                        // monitors the state of the connection to the service

        protected override void OnCreate(Bundle? savedInstanceState)
		{
			Log.Info(TAG, $"onCreate. TaskId={this.TaskId}");
            base.OnCreate(savedInstanceState);

            isSimulation = Intent != null && Intent.GetBooleanExtra(Constants.SIMULATION_NAME, false);

            MainServiceConnection = new ServiceConnection { Activity = this };

			PermissionsHelper.Init(this);

			batteryOptimizationHelper = new BatteryOptimizationHelper { MainActivity = this };

			if (Build.VERSION.SdkInt >= BuildVersionCodes.R)		// only enable edge to edge for >= Android 11
			{
				WindowCompat.EnableEdgeToEdge(Window);
			}

            SetContentView(Resource.Layout.activity_main);

            // Add Toolbar - created programmatically as a workaround to exception raised when starting app
            // when trying to inflate toolbar widget saying class not found

            var relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativelayout);
            AndroidX.AppCompat.Widget.Toolbar? toolbar = null;
            if (relativeLayout != null)
            {
                toolbar = OmniToolbar.Create(relativeLayout, Resource.Id.toolbar, Resource.Id.toolbar_image, ToolBarImage_Click);
            }
            SetSupportActionBar(toolbar);

            // Ensure toolbar correctly placed for android versions with edge-to-edge display

            ViewCompat.SetOnApplyWindowInsetsListener(
                toolbar,
                new OnApplyWindowInsetsListener((view, insets) =>
                {
                    AndroidX.Core.Graphics.Insets? bars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
                    if (Resources?.Configuration?.Orientation == Android.Content.Res.Orientation.Portrait)
					{
                        if (view != null && bars != null) view.SetPadding(0, bars.Top, 0, 0);
                    }
                    else
					{
                        if (view != null && bars != null) view.SetPadding(0, bars.Top, bars.Right, 0);
                    }
                    WindowInsetsCompat? rrr = WindowInsetsCompat.Consumed;
                    return rrr ?? throw new Exception("Exception setting window insets - Consumed is null");
                }));

            ViewCompat.SetOnApplyWindowInsetsListener(
                FindViewById<LinearLayout>(Resource.Id.activity_main),
                new OnApplyWindowInsetsListener((view, insets) =>
                {
                    AndroidX.Core.Graphics.Insets? bars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
                    if (view != null && bars != null) view.SetPadding(0, 0, 0, bars.Bottom);
                    WindowInsetsCompat? rrr = WindowInsetsCompat.Consumed;
                    return rrr ?? throw new Exception("Exception setting window insets - Consumed is null");
                }));

            // Add fragment

            if (savedInstanceState != null)
			{
				string fragmentTypeName = savedInstanceState.GetString(Constants.FRAGMENT_TYPE_KEY) ?? string.Empty;
				string fragmentTag = savedInstanceState.GetString(Constants.FRAGMENT_TAG_KEY) ?? string.Empty;
				switch (fragmentTypeName)
				{
					case "DashboardFragment":		LoadFragment(new DashboardFragment(),     fragmentTag);		break;
					case "InformationFragment":		LoadFragment(new InformationFragment(),   fragmentTag);		break;
					case "LocationFragment":		LoadFragment(new LocationFragment(),      fragmentTag);		break;
					case "PermissionFragment":		LoadFragment(new PermissionFragment(),    fragmentTag);     break;
					case "PrivacyPolicyFragment":	LoadFragment(new PrivacyPolicyFragment(), fragmentTag);		break;
					case "SettingsFragment":		LoadFragment(new SettingsFragment(),      fragmentTag);		break;
					case "DeveloperFragment":		LoadFragment(new DeveloperFragment(),     fragmentTag);		break;
					case "SupportFragment":			LoadFragment(new SupportFragment(),		  fragmentTag);		break;
					case "ShareSettingsFragment":	LoadFragment(new ShareSettingsFragment(), fragmentTag);     break;
					default:						LoadFragment(new DashboardFragment(),     fragmentTag);		break;
				}
				developerMenuItemVisible = savedInstanceState.GetBoolean(Constants.DEVELOPER_KEY, false);
			}
			else
			{
                // If any required permissions have not been granted show permissions fragment 

                if (   !PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions)
                    || !PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions)
                    || !PermissionsHelper.CheckPermissions(PermissionsHelper.NotificationPermissions))
                {
                    LoadFragment(new PermissionFragment(), string.Empty);
                }
                else
                {
                    LoadFragment(new DashboardFragment(), dashboardFragmentLayoutResourceIds[0].ToString());
                }
            }

			// Handle pressing back

			OnBackPressedDispatcher.AddCallback(new OnBackPressedCallback( () =>
			{
                if (SupportFragmentManager.BackStackEntryCount >= 2) SupportFragmentManager.PopBackStack();
            }));

            // Set up fragment result listener for when permissions fragment is done

            SupportFragmentManager.SetFragmentResultListener(Constants.PERMISSIONS_KEY, this, new FragmentResultListener((key, bundle) =>
			{
                LoadFragment(new DashboardFragment(), dashboardFragmentLayoutResourceIds[0].ToString());
            }));

            // Check battery optimization and get user to disable if necessary

            if (batteryOptimizationHelper.CheckBatteryOptimization())
            {
				batteryOptimizationHelper.RequestIgnoreBatteryOptimization();
			}
		}

        protected override void OnRestart()
        {
            Log.Info(TAG, "onRestart.");
            base.OnRestart();
        }

        protected override void OnStart()
		{
			Log.Info(TAG, "onStart.");
			base.OnStart();

            UpdateKeepScreenOn();

			// Need to both bind to the service and start the service as it is a hybrid service
			// Bind to the service
			BindService(new Intent(this, typeof(MainService)), MainServiceConnection, Bind.AutoCreate);

			// Now start the service as a foreground service
			StartService();
		}

		protected override void OnResume()
		{
			Log.Info(TAG, "onResume.");
			base.OnResume();
			PreferenceManager.GetDefaultSharedPreferences(this)?.RegisterOnSharedPreferenceChangeListener(this);

            // add data received event handler. First time round this is done when service service connection.onserviceconnected
            if (MainServiceConnection is not null && MainServiceConnection.Binder is not null)
			{
				MainServiceConnection.Binder.OnDataReceived += Binder_OnDataReceived;
				MainServiceConnection.Binder.OnDataUploaded += Binder_OnDataUploaded;
			}
		}

		protected override void OnPause()
		{
			Log.Info(TAG, "onPause.");

			if (Bound)
			{
				try                             // try to remove event handler - may not yet be attached!
				{
					MainServiceConnection?.Binder?.OnDataReceived -= Binder_OnDataReceived;
                    MainServiceConnection?.Binder?.OnDataUploaded -= Binder_OnDataUploaded;
                }
                catch (Java.Lang.Exception) { }
			}

			PreferenceManager.GetDefaultSharedPreferences(this)?.UnregisterOnSharedPreferenceChangeListener(this);
			base.OnPause();
		}

		protected override void OnStop()
		{
			Log.Info(TAG, "onStop.");
			if (Bound)
			{
				// Unbind from the service. This signals to the service that this activity is no longer
				// in the foreground, and the service can respond by promoting itself to a foreground
				// service.
				UnbindService(MainServiceConnection);
				Bound = false;
			}
			base.OnStop();
		}

        protected override void OnDestroy()
        {
            Log.Info(TAG, "onDestroy.");
            base.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle outState)
		{
			Log.Info(TAG, "onSaveInstanceState.");
			var fragment = GetVisibleFragment();
			if (fragment != null)
			{
				outState.PutString(Constants.FRAGMENT_TYPE_KEY, fragment.GetType().Name);
				outState.PutString(Constants.FRAGMENT_TAG_KEY, fragment.Tag);
			}
			outState.PutBoolean(Constants.DEVELOPER_KEY, developerMenuItemVisible);
			base.OnSaveInstanceState(outState);
		}

        public void Binder_OnDataReceived(List<string> Keys)
        {
            foreach (var fragment in SupportFragmentManager.Fragments)
            {
                if ((fragment != null) && (fragment.IsVisible))
                {
                    ((IFragmentServiceUpdates)fragment).OnDataReceived(Keys);
                }
            }
        }

        public void Binder_OnDataUploaded()
        {
            foreach (var fragment in SupportFragmentManager.Fragments)
            {
                if ((fragment != null) && (fragment.IsVisible))
                {
                    ((IFragmentServiceUpdates)fragment).OnDataUploaded();
                }
            }
        }

        private Fragment? GetVisibleFragment()
		{
            foreach (var fragment in SupportFragmentManager.Fragments)
            {
                if ((fragment != null) && (fragment.IsVisible)) return fragment;
			}
			return null;
		}

		public override bool OnCreateOptionsMenu(IMenu? menu)
		{

            //MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            //developerMenuItem = menu?.FindItem(Resource.Id.navigation_developer);
            //developerMenuItem?.SetVisible(developerMenuItemVisible);

            // Create menu programatically rather than inflating menu_main.xml,
            // workaround associated with creating the toolbar programmatically
			
            if (menu != null)
			{
				OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_dashboard, GetString(Resource.String.menu_data), Resource.Drawable.baseline_dashboard_white_36dp, ShowAsAction.Always, true);
				OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_location, GetString(Resource.String.menu_location), Resource.Drawable.baseline_map_white_36dp, ShowAsAction.IfRoom, true);
				OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_settings, GetString(Resource.String.menu_settings), Resource.Drawable.baseline_settings_white_36dp, ShowAsAction.IfRoom, true);
				OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_share, GetString(Resource.String.menu_share), Resource.Drawable.baseline_share_white_36dp, ShowAsAction.IfRoom, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_share_settings, GetString(Resource.String.menu_share_settings), Resource.Drawable.baseline_settings_white_36dp, ShowAsAction.Never, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_developer, GetString(Resource.String.menu_developer), Resource.Drawable.baseline_developer_mode_white_36dp, ShowAsAction.Never, false);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_permission, GetString(Resource.String.menu_permissions), Resource.Drawable.baseline_info_white_36dp, ShowAsAction.Never, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_information, GetString(Resource.String.menu_info), Resource.Drawable.baseline_info_white_36dp, ShowAsAction.Never, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_support, GetString(Resource.String.menu_support), Resource.Drawable.baseline_info_white_36dp, ShowAsAction.Never, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_privacy_policy, GetString(Resource.String.menu_privacy), Resource.Drawable.baseline_info_white_36dp, ShowAsAction.Never, true);
                OmniToolbar.AddToolbarMenuItem(menu, Resource.Id.navigation_close, GetString(Resource.String.menu_close), Resource.Drawable.baseline_cancel_white_36dp, ShowAsAction.IfRoom, true);
				developerMenuItem = menu.FindItem(Resource.Id.navigation_developer);
			}

            return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Resource.Id.navigation_dashboard:
					{
						int tag = dashboardFragmentLayoutResourceIds[0];
						var fragment = GetVisibleFragment();
						if ((fragment != null) && (fragment is DashboardFragment))		// if already showing dashboard
						{																// cycle to next dashboard layout
							if (int.TryParse(fragment.Tag, out var tagint))
							{
								var ix = dashboardFragmentLayoutResourceIds.IndexOf(tagint);
								ix++;
								if (ix >= dashboardFragmentLayoutResourceIds.Count) ix = 0;
								tag = dashboardFragmentLayoutResourceIds[ix];
							}
						}
						LoadFragment(new DashboardFragment(), tag.ToString());
					}
					break;
				case Resource.Id.navigation_location:	
					LoadFragment(new LocationFragment(), null);
					break;
				case Resource.Id.navigation_settings:
					LoadFragment(new SettingsFragment(), null);
					break;
				case Resource.Id.navigation_share:
                    {
						if (string.IsNullOrEmpty(Shared.LogFilePath))
                        {
							var alert = new AlertDialog.Builder(this);
							alert.SetTitle(Resource.String.dialog_nothing_to_share)?
								.SetPositiveButton("Ok", (sender, args) => { });
							alert.Create().Show();
						}
						else
                        {
                            Uri? fileUri = FileProvider.GetUriForFile(ApplicationContext, Constants.FILE_PROVIDER_AUTHORITY, new Java.IO.File(Shared.LogFilePath));
							if (fileUri != null)
							{
                                var alert = new AlertDialog.Builder(this);
                                alert.SetTitle(Resources?.GetString(Resource.String.dialog_share_logged_data))?
                                .SetMessage(Shared.LogFilePath)?
                                .SetPositiveButton(GetString(Resource.String.dialog_share), (sender, args) =>
                                {
                                    var shareIntent = new Intent(Intent.ActionSend);
                                    shareIntent.SetType("text/csv");
                                    shareIntent.PutExtra(Intent.ExtraStream, fileUri);
                                    shareIntent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
									var intent = Intent.CreateChooser(shareIntent, Resources?.GetString(Resource.String.dialog_share_logged_data));
									if (intent != null) StartActivity(intent);
                                })?
                                .SetNegativeButton(GetString(Resource.String.dialog_cancel), (sender, args) => { });
                                alert.Create().Show();
                            }
                        }
                    }
					break;
				case Resource.Id.navigation_developer:
					LoadFragment(new DeveloperFragment(), null);
					break;
                case Resource.Id.navigation_share_settings:
                    LoadFragment(new ShareSettingsFragment(), null);
                    break;
                case Resource.Id.navigation_permission:
					LoadFragment(new PermissionFragment(), null);
					break;
				case Resource.Id.navigation_information:	
					LoadFragment(new InformationFragment(), null);
					break;
				case Resource.Id.navigation_support:
					LoadFragment(new SupportFragment(), null);
					break;
				case Resource.Id.navigation_privacy_policy:
					LoadFragment(new PrivacyPolicyFragment(), null);
					break;
				case Resource.Id.navigation_close:
					{
						AlertDialog.Builder alert = new(this);
						//alert.SetTitle("Close OMNI telemetry?");
						alert.SetTitle(GetString(Resource.String.dialog_close_omni_text));
						alert.SetPositiveButton(GetString(Resource.String.dialog_close), (sender, args) =>
						{
							try
							{
								MainServiceConnection?.Binder?.Service?.CloseService();
                            }
                            catch (DeadObjectException) { }		// ignore lost connection to service!
							FinishAndRemoveTask();
						});
						alert.SetNegativeButton(GetString(Resource.String.dialog_cancel), (sender, args) =>
						{
						// cancel - don't close!
						});
						alert.Create().Show();
					}
					break;
			}
			return base.OnOptionsItemSelected(item);
		}

		public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
		{
			UpdateKeepScreenOn();
		}

		public void StartService()				// This may be called to start or restart service
		{
			if (ApplicationContext == null) return;

			var haveLocationPermissions = PermissionsHelper.CheckPermissions(PermissionsHelper.LocationPermissions);	// check if have required location permissions
            var haveBluetoothPermissions = PermissionsHelper.CheckPermissions(PermissionsHelper.BluetoothPermissions);	// check if have required bluetooth permissions
			
																						// From Android 14 onwards can only start the service with
            Log.Info(TAG, "Starting foreground service");                               // a foreground service type, and that only succeeds if the
            Log.Info(TAG, $"  have location permissions : {haveLocationPermissions}");  // relevant permissions have already been granted
            Log.Info(TAG, $"  have bluetooth permissions: {haveBluetoothPermissions}");	// Foreground service type is set in MainService.OnStartCommand
			try																			// Service is started even if don't have location or bluetooth
            {																			// permissions, e.g. for simulation
                var intent = new Intent(ApplicationContext, typeof(MainService));
                intent.PutExtra(Constants.SIMULATION_NAME, isSimulation);
                StartForegroundService(intent);
            }
            catch (Java.Lang.IllegalStateException)
            {
                Log.Info(TAG, "Not allowed to start service intent as app is in background!");
            }
        }

        private void ToolBarImage_Click(object? sender, System.EventArgs e)
		{
			const double MultiClicksPeriod = 1500;
			const int    MultiClicksNeeded = 7;
			DateTime now = DateTime.Now;
			if ((now-developerLogoClickStart).TotalMilliseconds > MultiClicksPeriod)
			{
				developerLogoClickStart = now;
				developerLogoClickCount = 1;
				return;
			}
			developerLogoClickCount++;
			Log.Info("HERE", $">>>> Click count: {developerLogoClickCount}");
			if (developerLogoClickCount >= MultiClicksNeeded)
			{
				developerMenuItemVisible = true;
				developerMenuItem?.SetVisible(true);
				var toast = Toast.MakeText(this, "Developer mode enabled", ToastLength.Short);  // use toast to avoid interfering with any current snackbars
				toast?.Show();
			}
		}

		private void UpdateKeepScreenOn()
		{
			var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
			if (sharedPreferences != null && Window != null)
			{
                bool keepScreenOn = sharedPreferences.GetBoolean(GetString(Resource.String.preference_key_keep_screen_on), false);
                if (keepScreenOn)
                    Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                else
                    Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
			else
			{
                Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
        }

		public void LoadFragment(Fragment fragment, string? Tag)
		{
			var currentFragment = SupportFragmentManager.FindFragmentById(Resource.Id.fragment_container);
			if (currentFragment != null && currentFragment.GetType() == fragment.GetType() && currentFragment.Tag == Tag) return;

            FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
            if (currentFragment != null)
			{ 
				fragmentTransaction.Replace(Resource.Id.fragment_container, fragment, Tag);
			}
			else
			{
				fragmentTransaction.Add(Resource.Id.fragment_container, fragment, Tag);
			}
			//if (fragment is DashboardFragment) fragmentTransaction.AddToBackStack(Tag);
			fragmentTransaction.Commit();
		}
	}
}

