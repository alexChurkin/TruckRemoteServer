using System;
using System.Globalization;

namespace TruckRemoteServer.Protocol
{
    //State message of the controller: steering, brake, gas, left signal, right signal, emergency,
    //parking brake, lights, horn, cruise (all are required),
    //then optional: gas level, brake level (0..1), action counters (see PCController),
    //and the last field may be the message number tagged with '#' (newer controllers).
    //Toggles are flipped on every click, so a lost packet can't lose a click
    public class ControllerMessage
    {
        private const int REQUIRED_PARTS = 10;
        private const int PEDAL_LEVELS_INDEX = 10;
        private const int ACTIONS_INDEX = 12;
        public const string SEQUENCE_TAG = "#";

        public double Steering;
        public bool BrakePressed;
        public bool GasPressed;
        public bool LeftSignalClick;
        public bool RightSignalClick;
        public bool EmergencyClick;
        public bool ParkingBrakeClick;
        public bool LightsClick;
        public int Horn;
        public bool CruiseClick;

        public bool HasPedalLevels;
        public double GasLevel;
        public double BrakeLevel;

        public int[] ActionCounters = new int[0];

        public long? Sequence;

        //Returns null if the message isn't a controller state.
        //maxActions limits the counters to the actions known by the server
        public static ControllerMessage Parse(string message, int maxActions)
        {
            string[] parts = message.Trim().Split(',');
            long? sequence = null;

            try
            {
                string last = parts[parts.Length - 1];
                if (last.StartsWith(SEQUENCE_TAG))
                {
                    sequence = long.Parse(last.Substring(1), CultureInfo.InvariantCulture);
                    Array.Resize(ref parts, parts.Length - 1);
                }
                if (parts.Length < REQUIRED_PARTS) return null;

                var result = new ControllerMessage
                {
                    Sequence = sequence,
                    Steering = ParseDouble(parts[0]),
                    BrakePressed = bool.Parse(parts[1]),
                    GasPressed = bool.Parse(parts[2]),
                    LeftSignalClick = bool.Parse(parts[3]),
                    RightSignalClick = bool.Parse(parts[4]),
                    EmergencyClick = bool.Parse(parts[5]),
                    ParkingBrakeClick = bool.Parse(parts[6]),
                    LightsClick = bool.Parse(parts[7]),
                    Horn = int.Parse(parts[8], CultureInfo.InvariantCulture),
                    CruiseClick = bool.Parse(parts[9])
                };

                if (parts.Length >= PEDAL_LEVELS_INDEX + 2)
                {
                    result.HasPedalLevels = true;
                    result.GasLevel = ParseDouble(parts[PEDAL_LEVELS_INDEX]);
                    result.BrakeLevel = ParseDouble(parts[PEDAL_LEVELS_INDEX + 1]);
                }

                int actionsCount = Math.Max(0, Math.Min(parts.Length - ACTIONS_INDEX, maxActions));
                result.ActionCounters = new int[actionsCount];
                for (int i = 0; i < actionsCount; i++)
                {
                    result.ActionCounters[i] = int.Parse(parts[ACTIONS_INDEX + i], CultureInfo.InvariantCulture);
                }
                return result;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        //NaN and infinity can't be applied to axes
        private static double ParseDouble(string text)
        {
            double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }
}
