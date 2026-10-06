using System.Text.RegularExpressions;

namespace TruckRemoteServer.Telemetry
{
    //The speed units the player has chosen in each game (the config.cfg of the profile played last):
    //the controller shows the speed in them unless its user has chosen otherwise
    public sealed class GameUnits
    {
        private static readonly Regex MphSetting =
            new Regex(@"^\s*uset\s+g_mph\s+""(\d+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant);

        private readonly object unitsLock = new object();
        private bool? ets2Mph;
        private bool? atsMph;

        //null - unknown (the setting wasn't found or read)
        public bool? Mph(int game)
        {
            lock (unitsLock)
            {
                switch (game)
                {
                    case TruckTelemetry.GameEts2: return ets2Mph;
                    case TruckTelemetry.GameAts: return atsMph;
                    default: return null;
                }
            }
        }

        public void Set(int game, bool? mph)
        {
            lock (unitsLock)
            {
                if (game == TruckTelemetry.GameEts2) ets2Mph = mph;
                else if (game == TruckTelemetry.GameAts) atsMph = mph;
            }
        }

        //The setting of a profile's config.cfg: uset g_mph "1" - miles per hour, "0" - km/h; null if it isn't there
        public static bool? ParseMph(string config)
        {
            if (string.IsNullOrEmpty(config)) return null;
            Match match = MphSetting.Match(config);
            if (!match.Success) return null;
            return match.Groups[1].Value != "0";
        }
    }
}
