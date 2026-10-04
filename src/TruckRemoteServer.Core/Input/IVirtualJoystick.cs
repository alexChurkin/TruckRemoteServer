using System;

namespace TruckRemoteServer.Input
{
    public static class JoystickAxis
    {
        public const int CENTER = 16384;
        public const int MAX = 32767;
    }

    //Virtual joystick (vJoy): steering on the X axis, gas and brake on the Y and Z axes
    public interface IVirtualJoystick
    {
        bool IsAvailable { get; }

        //Analog pedals need the Y and Z axes enabled in the vJoy configuration
        bool HasPedalAxes { get; }

        //Returns false if vJoy isn't installed or the device is busy
        bool Initialize();

        //0..32768
        void SetSteering(int value);

        //0..32767 each
        void SetPedals(int gas, int brake);

        void Release();

        //Force feedback effect of the game, its magnitude is used as the vibration duration (ms)
        event Action<uint> ForceFeedback;
    }
}
