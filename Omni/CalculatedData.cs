using Java.Util;
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
    internal static class CalculatedData
    {
        public static double calculatedAs = 0;              // in As (AmpSeconds)
        public static double calculatedJ = 0;               // in J  (Joules=Ws)
        public static double calculatedDistance = 0;        // in m

        public static double previousAmps = 0;
        public static double previousWatts = 0;
        public static Date? previousAmpsTime = null;
        public static double previousSpeed = 0;
        public static Date? previousSpeedTime = null;
        public static double previousEfficiencyAs = 0;
        public static double previousEfficiencyJ = 0;
        public static double previousEfficiencyDistance = 0;
        public static Date? previousEfficiencyTime = null;
    }
}