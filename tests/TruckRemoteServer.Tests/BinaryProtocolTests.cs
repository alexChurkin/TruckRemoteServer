using System;
using TruckRemoteServer.Protocol;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class BinaryProtocolTests
    {
        //The same bytes as BinaryProtocol.encodeState of the Android app makes
        private static byte[] State(uint sequence, float steering, int flags, ushort gas, ushort brake,
            params byte[] actions)
        {
            var message = new byte[16 + actions.Length];
            message[0] = 0x02;
            BitConverter.GetBytes(sequence).CopyTo(message, 1);
            BitConverter.GetBytes(steering).CopyTo(message, 5);
            BitConverter.GetBytes((ushort)flags).CopyTo(message, 9);
            BitConverter.GetBytes(gas).CopyTo(message, 11);
            BitConverter.GetBytes(brake).CopyTo(message, 13);
            message[15] = (byte)(actions.Length / 2);
            actions.CopyTo(message, 16);
            return message;
        }

        [Fact]
        public void ControllerStateIsReadWithoutLosingPrecision()
        {
            byte[] message = State(0xFFFFFFFE, -1.2345678f, 1 | 1 << 7 | 1 << 8 | 2 << 9, 39321, 65535, 1, 3, 11, 1);

            ControllerMessage state = BinaryProtocol.ParseControllerState(message, message.Length);

            Assert.Equal(0xFFFFFFFEL, state.Sequence);
            Assert.Equal(-1.2345678f, (float)state.Steering);
            Assert.True(state.BrakePressed);
            Assert.False(state.GasPressed);
            Assert.True(state.CruiseClick);
            Assert.True(state.HasPedalLevels);
            Assert.Equal(2, state.Horn);
            Assert.Equal(0.6, state.GasLevel, 5);
            Assert.Equal(1.0, state.BrakeLevel);
            Assert.Equal(3, state.Actions[1]);
            Assert.Equal(1, state.Actions[11]);
        }

        [Fact]
        public void TruncatedStateIsRejected()
        {
            byte[] message = State(1, 0, 0, 0, 0, 1, 3);

            Assert.Null(BinaryProtocol.ParseControllerState(message, 15));
            Assert.Null(BinaryProtocol.ParseControllerState(message, 17));
            Assert.NotNull(BinaryProtocol.ParseControllerState(message, 18));
        }

        [Fact]
        public void NotFiniteSteeringBecomesZero()
        {
            byte[] message = State(1, float.NaN, 0, 0, 0);

            Assert.Equal(0, BinaryProtocol.ParseControllerState(message, message.Length).Steering);
        }

        [Fact]
        public void ServerStateHasFlagsAndTheDashboard()
        {
            var truck = new TruckTelemetry
            {
                Available = true,
                Game = TruckTelemetry.GameAts,
                EngineOn = true,
                RightBlinker = true,
                TrailerAttached = true,
                Beacon = true,
                Speed = -2.5f,
                SpeedLimit = 24.587f,
                CruiseSpeed = 22.2222f,
                Gear = -1,
                EngineRpm = 1499.6f,
                EngineRpmMax = 2500,
                Fuel = 300,
                FuelCapacity = 800
            };
            byte[] message = BinaryProtocol.FormatServerState(truck, lightsMode: 3, ffbDuration: 100000,
                analogPedalsAvailable: true, sequence: 0x100000007);

            Assert.Equal(22, message.Length);
            Assert.Equal(0x02, message[0]);
            Assert.Equal(7u, BitConverter.ToUInt32(message, 1));
            Assert.Equal(1 | 1 << 3 | 1 << 4 | 1 << 6 | 1 << 7 | 3 << 8 | 1 << 10, BitConverter.ToUInt16(message, 5));
            //Clamped to the field
            Assert.Equal(ushort.MaxValue, BitConverter.ToUInt16(message, 7));
            Assert.Equal(-250, BitConverter.ToInt16(message, 9));
            Assert.Equal(2459, BitConverter.ToUInt16(message, 11));
            Assert.Equal(2222, BitConverter.ToUInt16(message, 13));
            Assert.Equal(-1, (sbyte)message[15]);
            Assert.Equal(1500, BitConverter.ToUInt16(message, 16));
            Assert.Equal(2500, BitConverter.ToUInt16(message, 18));
            Assert.Equal(38, message[20]);
            Assert.Equal(TruckTelemetry.GameAts, message[21]);
        }

        [Fact]
        public void UnknownTruckHasNoTelemetryFlag()
        {
            byte[] message = BinaryProtocol.FormatServerState(TruckTelemetry.Unknown, 0, 0, false, 1);

            Assert.Equal(0, BitConverter.ToUInt16(message, 5) & 1 << 10);
            Assert.Equal(0, message[20]);
        }

        [Fact]
        public void TextMessagesAreNotBinary()
        {
            byte[] text = System.Text.Encoding.UTF8.GetBytes("TruckRemoteHello2");

            Assert.False(BinaryProtocol.IsBinary(text, text.Length));
            Assert.True(BinaryProtocol.IsBinary(new byte[] { 0x03 }, 1));
        }
    }
}
