using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Input;
using TruckRemoteServer.Network;
using TruckRemoteServer.Settings;

namespace TruckRemoteServer.Presentation
{
    //Logic of the main window: server start/stop, settings, addresses, firewall, vJoy and telemetry plugin
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
        private readonly IJoystickSetup joystickSetup;
        private readonly IGameControlsSetup controlsSetup;
        private readonly string programPath;
        //The last start failed because the port is used by another program
        private bool portBusy;

        public MainPresenter(IMainView view, ControllerServer server, ControllerInputMapper input, IVirtualJoystick joystick,
            ISettingsStore settings, IFirewall firewall, INetworkInfo network, ITelemetryPluginSetup pluginSetup,
            IJoystickSetup joystickSetup, IGameControlsSetup controlsSetup, string programPath)
        {
            this.view = view;
            this.server = server;
            this.input = input;
            this.joystick = joystick;
            this.settings = settings;
            this.firewall = firewall;
            this.network = network;
            this.pluginSetup = pluginSetup;
            this.joystickSetup = joystickSetup;
            this.controlsSetup = controlsSetup;
            this.programPath = programPath;

            view.Shown += (s, e) => OnShown();
            view.Closing += (s, e) => OnClosing();
            view.StartRequested += (s, e) => StartServer(checkFirewall: true);
            view.StopRequested += (s, e) => server.Stop();
            view.PortChanged += (s, port) => ChangePort(port);
            view.LanguageChanged += (s, language) => ChangeLanguage(language);
            view.MinimizeToTrayChanged += (s, enabled) => ChangeMinimizeToTray(enabled);
            view.AllowFirewallRequested += (s, e) => AllowInFirewall();
            view.InstallJoystickRequested += (s, e) => SetupJoystick();
            view.SetupWizardRequested += (s, e) => OpenSetupWizard();
        }

        //For tests: firewall and vJoy checks run on this scheduler
        public Func<Action, Task> RunInBackground { get; set; } = Task.Run;

        //Before the window is shown: its texts depend on the language
        public void Initialize()
        {
            view.ShowLanguage(settings.Language ?? "");
        }

        private void OnShown()
        {
            view.ShowSettings(settings.Port, settings.MinimizeToTray);
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
                    view.ShowWarning(Warning.TelemetryPluginNotInstalled, e.Message);
                }
            }

            StartServer(checkFirewall: false);
            //Some buttons of the phone have no key in the default bindings of the games
            controlsSetup.Apply();

            //The first start goes through the setup wizard, it asks the questions of the firewall and vJoy itself
            bool firstStart = !settings.SetupWizardShown;
            CheckFirewall(offerFix: !firstStart);
            CheckJoystick(offerSetup: !firstStart);
            //After the window is shown and the server is started
            if (firstStart) view.RunOnUiThread(OpenSetupWizard);
        }

        private void OpenSetupWizard()
        {
            ISetupWizardView wizardView = view.CreateSetupWizard();
            new SetupWizard(wizardView, server, input, joystick, settings, firewall, network, pluginSetup,
                joystickSetup, programPath, RunInBackground).Run();
            //What the wizard has fixed: the banners of the window and the state
            CheckFirewall(offerFix: false);
            CheckJoystick(offerSetup: false);
            ShowStatus(server.Status);
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
                portBusy = true;
                view.ShowState(ServerState.PortBusy, settings.Port);
                return;
            }
            //Allowing rules may be limited to a port
            if (checkFirewall) CheckFirewall(offerFix: false);
        }

        //The running server moves to the new port at once
        private void ChangePort(int port)
        {
            if (port == settings.Port) return;
            settings.Port = port;
            settings.Save();
            ShowAddresses();
            if (server.Status.Running)
            {
                server.Stop();
                StartServer(checkFirewall: true);
            }
            else if (portBusy)
            {
                StartServer(checkFirewall: true);
            }
        }

        private void ChangeLanguage(string language)
        {
            settings.Language = language ?? "";
            settings.Save();
            view.ShowLanguage(settings.Language);
            //Texts of the state are made by the view in the new language
            ShowStatus(server.Status);
            ShowAddresses();
        }

        private void ChangeMinimizeToTray(bool enabled)
        {
            settings.MinimizeToTray = enabled;
            settings.Save();
            view.ShowSettings(settings.Port, settings.MinimizeToTray);
        }

        private void OnServerStatus(ServerStatus status)
        {
            view.RunOnUiThread(() => ShowStatus(status));
        }

        private void ShowStatus(ServerStatus status)
        {
            if (status.Running) portBusy = false;
            ServerState state;
            if (!status.Running) state = portBusy ? ServerState.PortBusy : ServerState.Stopped;
            else if (!status.ControllerConnected) state = ServerState.WaitingForController;
            else if (status.ControllerPaused) state = ServerState.ControllerPaused;
            else if (!joystick.IsAvailable) state = ServerState.ControllerConnectedWithoutJoystick;
            else state = ServerState.ControllerConnected;
            view.ShowState(state, settings.Port);
        }

        private void OnAddressesChanged(object sender, EventArgs e)
        {
            view.RunOnUiThread(ShowAddresses);
        }

        private void ShowAddresses()
        {
            view.ShowAddresses(network.GetLocalAddresses().Select(address => address.ToString()).ToList(), settings.Port);
        }

        /* Windows Firewall blocks the phone's packets when there is no allowing rule */

        private void CheckFirewall(bool offerFix)
        {
            int port = settings.Port;
            FirewallStatus status = FirewallStatus.Unknown;
            RunInBackground(() => status = firewall.Check(programPath, port))
                .ContinueWith(task => view.RunOnUiThread(() => OnFirewallChecked(status, offerFix)),
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
            FirewallStatus status = FirewallStatus.Unknown;
            RunInBackground(() => status = firewall.AllowProgram(programPath) ? firewall.Check(programPath, port) : FirewallStatus.Unknown)
                .ContinueWith(task => view.RunOnUiThread(() =>
                {
                    if (status == FirewallStatus.Unknown)
                    {
                        view.ShowFirewallWarning(visible: true, busy: false);
                        return;
                    }
                    OnFirewallChecked(status, offerFix: false);
                    if (status != FirewallStatus.Allowed)
                    {
                        view.ShowWarning(Warning.FirewallRuleNotApplied, port.ToString(CultureInfo.InvariantCulture));
                    }
                }), TaskContinuationOptions.ExecuteSynchronously);
        }

        /* vJoy: without it the phone can't steer */

        private void CheckJoystick(bool offerSetup)
        {
            bool needsSetup = false;
            RunInBackground(() => needsSetup = joystickSetup.NeedsSetup())
                .ContinueWith(task => view.RunOnUiThread(() => OnJoystickChecked(needsSetup, offerSetup)),
                    TaskContinuationOptions.ExecuteSynchronously);
        }

        private void OnJoystickChecked(bool needsSetup, bool offerSetup)
        {
            view.ShowJoystickWarning(needsSetup, busy: false);

            //The question is asked once, later only the button is shown
            if (!needsSetup || !offerSetup || settings.JoystickPromptShown) return;
            settings.JoystickPromptShown = true;
            settings.Save();
            if (view.AskInstallJoystick()) SetupJoystick();
        }

        private void SetupJoystick()
        {
            view.ShowJoystickWarning(visible: true, busy: true);
            bool done = false;
            bool needsSetup = true;
            RunInBackground(() =>
                {
                    done = joystickSetup.Setup();
                    needsSetup = joystickSetup.NeedsSetup();
                    //A connected phone gets the joystick at once
                    if (!needsSetup && !joystick.IsAvailable) joystick.Initialize();
                })
                .ContinueWith(task => view.RunOnUiThread(() =>
                {
                    view.ShowJoystickWarning(needsSetup, busy: false);
                    //Not done: the user refused administrator rights, nothing has changed
                    if (done && needsSetup) view.ShowWarning(Warning.JoystickSetupFailed, "");
                    ShowStatus(server.Status);
                }), TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
