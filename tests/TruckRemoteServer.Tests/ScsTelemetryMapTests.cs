using System;
using System.Text;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    //Offsets are checked against scsTelemetryMap_t of scs-sdk-plugin 1.12.1 (computed from the header with C layout rules)
    public class ScsTelemetryMapTests
    {
        private static byte[] ActiveMap(int revision = ScsTelemetryMap.Revision)
        {
            var data = new byte[32 * 1024];
            data[0] = 1;
            BitConverter.GetBytes(revision).CopyTo(data, 40);
            return data;
        }

        [Fact]
        public void TruckValuesAreReadFromPluginOffsets()
        {
            byte[] data = ActiveMap();
            data[1566] = 1; //parkBrake
            data[1576] = 1; //engineEnabled
            data[1580] = 1; //blinkerLeftOn
            data[1583] = 1; //lightsBeamLow
            data[1585] = 1; //lightsBeacon
            data[6080] = 1; //trailer[0].com_b.attached

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.True(truck.ParkingBrake);
            Assert.True(truck.EngineOn);
            Assert.True(truck.LeftBlinker);
            Assert.False(truck.RightBlinker);
            Assert.False(truck.ParkingLights);
            Assert.True(truck.LowBeam);
            Assert.False(truck.HighBeam);
            Assert.False(truck.Wipers);
            Assert.True(truck.Beacon);
            Assert.True(truck.TrailerAttached);
        }

        [Fact]
        public void DashboardValuesAreReadFromPluginOffsets()
        {
            byte[] data = ActiveMap();
            BitConverter.GetBytes(TruckTelemetry.GameEts2).CopyTo(data, 52);
            BitConverter.GetBytes(-2).CopyTo(data, 508);
            BitConverter.GetBytes(400f).CopyTo(data, 704);
            BitConverter.GetBytes(2500f).CopyTo(data, 740);
            BitConverter.GetBytes(22.5f).CopyTo(data, 948);
            BitConverter.GetBytes(1350f).CopyTo(data, 952);
            BitConverter.GetBytes(25f).CopyTo(data, 988);
            BitConverter.GetBytes(120f).CopyTo(data, 1000);
            BitConverter.GetBytes(27.78f).CopyTo(data, 1068);

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.True(truck.Available);
            Assert.Equal(TruckTelemetry.GameEts2, truck.Game);
            Assert.Equal(-2, truck.Gear);
            Assert.Equal(400f, truck.FuelCapacity);
            Assert.Equal(2500f, truck.EngineRpmMax);
            Assert.Equal(22.5f, truck.Speed);
            Assert.Equal(1350f, truck.EngineRpm);
            Assert.Equal(25f, truck.CruiseSpeed);
            Assert.Equal(120f, truck.Fuel);
            Assert.Equal(27.78f, truck.SpeedLimit);
        }

        [Fact]
        public void RemainingValuesAreReadFromPluginOffsets()
        {
            byte[] data = ActiveMap();
            data[1577] = 1; //wipers
            data[1581] = 1; //blinkerRightOn
            data[1582] = 1; //lightsParking
            data[1584] = 1; //lightsBeamHigh

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.True(truck.Wipers);
            Assert.True(truck.RightBlinker);
            Assert.True(truck.ParkingLights);
            Assert.True(truck.HighBeam);
            Assert.False(truck.EngineOn);
        }

        [Fact]
        public void WarningsAndAxlesAreReadFromPluginOffsets()
        {
            byte[] data = ActiveMap();
            data[1567] = 1; //motorBrake
            data[1568] = 1; //airPressureWarning
            data[1570] = 1; //fuelWarning
            data[1573] = 1; //waterTemperatureWarning
            data[1608] = 1; //differentialLock
            BitConverter.GetBytes(3).CopyTo(data, 76); //config_ui.retarderStepCount
            BitConverter.GetBytes(1).CopyTo(data, 108); //truck_ui.retarderBrake
            BitConverter.GetBytes(95).CopyTo(data, 500); //common_i.restStop
            BitConverter.GetBytes(0.12f).CopyTo(data, 1044); //wearCabin
            BitConverter.GetBytes(52_000f).CopyTo(data, 1060); //routeDistance
            BitConverter.GetBytes(2400f).CopyTo(data, 1064); //routeTime

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.True(truck.EngineBrake);
            Assert.True(truck.AirPressureWarning);
            Assert.False(truck.AirPressureEmergency);
            Assert.True(truck.FuelWarning);
            Assert.False(truck.AdBlueWarning);
            Assert.False(truck.OilPressureWarning);
            Assert.True(truck.WaterTemperatureWarning);
            Assert.False(truck.BatteryVoltageWarning);
            Assert.True(truck.DifferentialLock);
            Assert.False(truck.LiftAxle);
            Assert.Equal(3, truck.RetarderStepCount);
            Assert.Equal(1, truck.RetarderLevel);
            Assert.Equal(95, truck.RestStopMinutes);
            Assert.Equal(0.12f, truck.Wear);
            Assert.Equal(52_000f, truck.RouteDistance);
            Assert.Equal(2400f, truck.RouteTime);
        }

        [Fact]
        public void JobIsReadFromPluginOffsets()
        {
            byte[] data = ActiveMap();
            BitConverter.GetBytes(1000).CopyTo(data, 64); //common_ui.time_abs
            BitConverter.GetBytes(1190).CopyTo(data, 88); //config_ui.time_abs_delivery
            Encoding.UTF8.GetBytes("Брёвна").CopyTo(data, 2620); //config_s.cargo
            Encoding.UTF8.GetBytes("Berlin").CopyTo(data, 2748); //config_s.cityDst

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.Equal("Брёвна", truck.Cargo);
            Assert.Equal("Berlin", truck.DestinationCity);
            Assert.Equal(190, truck.DeliveryMinutesLeft);
        }

        [Fact]
        public void TrailerCountsOnlyWhenAttached()
        {
            byte[] data = ActiveMap();
            data[1611] = 1; //trailerLiftAxle
            BitConverter.GetBytes(0.4f).CopyTo(data, 6164); //trailer[0].com_f.wearBody

            TruckTelemetry detached = ScsTelemetryMap.Parse(data);
            data[6080] = 1;
            TruckTelemetry attached = ScsTelemetryMap.Parse(data);

            Assert.False(detached.LiftAxle);
            Assert.Equal(0f, detached.Wear);
            Assert.True(attached.LiftAxle);
            Assert.Equal(0.4f, attached.Wear);
        }

        [Fact]
        public void BlinkerSwitchIsNotTheLamp()
        {
            byte[] data = ActiveMap();
            data[1578] = 1; //blinkerLeftActive
            data[1579] = 1; //blinkerRightActive

            TruckTelemetry truck = ScsTelemetryMap.Parse(data);

            Assert.False(truck.LeftBlinker);
            Assert.False(truck.RightBlinker);
        }

        [Fact]
        public void InactiveSdkGivesUnknownState()
        {
            byte[] data = ActiveMap();
            data[0] = 0;
            data[1576] = 1;

            Assert.Same(TruckTelemetry.Unknown, ScsTelemetryMap.Parse(data));
        }

        [Fact]
        public void OtherPluginRevisionIsIgnored()
        {
            byte[] data = ActiveMap(revision: 11);
            data[1576] = 1;

            Assert.Same(TruckTelemetry.Unknown, ScsTelemetryMap.Parse(data));
        }

        [Fact]
        public void ShortDataGivesUnknownState()
        {
            Assert.Same(TruckTelemetry.Unknown, ScsTelemetryMap.Parse(new byte[100]));
            Assert.Same(TruckTelemetry.Unknown, ScsTelemetryMap.Parse(null));
        }
    }
}
