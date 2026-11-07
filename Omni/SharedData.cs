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
    // Keys used:
    //
    //  Key                 Description                             Source                              Notes
    //  ---                 -----------                             ------                              -----
    //  s                   speed in m/s                            eChook via bluetooth listener
    //  m                   motor rpm                               eChook via bluetooth listener       
    //  i                   motor current                           eChook via bluetooth listener       
    //  v                   total both batteries voltage            eChook via bluetooth listener
    //  w                   lower battery voltage                   eChook via bluetooth listener
    //  t                   input throttle                          eChook via bluetooth listener
    //  d                   actual throttle                         eChook via bluetooth listener
    //  a                   temperature 1                           eChook via bluetooth listener
    //  b                   temperature 2                           eChook via bluetooth listener
    //  c                   temperature 3                           eChook via bluetooth listener
    //  L                   launch button                           eChook via bluetooth listener
    //  C                   cycle view button                       eChook via bluetooth listener
    //  r                   gear ratio                              eChook via bluetooth listener
    //  B                   brake pressed                           eChook via bluetooth listener
    //  0                   Custom data 0                           eChook via bluetooth listener       
    //  1                   Custom data 1                           eChook via bluetooth listener       
    //  2                   Custom data 2                           eChook via bluetooth listener       
    //  3                   Custom data 3                           eChook via bluetooth listener       
    //  4                   Custom data 4                           eChook via bluetooth listener       
    //  5                   Custom data 5                           eChook via bluetooth listener       
    //  6                   Custom data 6                           eChook via bluetooth listener       
    //  7                   Custom data 7                           eChook via bluetooth listener       
    //  8                   Custom data 8                           eChook via bluetooth listener       
    //  9                   Custom data 9                           eChook via bluetooth listener       
    //  W                   Custom data W                           eChook via bluetooth listener       
    //  X                   Custom data X                           eChook via bluetooth listener       
    //  Y                   Custom data Y                           eChook via bluetooth listener       
    //  Z                   Custom data Z                           eChook via bluetooth listener       
    //
    //  latest_telemetry    time latest telemetry was received      main service
    //  bluetooth_connected bluetooth connected 1=true / 0=false    main service
    //
    //  location            location                                location listener
    //  address             address                                 location listener
    //
    //  accelx              accelerometer X                         main service
    //  accely              accelerometer Y                         main service
    //  accelz              accelerometer Z                         main service
    //  gyrox               gyroscope around X                      main service
    //  gyroy               gyroscope around Y                      main service
    //  gyroz               gyroscope around Z                      main service
    //
    //  vupper              lower battery voltage in V              calculated
    //  ampseconds          cumulative ampseconds used since launch calculated
    //  joules              cumulative joules used since launch     calculated
    //  distance            distance since launch in m              calculated
    //  lap                 lap since launch                        calculated
    //  efficiency_as_per_m efficiency in as/m                      calculated   
    //  efficiency_j_per_m  efficiency in j/m                       calculated
    //  start_time          race start time                         calculated
    //  elapsed_time        race elapsed time in s                  calculated
    //  state               launch state                            calculated

    public static class Shared
    {
        public static Bundle Data = new();

        public static bool FirstRun { get; set; } = true;
        public static string? LogFilePath { get; set; } = null;   //KR        
        public static string? QRcodePath { get; set; } = null;  
    }
}