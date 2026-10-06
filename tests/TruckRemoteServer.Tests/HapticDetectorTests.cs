using TruckRemoteServer.Haptics;
using TruckRemoteServer.Telemetry;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public class HapticDetectorTests
    {
        //A frame of the game every 20 ms
        private const long Frame = 20_000;

        private readonly HapticDetector detector = new HapticDetector();
        private long time;

        private TruckTelemetry Truck(float speed = 0, float[] deflection = null, float roughness = 0)
        {
            deflection = deflection ?? new[] { 0f, 0f, 0f, 0f };
            var surface = new float[deflection.Length];
            var onGround = new bool[deflection.Length];
            for (int i = 0; i < deflection.Length; i++)
            {
                surface[i] = roughness;
                onGround[i] = true;
            }
            time += Frame;
            return new TruckTelemetry
            {
                Available = true,
                SimulationTime = time,
                Speed = speed,
                SuspensionDeflection = deflection,
                WheelOnGround = onGround,
                WheelSurfaceRoughness = surface
            };
        }

        [Fact]
        public void SwitchesFireTheirEvents()
        {
            detector.Update(Truck());
            TruckTelemetry truck = Truck();
            truck.EngineOn = true;
            truck.TrailerAttached = true;
            truck.ParkingBrake = true;
            truck.LeftBlinker = true;
            truck.GearboxGear = 1;
            truck.FinedToggle = true;
            truck.PaidToggle = true;
            truck.JobDeliveredToggle = true;
            detector.Update(truck);

            foreach (HapticEvent kind in new[]
            {
                HapticEvent.EngineStart, HapticEvent.TrailerCoupled, HapticEvent.ParkingBrake, HapticEvent.Blinker,
                HapticEvent.GearShift, HapticEvent.Fine, HapticEvent.Payment, HapticEvent.JobDelivered
            })
            {
                Assert.Equal(1, detector.Counter(kind));
            }
            Assert.Equal(0, detector.Counter(HapticEvent.EngineStop));
            Assert.Equal(0, detector.Counter(HapticEvent.TrailerUncoupled));

            TruckTelemetry back = Truck();
            back.GearboxGear = 0;
            back.LeftBlinker = false;
            detector.Update(back);

            Assert.Equal(1, detector.Counter(HapticEvent.EngineStop));
            Assert.Equal(1, detector.Counter(HapticEvent.TrailerUncoupled));
            Assert.Equal(2, detector.Counter(HapticEvent.Blinker));
            //The blinker relay "tocks" softer when the lamp goes off, the neutral is a softer shift
            Assert.Equal(0.5f, detector.Strength(HapticEvent.Blinker));
            Assert.Equal(0.5f, detector.Strength(HapticEvent.GearShift));
            //The plugin flips the gameplay events, so flipping back is a new event too
            Assert.Equal(2, detector.Counter(HapticEvent.Fine));
        }

        [Fact]
        public void FirstTelemetryFiresNothing()
        {
            TruckTelemetry truck = Truck();
            truck.EngineOn = true;
            truck.FinedToggle = true;
            detector.Update(truck);

            Assert.Equal(0, detector.Counter(HapticEvent.EngineStart));
            Assert.Equal(0, detector.Counter(HapticEvent.Fine));
        }

        [Fact]
        public void SameTelemetryReadTwiceFiresOnce()
        {
            detector.Update(Truck());
            TruckTelemetry truck = Truck();
            truck.EngineOn = true;
            detector.Update(truck);
            //No new frame of the game yet
            detector.Update(truck);

            Assert.Equal(1, detector.Counter(HapticEvent.EngineStart));
        }

        [Fact]
        public void WarningComesWithItsStrength()
        {
            detector.Update(Truck());
            TruckTelemetry truck = Truck();
            truck.FuelWarning = true;
            detector.Update(truck);
            TruckTelemetry worse = Truck();
            worse.FuelWarning = true;
            worse.AirPressureEmergency = true;
            detector.Update(worse);

            Assert.Equal(2, detector.Counter(HapticEvent.Warning));
            Assert.Equal(1f, detector.Strength(HapticEvent.Warning));
        }

        [Fact]
        public void DamageAtOnceIsACollision()
        {
            detector.Update(Truck(speed: 20));
            TruckTelemetry crash = Truck(speed: 15);
            crash.Damage = 0.03f;
            detector.Update(crash);
            //The damage grows for a few more frames of the same collision
            TruckTelemetry after = Truck(speed: 10);
            after.Damage = 0.04f;
            detector.Update(after);

            Assert.Equal(1, detector.Counter(HapticEvent.Collision));
            Assert.Equal(1f, detector.Strength(HapticEvent.Collision));
        }

        [Fact]
        public void HardHorizontalAccelerationIsACollision()
        {
            detector.Update(Truck(speed: 20));
            TruckTelemetry hit = Truck(speed: 20);
            hit.AccelerationZ = 40;
            detector.Update(hit);

            Assert.Equal(1, detector.Counter(HapticEvent.Collision));
            Assert.InRange(detector.Strength(HapticEvent.Collision), 0.5f, 0.9f);
        }

        [Fact]
        public void BrakingIsNotACollision()
        {
            detector.Update(Truck(speed: 20));
            TruckTelemetry braking = Truck(speed: 19.8f);
            braking.AccelerationZ = 6;
            braking.Damage = 0.00001f;
            detector.Update(braking);

            Assert.Equal(0, detector.Counter(HapticEvent.Collision));
        }

        [Fact]
        public void FastSuspensionIsABump()
        {
            detector.Update(Truck(speed: 15));
            //One wheel moves 3 cm in 20 ms: 1.5 m/s
            detector.Update(Truck(speed: 15, deflection: new[] { 0.03f, 0f, 0f, 0f }));

            Assert.Equal(1, detector.Counter(HapticEvent.Bump));
            Assert.True(detector.Road > 0);
        }

        [Fact]
        public void SmoothRoadDoesNotShake()
        {
            for (int i = 0; i < 50; i++) detector.Update(Truck(speed: 25));

            Assert.Equal(0f, detector.Road);
            Assert.Equal(HapticSurface.Road, detector.Surface);
            Assert.Equal(0, detector.Counter(HapticEvent.Bump));
        }

        [Fact]
        public void OffroadShakesMoreWhenFaster()
        {
            for (int i = 0; i < 50; i++) detector.Update(Truck(speed: 3, roughness: 0.6f));
            float slow = detector.Road;
            for (int i = 0; i < 50; i++) detector.Update(Truck(speed: 15, roughness: 0.6f));

            Assert.Equal(HapticSurface.Offroad, detector.Surface);
            Assert.InRange(slow, 0.1f, 0.25f);
            Assert.InRange(detector.Road, 0.55f, 0.6f);
        }

        [Fact]
        public void PauseStopsTheRoad()
        {
            for (int i = 0; i < 50; i++) detector.Update(Truck(speed: 15, roughness: 0.6f));
            TruckTelemetry paused = Truck(speed: 15, roughness: 0.6f);
            paused.Paused = true;
            detector.Update(paused);

            Assert.Equal(0f, detector.Road);
        }

        [Fact]
        public void LongFrameGivesNoBump()
        {
            detector.Update(Truck(speed: 15));
            time += 1_000_000;
            detector.Update(Truck(speed: 15, deflection: new[] { 0.1f, 0.1f, 0.1f, 0.1f }));

            Assert.Equal(0, detector.Counter(HapticEvent.Bump));
        }

        [Fact]
        public void TelemetryGoneStartsAnew()
        {
            detector.Update(Truck());
            detector.Update(TruckTelemetry.Unknown);
            TruckTelemetry truck = Truck();
            truck.EngineOn = true;
            detector.Update(truck);

            Assert.Equal(0, detector.Counter(HapticEvent.EngineStart));
        }

        [Fact]
        public void SurfacesAreRoughByTheirNames()
        {
            Assert.Equal(0f, SurfaceRoughness.Of("road"));
            Assert.Equal(0f, SurfaceRoughness.Of("road_snow"));
            Assert.Equal(0f, SurfaceRoughness.Of(""));
            Assert.Equal(0f, SurfaceRoughness.Of(null));
            Assert.True(SurfaceRoughness.Of("gravel") > SurfaceRoughness.Of("grass"));
            Assert.True(SurfaceRoughness.Of("dirt") > 0);
            Assert.True(SurfaceRoughness.IsRumbleStrip("rumble_stripes"));
            Assert.False(SurfaceRoughness.IsRumbleStrip("road"));
        }
    }
}
