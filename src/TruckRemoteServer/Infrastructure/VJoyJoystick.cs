using System;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Input;
using vJoyInterfaceWrap;

namespace TruckRemoteServer.Infrastructure
{
    //vJoy device 1: steering on X, gas on Y, brake on Z (Y and Z are optional, for analog pedals)
    public sealed class VJoyJoystick : IVirtualJoystick
    {
        private const uint DeviceId = 1;
        private const int ErrorSuccess = 0;

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

                VjdStat status = device.GetVJDStatus(DeviceId);
                bool acquiredNow = status == VjdStat.VJD_STAT_OWN
                    || (status == VjdStat.VJD_STAT_FREE && device.AcquireVJD(DeviceId));
                if (!acquiredNow)
                {
                    logger.LogWarning("Failed to acquire vJoy device {Id} (status: {Status})", DeviceId, status);
                    return false;
                }

                device.ResetVJD(DeviceId);
                pedalAxesExist = device.GetVJDAxisExist(DeviceId, HID_USAGES.HID_USAGE_Y)
                    && device.GetVJDAxisExist(DeviceId, HID_USAGES.HID_USAGE_Z);
                if (forceFeedbackCallback == null)
                {
                    forceFeedbackCallback = OnForceFeedback;
                    device.FfbRegisterGenCB(forceFeedbackCallback, DeviceId);
                }
                acquired = true;
                logger.LogInformation("vJoy device {Id} acquired, pedal axes: {Pedals}", DeviceId, pedalAxesExist);
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
            device.SetAxis(value, DeviceId, HID_USAGES.HID_USAGE_X);
        }

        public void SetPedals(int gas, int brake)
        {
            if (!HasPedalAxes) return;
            device.SetAxis(gas, DeviceId, HID_USAGES.HID_USAGE_Y);
            device.SetAxis(brake, DeviceId, HID_USAGES.HID_USAGE_Z);
        }

        public void Release()
        {
            if (device == null || !acquired) return;
            acquired = false;
            device.RelinquishVJD(DeviceId);
        }

        private void OnForceFeedback(IntPtr data, object userData)
        {
            var effect = new vJoy.FFB_EFF_CONSTANT();
            if (device.Ffb_h_Eff_Constant(data, ref effect) == ErrorSuccess)
            {
                ForceFeedback?.Invoke((uint)Math.Abs(effect.Magnitude));
            }
        }
    }
}
