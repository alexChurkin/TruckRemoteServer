namespace TruckRemoteServer.Protocol
{
    //Truck state message sent to the controller
    public static class ServerMessage
    {
        public const int LightsOff = 0;
        public const int LightsParking = 1;
        public const int LightsLowBeam = 2;
        public const int LightsHighBeam = 3;

        //High beam without low beam isn't shown as high beam: it is only "flashing" then
        public static int LightsMode(bool parkingLights, bool lowBeam, bool highBeam)
        {
            if (lowBeam) return highBeam ? LightsHighBeam : LightsLowBeam;
            return parkingLights ? LightsParking : LightsOff;
        }

        //Base values are read by all controllers, the additional ones (0/1) only by newer controllers:
        //trailer attached, wipers, beacon, analog pedals available.
        //The message number is the last field, so controllers that don't know it ignore it;
        //new controllers measure packet loss by it
        public static string Format(bool engineOn, bool parkingBrake, bool leftBlinker, bool rightBlinker,
            int lightsMode, int ffbDuration,
            bool trailerAttached, bool wipersOn, bool beaconOn, bool analogPedalsAvailable, long sequence)
        {
            return $"{engineOn},{parkingBrake},{leftBlinker},{rightBlinker},{lightsMode},{ffbDuration}," +
                $"{Bit(trailerAttached)},{Bit(wipersOn)},{Bit(beaconOn)},{Bit(analogPedalsAvailable)}," +
                ControllerMessage.SequenceTag + sequence;
        }

        private static int Bit(bool value)
        {
            return value ? 1 : 0;
        }
    }
}
