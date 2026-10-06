using System;

namespace TruckRemoteServer.Telemetry
{
    //How rough a surface of the game feels, by the name of its substance ("road", "dirt", "gravel", "grass"...)
    public static class SurfaceRoughness
    {
        //Parts of the names and the roughness they mean, the first match wins: roads are smooth whatever else is
        //in their names (e.g. "road_snow"), unknown substances are smooth too
        private static readonly string[] Names = { "road", "rumble", "gravel", "dirt", "mud", "sand", "grass", "snow" };
        private static readonly float[] Values = { 0, 0, 0.7f, 0.6f, 0.6f, 0.5f, 0.45f, 0.4f };

        public static float Of(string substance)
        {
            if (string.IsNullOrEmpty(substance)) return 0;
            for (int i = 0; i < Names.Length; i++)
            {
                if (substance.IndexOf(Names[i], StringComparison.OrdinalIgnoreCase) >= 0) return Values[i];
            }
            return 0;
        }

        public static bool IsRumbleStrip(string substance) =>
            substance != null && substance.IndexOf("rumble", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
