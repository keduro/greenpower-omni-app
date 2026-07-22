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
    public class LapCounter
    {
        static readonly string TAG = typeof(LapCounter).FullName ?? "LapCounter";

        private enum State { Idle, AcquiringStartLocation, AcquiringObserverLocation, AcquiringLapCount }

        private int currentLap;
        private int lapOffset;
        private Location? startLocation;
        private DateTime startTime;
        private float maxDistanceFromStart;
        private Location? observerLocation;
        private Location? previousLocation;
        private double theta;
        private State state;
        private readonly List<Location> startLocationList    = [];      // buffer for up to last 20 locations (i.e. 20s) to determine start location
        private readonly List<Location> observerLocationList = [];      // list for saving locations when determining observer location
        private readonly float[] distanceResults = new float[3];

        public LapCounter ()
        {
            Reset();
        }

        public void Reset()
        {
            currentLap = 1;
            lapOffset = 0;
            startLocation = null;
            startTime = DateTime.MinValue;
            maxDistanceFromStart = 0F;
            observerLocation = null;
            previousLocation = null;
            theta = 0.0;
            startLocationList.Clear();
            observerLocationList.Clear();
            state = State.Idle;
        }

        public void ResetLapNumber()
        {
            lapOffset = currentLap - 1;
        }

        public void AcquireStartLocation()
        {
            state = State.AcquiringStartLocation;
            startLocationList.Clear();
        }

        public void AcquireLapCount()
        {
            state = State.AcquiringObserverLocation;
            observerLocationList.Clear();
            startTime = DateTime.Now;
            maxDistanceFromStart = 0F;
        }

        private int LapNumber()
        {
            return currentLap - lapOffset;
        }

        public int PositionUpdate(Location? location)
        {
            Log.Info(TAG, "Lapcounter position update");
            if (location == null) return LapNumber();
            if (!location.HasAccuracy) return LapNumber();       // need to have accuracy in the location

            switch (state)
            {
                case State.Idle:
                    break;
                case State.AcquiringStartLocation:                  // start location is average of twenty most recent locations, i.e. last 20 seconds
                    {
                        startLocationList.Add(location);
                        while (startLocationList.Count > 20)
                        {
                            startLocationList.RemoveAt(0);
                        }
                        startLocation ??= new Location("");
                        startLocation.Latitude  = startLocationList.Select(l => l.Latitude ).Average();
                        startLocation.Longitude = startLocationList.Select(l => l.Longitude).Average();
                        startLocation.Accuracy  = startLocationList.Select(l => l.Accuracy ).Average();
                    }
                    break;
                case State.AcquiringObserverLocation:               // aquire locations during first lap and take centroid as observer location
                    {
                        if (startLocation == null)                  // just in case launch button pressed after started running! 
                        {
                            startLocation = location;
                        }
                        else
                        {
                            observerLocationList.Add(location);
                            Location.DistanceBetween(startLocation.Latitude, startLocation.Longitude, location.Latitude, location.Longitude, distanceResults);
                            float distanceFromStart = distanceResults[0];
                            if (distanceFromStart > maxDistanceFromStart) maxDistanceFromStart = distanceFromStart;
                                                                                                                
                                                                                                                // Heuristics to determine if close to one lap complete
                            if (   (maxDistanceFromStart > 60)                                                  //       been more than 60m from start
                                && ((DateTime.Now-startTime).TotalSeconds > 25)                                 //   and been away for more than 25s
                                && (distanceFromStart < (startLocation.Accuracy + location.Accuracy) * 1.5))    //   and are getting close to being back at start
                            {                                                                                   // Above values allow for short circuits such as Alford
                                state = State.AcquiringLapCount;
                                observerLocation = new Location("")
                                {
                                    Latitude = observerLocationList.Select(l => l.Latitude).Average(),
                                    Longitude = observerLocationList.Select(l => l.Longitude).Average(),
                                    Accuracy = observerLocationList.Select(l => l.Accuracy).Average()
                                };
                                previousLocation = startLocation;
                                foreach (var loc in observerLocationList)
                                    UpdateLapNumber(loc);
                            }
                        }
                    }
                    break;
                case State.AcquiringLapCount:
                    {
                        UpdateLapNumber(location);
                    }
                    break;
            }
            return LapNumber();
        }

        private void UpdateLapNumber(Location location)
        {
            if (previousLocation != null && observerLocation != null)
            {
                double previousAngle = Math.Atan2(previousLocation.Latitude - observerLocation.Latitude, previousLocation.Longitude - observerLocation.Longitude);
                double currentAngle = Math.Atan2(location.Latitude - observerLocation.Latitude, location.Longitude - observerLocation.Longitude);
                double deltaAngle = currentAngle - previousAngle;
                if (deltaAngle < -Math.PI)
                    deltaAngle += 2 * Math.PI;
                else if (deltaAngle > Math.PI)
                    deltaAngle -= 2 * Math.PI;
                theta += deltaAngle;
                var rotations = Math.Abs(theta) / 2.0 / Math.PI;
                var rotationsLap = Convert.ToInt32(Math.Truncate(rotations)) + 1;
                if (rotationsLap > currentLap)
                    currentLap = rotationsLap;
            }
            previousLocation = location;
        }
    }
}