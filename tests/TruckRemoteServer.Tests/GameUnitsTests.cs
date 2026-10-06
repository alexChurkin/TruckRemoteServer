using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class GameUnitsTests
    {
        [Fact]
        public void SpeedUnitsAreReadFromTheConfigOfAProfile()
        {
            Assert.False(GameUnits.ParseMph("uset g_gallon \"0\"\r\nuset g_mph \"0\"\r\nuset g_fahrenheit \"0\"\r\n"));
            Assert.True(GameUnits.ParseMph("uset g_gallon \"1\"\nuset g_mph \"1\"\n"));
            //Not this setting, or no settings at all
            Assert.Null(GameUnits.ParseMph("uset g_mph_limit \"1\"\n"));
            Assert.Null(GameUnits.ParseMph(""));
            Assert.Null(GameUnits.ParseMph(null));
        }

        [Fact]
        public void EveryGameHasItsOwnUnits()
        {
            var units = new GameUnits();
            Assert.Null(units.Mph(TruckTelemetry.GameAts));

            units.Set(TruckTelemetry.GameAts, false);
            units.Set(TruckTelemetry.GameEts2, true);

            Assert.False(units.Mph(TruckTelemetry.GameAts));
            Assert.True(units.Mph(TruckTelemetry.GameEts2));
            Assert.Null(units.Mph(TruckTelemetry.GameUnknown));
        }
    }
}
