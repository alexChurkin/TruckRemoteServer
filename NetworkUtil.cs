using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TruckRemoteServer
{
    public class LocalAddress
    {
        public IPAddress Address;
        //Adapters with a default gateway are the real LAN connections
        public bool HasGateway;
        //Virtual machines, VPNs and similar adapters, a phone usually can't reach them
        public bool IsVirtual;
    }

    public static class NetworkUtil
    {
        private static readonly string[] VIRTUAL_ADAPTER_WORDS =
        {
            "virtual", "hyper-v", "vmware", "virtualbox", "vpn", "tap-", "wireguard", "tailscale",
            "zerotier", "hamachi", "npcap", "loopback", "bluetooth"
        };

        //IPv4 addresses of this PC, the most likely ones for the phone first
        public static List<IPAddress> GetLocalIps()
        {
            return Order(Collect()).Select(address => address.Address).ToList();
        }

        public static IEnumerable<LocalAddress> Order(IEnumerable<LocalAddress> addresses)
        {
            return addresses
                .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(address.Address)
                    && !IsLinkLocal(address.Address))
                .OrderBy(address => address.IsVirtual)
                .ThenByDescending(address => address.HasGateway)
                .ThenByDescending(address => IsPrivate(address.Address));
        }

        private static IEnumerable<LocalAddress> Collect()
        {
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
                string names = (adapter.Name + " " + adapter.Description).ToLowerInvariant();
                bool isVirtual = VIRTUAL_ADAPTER_WORDS.Any(names.Contains);

                foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
                {
                    yield return new LocalAddress { Address = info.Address, HasGateway = hasGateway, IsVirtual = isVirtual };
                }
            }
        }

        //169.254.x.x is assigned when DHCP didn't answer, nothing can connect to it
        private static bool IsLinkLocal(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 169 && bytes[1] == 254;
        }

        private static bool IsPrivate(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }
    }
}
