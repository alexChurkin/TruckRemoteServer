namespace TruckRemoteServer.Telemetry
{
    public interface ITelemetrySource
    {
        TruckTelemetry Read();
    }
}
