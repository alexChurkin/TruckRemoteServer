using System;
using System.Runtime.InteropServices;
using TruckRemoteServer.Input;

namespace TruckRemoteServer.Infrastructure
{
    //The game controllers as DirectInput lists them: the games (ETS2 and ATS) name a controller in their files by
    //its DirectInput instance and product ids, so the name of vJoy is known before a game has seen it
    public static class DirectInputDevices
    {
        private const int DirectInputVersion = 0x0800;
        private const int GameControllers = 4;      //DI8DEVCLASS_GAMECTRL
        private const int AttachedOnly = 1;         //DIEDFL_ATTACHEDONLY
        //DIDEVICEINSTANCE: the size, then the instance and the product ids
        private const int InstanceOffset = 4;
        private const int ProductOffset = 20;

        //The name of the vJoy device as the games write it, null if there is no vJoy device.
        //Of several vJoy devices it's the one DirectInput lists first
        public static string FindVJoy()
        {
            string found = null;
            try
            {
                Guid interfaceId = typeof(IDirectInput8).GUID;
                if (DirectInput8Create(GetModuleHandle(null), DirectInputVersion, ref interfaceId,
                    out IDirectInput8 directInput, IntPtr.Zero) != 0)
                {
                    return null;
                }
                try
                {
                    EnumDevicesCallback callback = (device, reference) =>
                    {
                        var instance = Marshal.PtrToStructure<Guid>(device + InstanceOffset);
                        var product = Marshal.PtrToStructure<Guid>(device + ProductOffset);
                        if (!GameControlsFile.IsVJoy(product)) return true;
                        found = GameControlsFile.DeviceName(instance, product);
                        return false;
                    };
                    directInput.EnumDevices(GameControllers, callback, IntPtr.Zero, AttachedOnly);
                    GC.KeepAlive(callback);
                }
                finally
                {
                    Marshal.ReleaseComObject(directInput);
                }
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException
                || e is COMException || e is InvalidCastException)
            {
                //No DirectInput: the game tells the name once it has seen vJoy
            }
            return found;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumDevicesCallback(IntPtr device, IntPtr reference);

        //Only the methods up to the one that is called (the order is the order of the interface)
        [ComImport]
        [Guid("BF798031-483A-4DA2-AA99-5D64ED369700")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirectInput8
        {
            [PreserveSig]
            int CreateDevice(ref Guid id, out IntPtr device, IntPtr outer);

            [PreserveSig]
            int EnumDevices(int deviceType, [MarshalAs(UnmanagedType.FunctionPtr)] EnumDevicesCallback callback,
                IntPtr reference, int flags);
        }

        [DllImport("dinput8.dll")]
        private static extern int DirectInput8Create(IntPtr instance, int version, ref Guid interfaceId,
            [MarshalAs(UnmanagedType.Interface)] out IDirectInput8 directInput, IntPtr outer);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string moduleName);
    }
}
