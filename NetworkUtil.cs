using System.Net;

namespace TruckRemoteServer
{
    class NetworkUtil
    {
        public static IPAddress GetLocalIp()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addresses = ipEntry.AddressList;
            IPAddress address = null;

            if (addresses.Length > 0)
            {
                address = addresses[0];
            }

            if (addresses.Length > 1)
            {
                foreach (IPAddress iAddr in addresses)
                {
                    string addressString = iAddr.ToString();
                    if (addressString.StartsWith("192"))
                    {
                        address = iAddr;
                        break;
                    }
                }
            }
            return address;
        }
    }
}
