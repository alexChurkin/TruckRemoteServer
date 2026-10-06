using System.Windows.Forms;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.Setup;

namespace TruckRemoteServer.Infrastructure
{
    //Installs the telemetry plugin into ETS2 and ATS folders (asks for them if they aren't found)
    public sealed class TelemetryPluginSetup : ITelemetryPluginSetup
    {
        private readonly PluginInstaller installer = new PluginInstaller();

        //A failed installation isn't repeated on every start (the user may have no such game)
        public bool IsInstalled => installer.Status != SetupStatus.Uninstalled;

        public void Install()
        {
            installer.Install(Form.ActiveForm);
        }
    }
}
