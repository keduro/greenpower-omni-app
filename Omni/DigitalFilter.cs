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
    public static class DigitalFilter
    {
        public static double LowPass(double NewInput, double PreviousOutput, double SamplingPeriod, double CutoffFrequency)
        {
            // Formula for the filter is:
            //
            //      y[n] = (α ∙ x[n]) + ((1 - α) ∙ y[n-1])
            //
            // where
            //  y[n]        is the current filtered output
            //  x[n]        is the current input
            //  y[n-1]      is the pevious filtered output
            //  α (aplha)   is the smoothing factor
            //
            // Formula for the smoothing factor is:
            //
            //      α = 1 - exp(-Δt ∙ 2π ∙ fc)

            // where
            //  Δt          is the sampling period in seconds
            //  fc          is the cutoff frequency in hertz

            var alpha = 1.0 - Math.Exp(-1.0 * SamplingPeriod * 2.0 * Math.PI * CutoffFrequency);

            return (alpha * NewInput) + ((1.0 - alpha) * PreviousOutput);
        }

    }
}
