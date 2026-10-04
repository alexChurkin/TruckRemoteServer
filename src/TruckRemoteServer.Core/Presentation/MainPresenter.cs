using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Input;
using TruckRemoteServer.Network;
using TruckRemoteServer.Settings;

namespace TruckRemoteServer.Presentation
{
    //Logic of the main window: server start/stop, settings, addresses, firewall and telemetry plugin
    public class MainPresenter
    {
        private readonly IMainView view;
        private readonly ControllerServer server;
        private readonly ControllerInputMapper input;
        private readonly IVirtualJoystick joystick;
        private readonly ISettingsStore settings;
        private readonly IFirewall firewall;
        private readonly INetworkInfo network;
        private readonly ITelemetryPluginSetup pluginSetup;
        private readonly string programPath;

        public MainPresenter(IMainView view, ControllerServer server, ControllerInputMapper input,
            IVirtualJoystick joystick, ISettingsStore settings, IFirewall firewall, INetworkInfo network,
            ITelemetryPluginSetup pluginSetup, string programPath)
        {
            this.view = view;
            this.server = server;
            this.input = input;
            this.joystick = joystick;
            this.settings = settings;
            this.firewall = firewall;
            this.network = network;
            this.pluginSetup = pluginSetup;
            this.programPath = programPath;

            view.Shown += (s, e) => OnShown();
            view.Closing += (s, e) => OnClosing();
            view.StartRequested += (s, e) => StartServer(checkFirewall: true);
            view.StopRequested += (s, e) => server.Stop();
            view.PortChanged += (s, port) => SavePort(port);
            view.SensitivityChanged += (s, value) => SetSensitivity(value);
            view.AllowFirewallRequested += (s, e) => AllowInFirewall();
        }

        //For tests: firewall checks run on this scheduler
        public Func<Func<FirewallStatus>, Task<FirewallStatus>> RunInBackground { get; set; } = Task.Run;

        private void OnShown()
        {
            input.SteeringSensitivity = settings.Sensitivity;
            view.ShowSettings(settings.Port, settings.Sensitivity);
            server.StatusChanged += OnServerStatus;
            network.AddressesChanged += OnAddressesChanged;
            ShowAddresses();
            ShowStatus(server.Status);

            if (!pluginSetup.IsInstalled)
            {
                try
                {
                    pluginSetup.Install();
                }
                catch (Exception e)
                {
                    //Controls still work without telemetry, so the server is started anyway
                    view.ShowWarning("Telemetry plugin wasn't installed: " + e.Message);
                }
            }

            StartServer(checkFirewall: false);
            CheckFirewall(offerFix: true);
        }

        private void OnClosing()
        {
            network.AddressesChanged -= OnAddressesChanged;
            server.StatusChanged -= OnServerStatus;
            server.Stop();
            joystick.Release();
        }

        private void StartServer(bool checkFirewall)
        {
            if (!server.Start(settings.Port))
            {
                view.ShowStatus("Port " + settings.Port + " is busy", StatusKind.Error);
                return;
            }
            //Allowing rules may be limited to a port
            if (checkFirewall) CheckFirewall(offerFix: false);
        }

        private void SavePort(int port)
        {
            settings.Port = port;
            settings.Save();
        }

        private void SetSensitivity(int value)
        {
            input.SteeringSensitivity = value;
            settings.Sensitivity = value;
            settings.Save();
        }

        private void OnServerStatus(ServerStatus status)
        {
            view.RunOnUiThread(() => ShowStatus(status));
        }

        private void ShowStatus(ServerStatus status)
        {
            view.ShowRunning(status.Running);
            if (!status.Running)
            {
                view.ShowStatus("Disabled", StatusKind.Error);
            }
            else if (!status.ControllerConnected)
            {
                view.ShowStatus("Enabled", StatusKind.Ok);
            }
            else if (status.ControllerPaused)
            {
                view.ShowStatus("Controller paused", StatusKind.Ok);
            }
            else if (!joystick.IsAvailable)
            {
                view.ShowStatus("Controller active, vJoy error", StatusKind.Warning);
            }
            else
            {
                view.ShowStatus("Controller active", StatusKind.Ok);
            }
        }

        private void OnAddressesChanged(object sender, EventArgs e)
        {
            view.RunOnUiThread(ShowAddresses);
        }

        private void ShowAddresses()
        {
            view.ShowAddresses(network.GetLocalAddresses().Select(address => address.ToString()).ToList());
        }

        /* Windows Firewall blocks the phone's packets when there is no allowing rule */

        private void CheckFirewall(bool offerFix)
        {
            int port = settings.Port;
            RunInBackground(() => firewall.Check(programPath, port))
                .ContinueWith(task => view.RunOnUiThread(() => OnFirewallChecked(task.Result, offerFix)),
                    TaskContinuationOptions.ExecuteSynchronously);
        }

        private void OnFirewallChecked(FirewallStatus status, bool offerFix)
        {
            bool blocked = status == FirewallStatus.NoRule || status == FirewallStatus.Blocked;
            view.ShowFirewallWarning(blocked, busy: false);

            //The question is asked once, later only the link is shown
            if (!blocked || !offerFix || settings.FirewallPromptShown) return;
            settings.FirewallPromptShown = true;
            settings.Save();
            if (view.AskAllowFirewall()) AllowInFirewall();
        }

        private void AllowInFirewall()
        {
            int port = settings.Port;
            view.ShowFirewallWarning(visible: true, busy: true);
            //Unknown: elevation was cancelled, nothing has changed
            RunInBackground(() => firewall.AllowProgram(programPath) ? firewall.Check(programPath, port) : FirewallStatus.Unknown)
                .ContinueWith(task => view.RunOnUiThread(() =>
                {
                    FirewallStatus status = task.Result;
                    if (status == FirewallStatus.Unknown)
                    {
                        view.ShowFirewallWarning(visible: true, busy: false);
                        return;
                    }
                    OnFirewallChecked(status, offerFix: false);
                    if (status != FirewallStatus.Allowed)
                    {
                        view.ShowWarning("The firewall rule wasn't applied. Please allow Truck Remote Server " +
                            "(UDP port " + port + ") in your firewall manually.");
                    }
                }), TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
