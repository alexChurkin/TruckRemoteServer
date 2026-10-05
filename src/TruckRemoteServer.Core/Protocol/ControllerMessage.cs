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
        private const int RequiredParts = 10;
        private const int PedalLevelsIndex = 10;
        private const int ActionsIndex = 12;
        public const string SequenceTag = "#";

        public double Steering { get; set; }
        public bool BrakePressed { get; set; }
        public bool GasPressed { get; set; }
        public bool LeftSignalClick { get; set; }
        public bool RightSignalClick { get; set; }
        public bool EmergencyClick { get; set; }
        public bool ParkingBrakeClick { get; set; }
        public bool LightsClick { get; set; }
        public int Horn { get; set; }
        public bool CruiseClick { get; set; }

        public bool HasPedalLevels { get; set; }
        public double GasLevel { get; set; }
        public double BrakeLevel { get; set; }

        public int[] ActionCounters { get; set; } = Array.Empty<int>();

        public long? Sequence { get; set; }

        //Returns null if the message isn't a controller state.
        //maxActions limits the counters to the actions known by the server
        public static ControllerMessage Parse(string message, int maxActions)
        {
            string[] parts = message.Trim().Split(',');
            long? sequence = null;

            try
            {
                string last = parts[parts.Length - 1];
                if (last.StartsWith(SequenceTag, StringComparison.Ordinal))
                {
                    sequence = long.Parse(last.Substring(1), CultureInfo.InvariantCulture);
                    Array.Resize(ref parts, parts.Length - 1);
                }
                if (parts.Length < RequiredParts) return null;

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

                if (parts.Length >= PedalLevelsIndex + 2)
                {
                    result.HasPedalLevels = true;
                    result.GasLevel = ParseDouble(parts[PedalLevelsIndex]);
                    result.BrakeLevel = ParseDouble(parts[PedalLevelsIndex + 1]);
                }

                int actionsCount = Math.Max(0, Math.Min(parts.Length - ActionsIndex, maxActions));
                result.ActionCounters = new int[actionsCount];
                for (int i = 0; i < actionsCount; i++)
                {
                    result.ActionCounters[i] = int.Parse(parts[ActionsIndex + i], CultureInfo.InvariantCulture);
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
