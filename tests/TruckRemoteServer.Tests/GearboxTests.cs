using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class GearboxTests
    {
        //As the telemetry gives them: floats of the definition
        internal static readonly float[] ScaniaGrso925 =
        {
            13.26f, 10.625f, 9.15f, 7.331f, 5.81f, 4.659f, 3.75f, 3.01f, 2.444f, 1.955f, 1.553f, 1.242f, 1.0f, 0.8f
        };

        private static readonly float[] ScaniaG33 =
        {
            20.81f, 16.16f, 12.57f, 9.76f, 7.56f, 5.87f, 4.55f, 3.53f, 2.77f, 2.15f, 1.66f, 1.29f, 1.0f, 0.78f
        };

        //A 12-speed gearbox without crawler gears
        private static readonly float[] Zf12 =
        {
            15.86f, 12.33f, 9.57f, 7.44f, 5.87f, 4.57f, 3.47f, 2.70f, 2.10f, 1.63f, 1.29f, 1.0f
        };

        [Fact]
        public void GearsAreCountedAfterTheCrawlerGears()
        {
            Gearbox gearbox = Gearbox.Find(TruckTelemetry.GameEts2, ScaniaGrso925);

            Assert.NotNull(gearbox);
            Assert.Equal(2, gearbox.CrawlerGears);
            //Seen in the game: gear 5 of the telemetry is "A3" there
            Assert.Equal(3, Gearbox.ShownGear(gearbox, 5));
            Assert.Equal(12, Gearbox.ShownGear(gearbox, 14));
            Assert.True(Gearbox.IsCrawlerGear(gearbox, 1));
            Assert.True(Gearbox.IsCrawlerGear(gearbox, 2));
            Assert.Equal(2, Gearbox.ShownGear(gearbox, 2));
            Assert.False(Gearbox.IsCrawlerGear(gearbox, 3));
            Assert.False(Gearbox.IsOverdrive(gearbox, 14));
            //Neutral and reverse are as they are
            Assert.Equal(0, Gearbox.ShownGear(gearbox, 0));
            Assert.Equal(-2, Gearbox.ShownGear(gearbox, -2));
            Assert.False(Gearbox.IsCrawlerGear(gearbox, -1));
        }

        [Fact]
        public void TopGearNamedOverdriveIsTold()
        {
            Gearbox gearbox = Gearbox.Find(TruckTelemetry.GameEts2, ScaniaG33);

            Assert.Equal(1, gearbox.CrawlerGears);
            Assert.True(gearbox.TopGearIsOverdrive);
            Assert.True(Gearbox.IsOverdrive(gearbox, 14));
            Assert.False(Gearbox.IsOverdrive(gearbox, 13));
            Assert.Equal(12, Gearbox.ShownGear(gearbox, 13));
        }

        [Fact]
        public void TheSameRatiosInTheOtherGameArePlainGears()
        {
            //International LT of American Truck Simulator has the ratios of the Scania gearbox and no named gears
            Assert.Null(Gearbox.Find(TruckTelemetry.GameAts, ScaniaG33));
        }

        [Fact]
        public void UnknownGearboxShowsItsGearsAsTheyAre()
        {
            Assert.Null(Gearbox.Find(TruckTelemetry.GameEts2, Zf12));
            Assert.Null(Gearbox.Find(TruckTelemetry.GameEts2, System.Array.Empty<float>()));
            Assert.Null(Gearbox.Find(TruckTelemetry.GameEts2, null));
            Assert.Equal(5, Gearbox.ShownGear(null, 5));
            Assert.False(Gearbox.IsCrawlerGear(null, 1));
            Assert.False(Gearbox.IsOverdrive(null, 12));
        }

        [Fact]
        public void RatiosOfTheTelemetryMayDifferALittle()
        {
            float[] ratios = (float[])ScaniaGrso925.Clone();
            ratios[1] = 10.63f;
            ratios[3] = 7.33f;

            Assert.NotNull(Gearbox.Find(TruckTelemetry.GameEts2, ratios));

            //Another gearbox: one ratio is different
            ratios[13] = 0.78f;
            Assert.Null(Gearbox.Find(TruckTelemetry.GameEts2, ratios));
        }
    }
}
