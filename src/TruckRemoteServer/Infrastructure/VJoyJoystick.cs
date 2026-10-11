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
        private const int ReacquireIntervalMs = 2000;
        private static readonly HID_USAGES[] RotationAxes =
        {
            HID_USAGES.HID_USAGE_RX, HID_USAGES.HID_USAGE_RY, HID_USAGES.HID_USAGE_RZ
        };

        private readonly ILogger<VJoyJoystick> logger;
        private readonly object initLock = new object();
        private vJoy device;
        private volatile bool acquired;
        private volatile bool pedalAxesExist;
        //The server is closing: the device isn't taken back
        private volatile bool released;
        private int lastReacquireTicks = Environment.TickCount - ReacquireIntervalMs;
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
                CenterRotationAxes();
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

        //The reset leaves the rotation axes at 0, an end of their range. The server doesn't use them, but a device
        //that has them (the default one of vJoy) is also a controller of the game's menus and cameras (ui_joy of
        //the game looks around with rx and ry whatever the profile binds), and the camera would keep turning left
        //and up: they rest in the middle
        private void CenterRotationAxes()
        {
            foreach (HID_USAGES axis in RotationAxes)
            {
                if (device.GetVJDAxisExist(DeviceId, axis)) device.SetAxis(JoystickAxis.Center, DeviceId, axis);
            }
        }

        public void SetSteering(int value)
        {
            if (!acquired && !Reacquire()) return;
            if (!device.SetAxis(value, DeviceId, HID_USAGES.HID_USAGE_X)) OnDeviceLost();
        }

        public void SetPedals(int gas, int brake)
        {
            if (!HasPedalAxes) return;
            if (!device.SetAxis(gas, DeviceId, HID_USAGES.HID_USAGE_Y)
                || !device.SetAxis(brake, DeviceId, HID_USAGES.HID_USAGE_Z))
            {
                OnDeviceLost();
            }
        }

        public void Release()
        {
            if (device == null || !acquired) return;
            acquired = false;
            released = true;
            device.RelinquishVJD(DeviceId);
        }

        //The driver doesn't tell that the device isn't ours anymore (it was reconfigured or restarted, another
        //program took and left it): the axes just stop being set, so the device is acquired again
        private void OnDeviceLost()
        {
            if (!acquired) return;
            acquired = false;
            logger.LogWarning("vJoy device {Id} was lost", DeviceId);
        }

        //Not on every message of the phone: a missing device is asked for once in a while
        private bool Reacquire()
        {
            if (released) return false;
            int now = Environment.TickCount;
            if (unchecked(now - lastReacquireTicks) < ReacquireIntervalMs) return false;
            lastReacquireTicks = now;
            return Initialize();
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
