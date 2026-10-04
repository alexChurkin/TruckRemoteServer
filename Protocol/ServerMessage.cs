namespace TruckRemoteServer.Protocol
{
    //Truck state message sent to the controller
    public static class ServerMessage
    {
        public const int LIGHTS_OFF = 0;
        public const int LIGHTS_PARKING = 1;
        public const int LIGHTS_LOW_BEAM = 2;
        public const int LIGHTS_HIGH_BEAM = 3;

        //High beam without low beam isn't shown as high beam: it is only "flashing" then
        public static int LightsMode(bool parkingLights, bool lowBeam, bool highBeam)
        {
            if (lowBeam) return highBeam ? LIGHTS_HIGH_BEAM : LIGHTS_LOW_BEAM;
            return parkingLights ? LIGHTS_PARKING : LIGHTS_OFF;
        }

        //Base values are read by all controllers, the additional ones (0/1) only by newer controllers:
        //trailer attached, wipers, beacon, analog pedals available
        public static string Format(bool engineOn, bool parkingBrake, bool leftBlinker, bool rightBlinker,
            int lightsMode, int ffbDuration,
            bool trailerAttached, bool wipersOn, bool beaconOn, bool analogPedalsAvailable)
        {
            return $"{engineOn},{parkingBrake},{leftBlinker},{rightBlinker},{lightsMode},{ffbDuration}," +
                $"{Bit(trailerAttached)},{Bit(wipersOn)},{Bit(beaconOn)},{Bit(analogPedalsAvailable)}";
        }

        private static int Bit(bool value)
        {
            return value ? 1 : 0;
        }
    }
}
