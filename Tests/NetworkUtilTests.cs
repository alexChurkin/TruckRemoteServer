using System.Linq;
using System.Net;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class NetworkUtilTests
    {
        private static LocalAddress Address(string ip, bool gateway = false, bool isVirtual = false)
        {
            return new LocalAddress { Address = IPAddress.Parse(ip), HasGateway = gateway, IsVirtual = isVirtual };
        }

        [Fact]
        public void RealLanAdapterIsFirstAndUselessAddressesAreRemoved()
        {
            var ordered = NetworkUtil.Order(new[]
            {
                Address("172.20.0.1", isVirtual: true),
                Address("169.254.10.20"),
                Address("127.0.0.1"),
                Address("fe80::1"),
                Address("10.8.0.2", gateway: true, isVirtual: true),
                Address("192.168.1.35", gateway: true),
                Address("192.168.56.1"),
            }).Select(address => address.Address.ToString()).ToList();

            Assert.Equal(new[] { "192.168.1.35", "192.168.56.1", "10.8.0.2", "172.20.0.1" }, ordered);
        }
    }
}
