using System;
using TruckRemoteServer.Telemetry;

namespace TruckRemoteServer.Haptics
{
    //Turns the telemetry into what the driver feels: the vibration of the road (0..1) and counters of the events.
    //A counter (mod 256) is sent instead of an event, so an event can't be lost with a message; the phone plays
    //an event when its counter changes. Updated by the telemetry of every sent message (50 times per second).
    public sealed class HapticDetector
    {
        public static readonly int EventCount = Enum.GetValues(typeof(HapticEvent)).Length;

        //Suspension speeds (m/s): below the first one the road feels smooth, at the second one it is the roughest
        private const float SmoothSuspensionSpeed = 0.03f;
        private const float RoughSuspensionSpeed = 0.33f;
        //A wheel hit a bump: the speed of its suspension (m/s) and the speed of a very hard hit
        private const float BumpSuspensionSpeed = 0.6f;
        private const float HardBumpSuspensionSpeed = 2f;
        //Driving faster than this (m/s) on a rough surface shakes the truck at full strength
        private const float FullSurfaceSpeed = 10f;
        private const float RumbleStripSpeed = 3f;
        private const float OffroadRoughness = 0.3f;
        //Smoothing of the road vibration, seconds
        private const float RoadSmoothing = 0.12f;
        //Damage that comes at once (a part of 1, the sum of the wear of all parts) and the damage of a hard crash
        private const float CollisionDamage = 0.0005f;
        private const float HardCollisionDamage = 0.03f;
        //Horizontal acceleration (m/s²) that only a collision gives, and that of a hard crash
        private const float CollisionAcceleration = 25f;
        private const float HardCollisionAcceleration = 60f;
        //The same event isn't repeated faster than this (a collision changes the damage over several frames)
        private const long CollisionInterval = 300000;
        private const long BumpInterval = 250000;
        //Longer frames (loading, a hitch of the game) don't give speeds of the suspension
        private const long MaxFrame = 200000;
        private const float MicrosecondsInSecond = 1e6f;

        private readonly byte[] counters = new byte[EventCount + 1];
        private readonly float[] strengths = new float[EventCount + 1];
        //The telemetry of the previous update and of the previous frame of the game
        private TruckTelemetry previous;
        private TruckTelemetry lastFrame;
        private long lastCollision = long.MinValue / 2;
        private long lastBump = long.MinValue / 2;

        //The vibration of the road, 0..1
        public float Road { get; private set; }

        public HapticSurface Surface { get; private set; }

        public byte Counter(HapticEvent kind) => counters[(int)kind];

        //The strength of the last event of the kind, 0..1
        public float Strength(HapticEvent kind) => strengths[(int)kind];

        public void Update(TruckTelemetry truck)
        {
            if (!truck.Available)
            {
                previous = null;
                lastFrame = null;
                Road = 0;
                Surface = HapticSurface.Road;
                return;
            }
            if (previous == null)
            {
                previous = truck;
                lastFrame = truck;
                return;
            }

            DetectSwitches(previous, truck);
            previous = truck;
            if (truck.Paused)
            {
                Road = 0;
                Surface = HapticSurface.Road;
                lastFrame = truck;
                return;
            }

            long frame = truck.SimulationTime - lastFrame.SimulationTime;
            //The game hasn't made a new frame yet (it may run slower than 50 frames per second)
            if (frame <= 0) return;
            if (frame <= MaxFrame) DetectDriving(lastFrame, truck, frame / MicrosecondsInSecond);
            lastFrame = truck;
        }

        //Events that don't depend on the time between the frames
        private void DetectSwitches(TruckTelemetry before, TruckTelemetry now)
        {
            if (now.EngineOn != before.EngineOn) Fire(now.EngineOn ? HapticEvent.EngineStart : HapticEvent.EngineStop, 1);
            if (now.TrailerAttached != before.TrailerAttached)
            {
                Fire(now.TrailerAttached ? HapticEvent.TrailerCoupled : HapticEvent.TrailerUncoupled, 1);
            }
            if (now.ParkingBrake != before.ParkingBrake) Fire(HapticEvent.ParkingBrake, now.ParkingBrake ? 1 : 0.6f);
            if (now.GearboxGear != before.GearboxGear) Fire(HapticEvent.GearShift, now.GearboxGear == 0 ? 0.5f : 1);

            bool blinkerBefore = before.LeftBlinker || before.RightBlinker;
            bool blinkerNow = now.LeftBlinker || now.RightBlinker;
            if (blinkerNow != blinkerBefore) Fire(HapticEvent.Blinker, blinkerNow ? 1 : 0.5f);

            if (now.RetarderLevel != before.RetarderLevel)
            {
                float steps = Math.Max(1, now.RetarderStepCount);
                Fire(HapticEvent.Retarder, Math.Max(0.3f, now.RetarderLevel / steps));
            }
            if (now.EngineBrake != before.EngineBrake) Fire(HapticEvent.Retarder, now.EngineBrake ? 0.6f : 0.3f);

            float warning = Max(
                Came(before.AirPressureEmergency, now.AirPressureEmergency, 1),
                Came(before.AirPressureWarning, now.AirPressureWarning, 0.8f),
                Came(before.OilPressureWarning, now.OilPressureWarning, 0.8f),
                Came(before.WaterTemperatureWarning, now.WaterTemperatureWarning, 0.8f),
                Came(before.BatteryVoltageWarning, now.BatteryVoltageWarning, 0.6f),
                Came(before.FuelWarning, now.FuelWarning, 0.4f),
                Came(before.AdBlueWarning, now.AdBlueWarning, 0.4f));
            if (warning > 0) Fire(HapticEvent.Warning, warning);

            if (now.FinedToggle != before.FinedToggle) Fire(HapticEvent.Fine, 1);
            if (now.PaidToggle != before.PaidToggle) Fire(HapticEvent.Payment, 1);
            if (now.JobDeliveredToggle != before.JobDeliveredToggle) Fire(HapticEvent.JobDelivered, 1);
        }

        private void DetectDriving(TruckTelemetry before, TruckTelemetry now, float seconds)
        {
            float speed = Math.Abs(now.Speed);

            //Speeds of the suspension of the wheels on the ground: the road texture and bumps
            int wheels = Math.Min(now.SuspensionDeflection.Length, before.SuspensionDeflection.Length);
            float sumOfSquares = 0;
            float fastest = 0;
            float roughness = 0;
            int onGround = 0;
            for (int i = 0; i < wheels; i++)
            {
                if (!now.WheelOnGround[i]) continue;
                float wheelSpeed = Math.Abs(now.SuspensionDeflection[i] - before.SuspensionDeflection[i]) / seconds;
                sumOfSquares += wheelSpeed * wheelSpeed;
                fastest = Math.Max(fastest, wheelSpeed);
                roughness += now.WheelSurfaceRoughness[i];
                onGround++;
            }

            float target = 0;
            if (onGround > 0)
            {
                float suspension = (float)Math.Sqrt(sumOfSquares / onGround);
                float fromSuspension = Fraction(suspension, SmoothSuspensionSpeed, RoughSuspensionSpeed);
                float surface = roughness / onGround;
                float fromSurface = surface * Fraction(speed, 0, FullSurfaceSpeed);
                target = Math.Max(fromSuspension, fromSurface);
                if (now.OnRumbleStrip && speed > RumbleStripSpeed) Surface = HapticSurface.RumbleStrip;
                else Surface = surface >= OffroadRoughness ? HapticSurface.Offroad : HapticSurface.Road;
            }
            else
            {
                Surface = HapticSurface.Road;
            }
            Road += (target - Road) * Math.Min(1, seconds / RoadSmoothing);

            long time = now.SimulationTime;
            if (fastest >= BumpSuspensionSpeed && time - lastBump >= BumpInterval)
            {
                lastBump = time;
                Fire(HapticEvent.Bump, 0.3f + 0.7f * Fraction(fastest, BumpSuspensionSpeed, HardBumpSuspensionSpeed));
            }

            float damage = now.Damage - before.Damage;
            float acceleration = (float)Math.Sqrt(now.AccelerationX * now.AccelerationX
                + now.AccelerationZ * now.AccelerationZ);
            float collision = Math.Max(
                damage >= CollisionDamage ? 0.4f + 0.6f * Fraction(damage, CollisionDamage, HardCollisionDamage) : 0,
                acceleration >= CollisionAcceleration
                    ? 0.4f + 0.6f * Fraction(acceleration, CollisionAcceleration, HardCollisionAcceleration)
                    : 0);
            if (collision > 0 && time - lastCollision >= CollisionInterval)
            {
                lastCollision = time;
                Fire(HapticEvent.Collision, collision);
            }
        }

        private void Fire(HapticEvent kind, float strength)
        {
            int index = (int)kind;
            counters[index]++;
            strengths[index] = Math.Max(0, Math.Min(1, strength));
        }

        private static float Came(bool before, bool now, float strength) => now && !before ? strength : 0;

        private static float Max(params float[] values)
        {
            float max = 0;
            foreach (float value in values) max = Math.Max(max, value);
            return max;
        }

        //Where the value is between the two points, 0..1
        private static float Fraction(float value, float from, float to) =>
            Math.Max(0, Math.Min(1, (value - from) / (to - from)));
    }
}
