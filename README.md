# OMNI telemetry
Developed and maintained by the Banchory Greenpower team, the OMNI
telemetry app works with the eChook data logging and telemetry system
available from eChook.uk for electric cars participating in the Greenpower
Education Trust challenge. 

The app connects to the eChook using bluetooth to collect sensor data including
battery voltage, current, motor rpm, speed, throttle, brake, and temperatures.
The app calculates distance travelled and battery usage from the sensor data and 
acquires the car position.

The data can be shown on a number of different dashboards in different sizes and device 
orientations to provide an in car display.

The app logs the data every 0.25s to a file which can be shared or downloaded for later
analysis.

The app can also upload the telemetry data to a website, such as the eChook live data site, or to
a user defined location to allow the data to be viewed in near real time during testing or racing.

## OMNI telemetry code
This repository contains all the resources and code to allow building of
the Android app. The app is a .NET for Android project, coded in C#.

## Changes
A description of all changes other than minor changes is maintained in 
MainActivity.cs, the source for the main activity.

## Modifications
This code is shared under the GPL-3.0 licence - see LICENCE. You are 
welcome to use this code and to make improvements. If you have modifications that 
you would like to have added and shared with others
then get in contact with us and we will add them here. 
Pull requests are welcome, however they must not break the original functionality
and requests will be merged at the developers' discretion.

## Support
Please contact us at mailto:omni@banchory-greenpower.org

## Links
https://www.greenpower.co.uk/

https://www.echook.uk/

