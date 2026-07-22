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

global using Android;
global using Android.App;
global using Android.Bluetooth;
global using Android.Content;
global using Android.Content.PM;
global using Android.Locations;
global using Android.OS;
global using Android.Runtime;
global using Android.Util;
global using Android.Views;
global using Android.Widget;
global using AndroidX.Activity;
global using AndroidX.Activity.Result;
global using AndroidX.Activity.Result.Contract;
global using AndroidX.Core.Content;
global using AndroidX.Core.View;
global using AndroidX.Fragment.App;
global using AndroidX.Preference;
global using System;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Timers;
global using Activity = AndroidX.AppCompat.App.AppCompatActivity;
global using ActivityFlags = Android.Content.ActivityFlags;
global using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
global using AppCompatActivity = AndroidX.AppCompat.App.AppCompatActivity;
global using Bundle = Android.OS.Bundle;
global using Context = Android.Content.Context;
global using EditTextPreference = AndroidX.Preference.EditTextPreference;
global using Fragment = AndroidX.Fragment.App.Fragment;
global using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;
global using Intent = Android.Content.Intent;
global using Snackbar = Google.Android.Material.Snackbar.Snackbar;
global using Timer = System.Timers.Timer;
global using Uri = Android.Net.Uri;
