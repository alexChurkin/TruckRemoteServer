using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using TruckRemoteServer.Haptics;
using TruckRemoteServer.Input;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    //Real UDP packets on the loopback interface
    public sealed class ControllerServerTests : IDisposable
    {
        private const string StateTemplate = "0,false,{0},{1},false,false,false,false,0,false,0,0,0,0,0,0,0,0,0,0,#{2}";

        private readonly FakeKeyboard keyboard = new FakeKeyboard();
        private readonly FakeJoystick joystick = new FakeJoystick();
        private readonly FakeTelemetry telemetry = new FakeTelemetry();
        private readonly ControllerServer server;
        private readonly List<ServerStatus> statuses = new List<ServerStatus>();
        private readonly UdpClient phone = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));

        public ControllerServerTests()
        {
            var mapper = new ControllerInputMapper(keyboard, joystick);
            server = new ControllerServer(mapper, telemetry, joystick, new NoTimerResolution(),
                NullLogger<ControllerServer>.Instance);
            server.StatusChanged += status =>
            {
                lock (statuses) statuses.Add(status);
            };
            Assert.True(server.Start(0));
            phone.Client.ReceiveTimeout = 2000;
        }

        public void Dispose()
        {
            server.Stop();
            phone.Dispose();
        }

        private void Send(string text, UdpClient from = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            (from ?? phone).Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, server.Port));
        }

        private static string Receive(UdpClient client)
        {
            IPEndPoint from = null;
            return Encoding.UTF8.GetString(client.Receive(ref from));
        }

        //The condition is checked once per try: it may consume received messages
        private static void WaitFor(Func<bool> condition)
        {
            for (int i = 0; i < 100; i++)
            {
                if (condition()) return;
                Thread.Sleep(20);
            }
            Assert.Fail("The condition wasn't met in 2 seconds");
        }

        private void Connect()
        {
            Send("TruckRemoteHello");
            Assert.Equal("Hi!", Receive(phone));
            WaitFor(() => server.Status.ControllerConnected);
        }

        [Fact]
        public void HelloConnectsAndTruckStateIsSentWithNumbers()
        {
            telemetry.Truck = new TruckTelemetry { EngineOn = true, LowBeam = true };
            Connect();

            string first = Receive(phone);
            string second = Receive(phone);

            Assert.StartsWith("True,False,False,False,2,0,0,0,0,1,#", first);
            long a = long.Parse(first.Split('#')[1]);
            long b = long.Parse(second.Split('#')[1]);
            Assert.True(b > a);
        }

        [Fact]
        public void Version2HelloSwitchesToTheBinaryProtocol()
        {
            telemetry.Truck = new TruckTelemetry { EngineOn = true, LowBeam = true };
            Send("TruckRemoteHello2");
            Assert.Equal("Hi!2", Receive(phone));
            WaitFor(() => server.Status.ControllerConnected);

            IPEndPoint from = null;
            byte[] truck = phone.Receive(ref from);
            //The dashboard and the haptics
            Assert.Equal(43 + 3 * HapticDetector.EventCount, truck.Length);
            Assert.Equal(1 | 2 << 8, BitConverter.ToUInt16(truck, 5) & (1 | 3 << 8));

            //First state is synchronized, the second one clicks the engine and holds the engine brake
            SendBytes(BinaryState(1, flags: 0, 1, 0));
            SendBytes(BinaryState(2, flags: 1 << 1, 1, 1, 11, 1));
            WaitFor(() => keyboard.Events.Contains("click Engine") && keyboard.Events.Contains("press EngineBrake"));
            Assert.Contains("press Gas", keyboard.Events);

            //The actions without keys come once a second, together with the job
            keyboard.Unbound.Add(GameKey.Map);
            byte[] unbound;
            do unbound = phone.Receive(ref from);
            while (unbound[0] != BinaryProtocol.UnboundActionsType || unbound.Length < 3);
            Assert.Equal(new byte[] { BinaryProtocol.UnboundActionsType, 1, 19 }, unbound);

            //Binary pause releases everything
            SendBytes(new byte[] { 0x03 });
            WaitFor(() => server.Status.ControllerPaused);
            Assert.Contains("release EngineBrake", keyboard.Events);
        }

        private void SendBytes(byte[] bytes)
        {
            phone.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, server.Port));
        }

        private static byte[] BinaryState(uint sequence, int flags, params byte[] actions)
        {
            var message = new byte[16 + actions.Length];
            message[0] = 0x02;
            BitConverter.GetBytes(sequence).CopyTo(message, 1);
            BitConverter.GetBytes((ushort)flags).CopyTo(message, 9);
            message[15] = (byte)(actions.Length / 2);
            actions.CopyTo(message, 16);
            return message;
        }

        [Fact]
        public void StateMessagesBecomeInput()
        {
            Connect();
            Send(string.Format(StateTemplate, "false", "false", 1));
            Send(string.Format(StateTemplate, "true", "true", 2));

            WaitFor(() => keyboard.Events.Contains("click LeftBlinker"));
            Assert.Contains("press Gas", keyboard.Events);
        }

        [Fact]
        public void LateMessageDoesntMakeAnExtraClick()
        {
            Connect();
            Send(string.Format(StateTemplate, "false", "false", 1));
            Send(string.Format(StateTemplate, "false", "true", 3));
            //Came late: its old toggle value would be one more click
            Send(string.Format(StateTemplate, "false", "false", 2));
            Send(string.Format(StateTemplate, "false", "true", 4));
            Thread.Sleep(200);

            Assert.Single(keyboard.Events, e => e == "click LeftBlinker");
        }

        [Fact]
        public void PauseAndGoodbyeReleaseControls()
        {
            Connect();
            Send(string.Format(StateTemplate, "false", "false", 1));
            Send(string.Format(StateTemplate, "true", "false", 2));
            WaitFor(() => keyboard.Events.Contains("press Gas"));

            Send("paused");
            WaitFor(() => server.Status.ControllerPaused);
            Assert.Contains("release Gas", keyboard.Events);

            Send("goodbye");
            WaitFor(() => !server.Status.ControllerConnected);
        }

        [Fact]
        public void AnotherDeviceWaitsUntilTheControllerIsSilent()
        {
            Connect();
            using (var other = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0)))
            {
                other.Client.ReceiveTimeout = 500;
                Send("TruckRemoteHello", other);
                Assert.Throws<SocketException>(() => Receive(other));
            }
        }

        [Fact]
        public void SameControllerResumesWithoutReleasingControls()
        {
            Connect();
            Send(string.Format(StateTemplate, "false", "false", 1));
            Send(string.Format(StateTemplate, "true", "false", 2));
            WaitFor(() => keyboard.Events.Contains("press Gas"));

            Send("TruckRemoteHello");
            WaitFor(() => ReceiveAll().Contains("Hi!"));

            Assert.DoesNotContain("release Gas", keyboard.Events);
            Assert.True(server.Status.ControllerConnected);
        }

        [Fact]
        public void SilentControllerIsDisconnected()
        {
            Connect();
            Send(string.Format(StateTemplate, "false", "false", 1));
            Send(string.Format(StateTemplate, "true", "false", 2));
            WaitFor(() => keyboard.Events.Contains("press Gas"));

            Thread.Sleep(ControllerServer.ControllerTimeout + 500);

            Assert.False(server.Status.ControllerConnected);
            Assert.Contains("release Gas", keyboard.Events);
        }

        [Fact]
        public void ForceFeedbackIsSentOnce()
        {
            Connect();
            joystick.RaiseForceFeedback(150);

            WaitFor(() => ReceiveAll().Any(m => m.Split(',')[5] == "150"));
        }

        [Fact]
        public void BusyPortCantBeUsed()
        {
            var second = new ControllerServer(new ControllerInputMapper(keyboard, joystick), telemetry, joystick,
                new NoTimerResolution(), NullLogger<ControllerServer>.Instance);
            Assert.False(second.Start(server.Port));
        }

        private List<string> ReceiveAll()
        {
            var messages = new List<string>();
            while (phone.Available > 0) messages.Add(Receive(phone));
            return messages;
        }
    }
}
