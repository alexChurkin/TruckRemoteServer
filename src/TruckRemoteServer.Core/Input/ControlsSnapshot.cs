namespace TruckRemoteServer.Input
{
    //What the controller is doing now, for the window: the steering -1..1 (full lock left..right), the pedals 0..1
    public sealed class ControlsSnapshot
    {
        public static readonly ControlsSnapshot Released = new ControlsSnapshot(0, 0, 0);

        public ControlsSnapshot(double steering, double gas, double brake)
        {
            Steering = steering;
            Gas = gas;
            Brake = brake;
        }

        public double Steering { get; }
        public double Gas { get; }
        public double Brake { get; }
    }
}
