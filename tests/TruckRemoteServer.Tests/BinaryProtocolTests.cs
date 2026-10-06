using System;
using System.Text;
using TruckRemoteServer.Haptics;
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
                FuelCapacity = 800,
                AirPressureWarning = true,
                WaterTemperatureWarning = true,
                FuelWarning = true,
                LiftAxle = true,
                RetarderLevel = 2,
                RetarderStepCount = 4,
                Wear = 0.234f,
                RestStopMinutes = -40,
                RouteDistance = 128_400.6f,
                RouteTime = 6300.2f,
                FuelRange = 85.4f
            };
            byte[] message = BinaryProtocol.FormatServerState(truck, lightsMode: 3, ffbDuration: 100000,
                analogPedalsAvailable: true, sequence: 0x100000007);

            Assert.Equal(40, message.Length);
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
            Assert.Equal(1 | 1 << 3 | 1 << 6 | 1 << 8, BitConverter.ToUInt16(message, 22));
            Assert.Equal(2, message[24]);
            Assert.Equal(4, message[25]);
            Assert.Equal(23, message[26]);
            Assert.Equal(-40, BitConverter.ToInt16(message, 27));
            Assert.Equal(128_401u, BitConverter.ToUInt32(message, 29));
            Assert.Equal(6300u, BitConverter.ToUInt32(message, 33));
            Assert.Equal(BinaryProtocol.Revision, message[37]);
            Assert.Equal(85, BitConverter.ToUInt16(message, 38));
        }

        [Fact]
        public void HapticsFollowTheState()
        {
            var haptics = new HapticDetector();
            var truck = new TruckTelemetry { Available = true };
            haptics.Update(truck);
            haptics.Update(new TruckTelemetry { Available = true, EngineOn = true, FinedToggle = true });

            byte[] message = BinaryProtocol.FormatServerState(truck, 0, 0, false, 1, haptics);

            Assert.Equal(43 + 3 * HapticDetector.EventCount, message.Length);
            Assert.Equal(0, message[40]);
            Assert.Equal((byte)HapticSurface.Road, message[41]);
            Assert.Equal(HapticDetector.EventCount, message[42]);
            for (int i = 0; i < HapticDetector.EventCount; i++)
            {
                var kind = (HapticEvent)message[43 + i * 3];
                Assert.Equal(i + 1, (int)kind);
                bool happened = kind == HapticEvent.EngineStart || kind == HapticEvent.Fine;
                Assert.Equal(happened ? 1 : 0, message[44 + i * 3]);
                Assert.Equal(happened ? 255 : 0, message[45 + i * 3]);
            }
        }

        [Fact]
        public void JobIsFormattedWithUtf8Texts()
        {
            var truck = new TruckTelemetry { Cargo = "Брёвна", DestinationCity = "Berlin", DeliveryMinutesLeft = -20 };

            byte[] message = BinaryProtocol.FormatJob(truck);

            Assert.Equal(BinaryProtocol.JobType, message[0]);
            Assert.Equal(-20, BitConverter.ToInt32(message, 1));
            int cargoLength = message[5];
            Assert.Equal("Брёвна", Encoding.UTF8.GetString(message, 6, cargoLength));
            int cityLength = message[6 + cargoLength];
            Assert.Equal("Berlin", Encoding.UTF8.GetString(message, 7 + cargoLength, cityLength));
            Assert.Equal(7 + cargoLength + cityLength, message.Length);
        }

        [Fact]
        public void NoJobHasEmptyTexts()
        {
            Assert.Equal(new byte[] { BinaryProtocol.JobType, 0, 0, 0, 0, 0, 0 },
                BinaryProtocol.FormatJob(new TruckTelemetry { DeliveryMinutesLeft = 300 }));
        }

        [Fact]
        public void LongJobTextIsCutBetweenCharacters()
        {
            var truck = new TruckTelemetry { Cargo = new string('ж', 40), DestinationCity = "X" };

            byte[] message = BinaryProtocol.FormatJob(truck);

            //Two bytes per character: 64 bytes are 32 whole characters
            Assert.Equal(64, message[5]);
            Assert.Equal(new string('ж', 32), Encoding.UTF8.GetString(message, 6, 64));
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
