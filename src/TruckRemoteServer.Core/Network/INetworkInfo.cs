using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace TruckRemoteServer.Network
{
    public interface INetworkInfo
    {
        //IPv4 addresses of this PC, the most likely ones for the phone first
        IList<IPAddress> GetLocalAddresses();

        //Wi-Fi reconnection or a new DHCP lease may change the addresses (raised on a system thread)
        event EventHandler AddressesChanged;
    }

    public sealed class SystemNetworkInfo : INetworkInfo
    {
        public IList<IPAddress> GetLocalAddresses()
        {
            return NetworkUtil.GetLocalIps().ToList();
        }

        public event EventHandler AddressesChanged
        {
            add { NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(value); }
            remove { NetworkChange.NetworkAddressChanged -= new NetworkAddressChangedEventHandler(value); }
        }
    }
}
