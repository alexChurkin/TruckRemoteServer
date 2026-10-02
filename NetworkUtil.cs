using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TruckRemoteServer
{
    class NetworkUtil
    {
        //Returns IPv4 address of the active LAN adapter.
        //Adapters with default gateway are preferred (virtual adapters usually don't have it)
        public static IPAddress GetLocalIp()
        {
            IPAddress fallback = null;

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up
                    || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                IPInterfaceProperties properties = adapter.GetIPProperties();
                bool hasGateway = properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork
                    && !gateway.Address.Equals(IPAddress.Any));

                foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
                {
                    IPAddress address = info.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }
                    if (hasGateway) return address;
                    if (fallback == null) fallback = address;
                }
            }
            return fallback;
        }
    }
}
