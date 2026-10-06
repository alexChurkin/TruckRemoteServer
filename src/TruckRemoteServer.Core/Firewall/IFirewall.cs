namespace TruckRemoteServer.Firewall
{
    public interface IFirewall
    {
        FirewallStatus Check(string programPath, int port);

        //Returns false if the user refused elevation
        bool AllowProgram(string programPath);
    }
}
