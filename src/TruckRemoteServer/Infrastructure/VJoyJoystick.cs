using System;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Input;
using vJoyInterfaceWrap;

namespace TruckRemoteServer.Infrastructure
{
    //vJoy device 1: steering on X, gas on Y, brake on Z (Y and Z are optional, for analog pedals)
    public sealed class VJoyJoystick : IVirtualJoystick
    {
        private const uint DEVICE_ID = 1;
        private const int ERROR_SUCCESS = 0;

        private readonly ILogger<VJoyJoystick> logger;
        private readonly object initLock = new object();
        private vJoy device;
        private volatile bool acquired;
        private volatile bool pedalAxesExist;
        //Native code keeps the callback: the delegate must not be collected
        private vJoy.FfbCbFunc forceFeedbackCallback;

        public VJoyJoystick(ILogger<VJoyJoystick> logger)
        {
            this.logger = logger;
        }

        public event Action<uint> ForceFeedback;

        public bool IsAvailable => acquired;

        public bool HasPedalAxes => acquired && pedalAxesExist;

        //Called by the network thread when the phone connects and by the window after vJoy setup
        public bool Initialize()
        {
            lock (initLock)
            {
                return InitializeDevice();
            }
        }

        private bool InitializeDevice()
        {
            try
            {
                if (device == null) device = new vJoy();

                if (!device.vJoyEnabled())
                {
                    logger.LogWarning("vJoy driver is not enabled");
                    return false;
                }

                VjdStat status = device.GetVJDStatus(DEVICE_ID);
                bool acquiredNow = status == VjdStat.VJD_STAT_OWN
                    || (status == VjdStat.VJD_STAT_FREE && device.AcquireVJD(DEVICE_ID));
                if (!acquiredNow)
                {
                    logger.LogWarning("Failed to acquire vJoy device {Id} (status: {Status})", DEVICE_ID, status);
                    return false;
                }

                device.ResetVJD(DEVICE_ID);
                pedalAxesExist = device.GetVJDAxisExist(DEVICE_ID, HID_USAGES.HID_USAGE_Y)
                    && device.GetVJDAxisExist(DEVICE_ID, HID_USAGES.HID_USAGE_Z);
                if (forceFeedbackCallback == null)
                {
                    forceFeedbackCallback = OnForceFeedback;
                    device.FfbRegisterGenCB(forceFeedbackCallback, DEVICE_ID);
                }
                acquired = true;
                logger.LogInformation("vJoy device {Id} acquired, pedal axes: {Pedals}", DEVICE_ID, pedalAxesExist);
                return true;
            }
            catch (Exception e)
            {
                //vJoy is not installed
                logger.LogWarning("vJoy initialization failed: {Message}", e.Message);
                return false;
            }
        }

        public void SetSteering(int value)
        {
            if (!acquired) return;
            device.SetAxis(value, DEVICE_ID, HID_USAGES.HID_USAGE_X);
        }

        public void SetPedals(int gas, int brake)
        {
            if (!HasPedalAxes) return;
            device.SetAxis(gas, DEVICE_ID, HID_USAGES.HID_USAGE_Y);
            device.SetAxis(brake, DEVICE_ID, HID_USAGES.HID_USAGE_Z);
        }

        public void Release()
        {
            if (device == null || !acquired) return;
            acquired = false;
            device.RelinquishVJD(DEVICE_ID);
        }

        private void OnForceFeedback(IntPtr data, object userData)
        {
            var effect = new vJoy.FFB_EFF_CONSTANT();
            if (device.Ffb_h_Eff_Constant(data, ref effect) == ERROR_SUCCESS)
            {
                ForceFeedback?.Invoke((uint)Math.Abs(effect.Magnitude));
            }
        }
    }
}
