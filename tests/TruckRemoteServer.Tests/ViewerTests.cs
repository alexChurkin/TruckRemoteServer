using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using TruckRemoteServer.Input;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    //Dashboards of tablets and other phones: they get the truck state, the controller isn't touched
    public sealed class ViewerTests : IDisposable
    {
        private readonly FakeTelemetry telemetry = new FakeTelemetry();
        private readonly ControllerServer server;
        private readonly List<UdpClient> clients = new List<UdpClient>();

        public ViewerTests()
        {
            var joystick = new FakeJoystick();
            server = new ControllerServer(new ControllerInputMapper(new FakeKeyboard(), joystick), telemetry, joystick,
                new NoTimerResolution(), NullLogger<ControllerServer>.Instance);
            Assert.True(server.Start(0));
        }

        public void Dispose()
        {
            server.Stop();
            foreach (UdpClient client in clients) client.Dispose();
        }

        private UdpClient Client()
        {
            var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            client.Client.ReceiveTimeout = 2000;
            clients.Add(client);
            return client;
        }

        private void Send(UdpClient client, string text) => Send(client, Encoding.UTF8.GetBytes(text));

        private void Send(UdpClient client, byte[] bytes) =>
            client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, server.Port));

        private static byte[] Receive(UdpClient client)
        {
            IPEndPoint from = null;
            return client.Receive(ref from);
        }

        //The next truck state (job messages are skipped)
        private static byte[] ReceiveState(UdpClient client)
        {
            while (true)
            {
                byte[] message = Receive(client);
                if (message[0] == BinaryProtocol.StateType) return message;
            }
        }

        [Fact]
        public void ViewerGetsTheTruckStateWithoutBecomingTheController()
        {
            telemetry.Truck = new TruckTelemetry { Available = true, EngineOn = true, Speed = 20 };
            UdpClient tablet = Client();
            Send(tablet, "TruckRemoteViewer2");

            Assert.Equal("Hi!2", Encoding.UTF8.GetString(Receive(tablet)));
            byte[] state = ReceiveState(tablet);
            Assert.Equal(40, state.Length);
            Assert.Equal(1 | 1 << 10, BitConverter.ToUInt16(state, 5) & (1 | 1 << 10));
            Assert.Equal(2000, BitConverter.ToInt16(state, 9));
            Assert.Equal(1, server.ViewerCount);
            Assert.False(server.Status.ControllerConnected);
        }

        [Fact]
        public void ViewersAndTheControllerWorkTogether()
        {
            UdpClient tablet = Client();
            UdpClient phone = Client();
            Send(tablet, "TruckRemoteViewer2");
            Send(phone, "TruckRemoteHello2");

            Assert.Equal("Hi!2", Encoding.UTF8.GetString(Receive(phone)));
            Assert.Equal("Hi!2", Encoding.UTF8.GetString(Receive(tablet)));
            ReceiveState(tablet);
            Assert.True(server.Status.ControllerConnected);
        }

        [Fact]
        public void TooManyViewersAreNotAnswered()
        {
            for (int i = 0; i < ControllerServer.MaxViewers; i++)
            {
                UdpClient viewer = Client();
                Send(viewer, "TruckRemoteViewer2");
                Assert.Equal("Hi!2", Encoding.UTF8.GetString(Receive(viewer)));
            }
            UdpClient extra = Client();
            extra.Client.ReceiveTimeout = 300;
            Send(extra, "TruckRemoteViewer2");

            Assert.Throws<SocketException>(() => Receive(extra));
            Assert.Equal(ControllerServer.MaxViewers, server.ViewerCount);
        }

        [Fact]
        public void GoodbyeOfAViewerForgetsIt()
        {
            UdpClient tablet = Client();
            Send(tablet, "TruckRemoteViewer2");
            Receive(tablet);
            Send(tablet, new[] { BinaryProtocol.GoodbyeType });

            for (int i = 0; i < 100 && server.ViewerCount > 0; i++) Thread.Sleep(20);
            Assert.Equal(0, server.ViewerCount);
        }
    }
}
