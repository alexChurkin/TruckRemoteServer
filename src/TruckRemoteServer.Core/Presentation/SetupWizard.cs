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
    //Steps of the setup, in the order they are shown
    public enum SetupStep
    {
        //The telemetry plugin of the games: the dashboard and the vibration of the phone
        TelemetryPlugin,
        //vJoy: the steering and the analog pedals
        Joystick,
        //Windows Firewall lets the phone's packets in
        Firewall,
        //The bindings of the game profiles (done by the server, nothing to do)
        GameControls,
        //The phone is connected and its controls reach the game
        Phone
    }

    public enum StepState
    {
        Checking,
        Done,
        //Something is missing, the step's button fixes it
        NeedsAction,
        //The fix is running
        Working,
        //The fix didn't help (the detail of the view says why, if it's known)
        Failed
    }

    //The window of the setup (passive view: everything is decided by SetupWizard)
    public interface ISetupWizardView
    {
        //The button of a step that needs an action
        event EventHandler<SetupStep> FixRequested;

        //About 10 times per second while the window is open: the phone's controls are shown live
        event EventHandler Tick;

        void ShowStep(SetupStep setupStep, StepState state, string detail);

        //Where the phone connects (the first address is the most likely one), for the QR code
        void ShowAddresses(IList<string> addresses, int port);

        //Steering -1..1 and the pedals 0..1, null while the phone isn't connected
        void ShowControls(ControlsSnapshot controls);

        //Blocks until the window is closed
        void ShowModal();

        void RunOnUiThread(Action action);
    }

    //First start (and later from the Settings menu): everything the server needs, checked one by one,
    //with a button for each missing part, then the phone's controls are shown live as a test
    public class SetupWizard
    {
        private readonly ISetupWizardView view;
        private readonly ControllerServer server;
        private readonly ControllerInputMapper input;
        private readonly IVirtualJoystick joystick;
        private readonly ISettingsStore settings;
        private readonly IFirewall firewall;
        private readonly INetworkInfo network;
        private readonly ITelemetryPluginSetup pluginSetup;
        private readonly IJoystickSetup joystickSetup;
        private readonly string programPath;
        private readonly Func<Action, Task> runInBackground;
        private readonly Dictionary<SetupStep, StepState> states = new Dictionary<SetupStep, StepState>();

        public SetupWizard(ISetupWizardView view, ControllerServer server, ControllerInputMapper input,
            IVirtualJoystick joystick, ISettingsStore settings, IFirewall firewall, INetworkInfo network,
            ITelemetryPluginSetup pluginSetup, IJoystickSetup joystickSetup, string programPath,
            Func<Action, Task> runInBackground)
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
            this.programPath = programPath;
            this.runInBackground = runInBackground;
            view.FixRequested += (s, step) => Fix(step);
            view.Tick += (s, e) => ShowPhone();
        }

        //The state of a step as it was shown last
        public StepState State(SetupStep step) => states.TryGetValue(step, out StepState state) ? state : StepState.Checking;

        //Shows the window until it's closed; the wizard isn't shown by itself again
        public void Run()
        {
            Check();
            view.ShowModal();
            settings.SetupWizardShown = true;
            settings.Save();
        }

        //Checks every step (the slow ones in background)
        public void Check()
        {
            Show(SetupStep.TelemetryPlugin, pluginSetup.IsInstalled ? StepState.Done : StepState.NeedsAction);
            Show(SetupStep.GameControls, StepState.Done);
            CheckJoystick();
            CheckFirewall();
            view.ShowAddresses(network.GetLocalAddresses().Select(address => address.ToString()).ToList(), settings.Port);
            ShowPhone();
        }

        private void Fix(SetupStep step)
        {
            if (State(step) != StepState.NeedsAction && State(step) != StepState.Failed) return;
            switch (step)
            {
                case SetupStep.TelemetryPlugin:
                    InstallPlugin();
                    break;
                case SetupStep.Joystick:
                    SetUpJoystick();
                    break;
                case SetupStep.Firewall:
                    AllowInFirewall();
                    break;
            }
        }

        //The installation may ask for the game folders, so it runs on the UI thread
        private void InstallPlugin()
        {
            Show(SetupStep.TelemetryPlugin, StepState.Working);
            try
            {
                pluginSetup.Install();
                Show(SetupStep.TelemetryPlugin, pluginSetup.IsInstalled ? StepState.Done : StepState.Failed);
            }
            catch (Exception e)
            {
                Show(SetupStep.TelemetryPlugin, StepState.Failed, e.Message);
            }
        }

        private void CheckJoystick()
        {
            Show(SetupStep.Joystick, StepState.Checking);
            bool needsSetup = true;
            Background(() => needsSetup = joystickSetup.NeedsSetup(),
                () => Show(SetupStep.Joystick, needsSetup ? StepState.NeedsAction : StepState.Done));
        }

        private void SetUpJoystick()
        {
            Show(SetupStep.Joystick, StepState.Working);
            bool done = false;
            bool needsSetup = true;
            Background(() =>
                {
                    done = joystickSetup.Setup();
                    needsSetup = joystickSetup.NeedsSetup();
                    //A connected phone gets the joystick at once
                    if (!needsSetup && !joystick.IsAvailable) joystick.Initialize();
                },
                //Not done: administrator rights were refused, nothing has changed
                () => Show(SetupStep.Joystick, !needsSetup ? StepState.Done : done ? StepState.Failed : StepState.NeedsAction));
        }

        private void CheckFirewall()
        {
            Show(SetupStep.Firewall, StepState.Checking);
            int port = settings.Port;
            FirewallStatus status = FirewallStatus.Unknown;
            Background(() => status = firewall.Check(programPath, port),
                () => Show(SetupStep.Firewall, Blocks(status) ? StepState.NeedsAction : StepState.Done));
        }

        private void AllowInFirewall()
        {
            Show(SetupStep.Firewall, StepState.Working);
            int port = settings.Port;
            bool applied = false;
            FirewallStatus status = FirewallStatus.Unknown;
            Background(() =>
                {
                    applied = firewall.AllowProgram(programPath);
                    status = firewall.Check(programPath, port);
                },
                () => Show(SetupStep.Firewall, !Blocks(status) ? StepState.Done
                    : applied ? StepState.Failed : StepState.NeedsAction));
        }

        //Unknown: another firewall or the service is stopped, nothing can be told or done then
        private static bool Blocks(FirewallStatus status) => status == FirewallStatus.NoRule || status == FirewallStatus.Blocked;

        private void ShowPhone()
        {
            ServerStatus status = server.Status;
            StepState state;
            if (!status.Running) state = StepState.Failed;
            else if (status.ControllerConnected) state = StepState.Done;
            else state = StepState.NeedsAction;
            //The phone's state changes all the time, the view is told only about changes
            if (State(SetupStep.Phone) != state) Show(SetupStep.Phone, state);
            view.ShowControls(status.ControllerConnected && !status.ControllerPaused ? input.Controls : null);
        }

        private void Show(SetupStep step, StepState state, string detail = "")
        {
            states[step] = state;
            view.ShowStep(step, state, detail);
        }

        private void Background(Action work, Action done)
        {
            runInBackground(work).ContinueWith(task => view.RunOnUiThread(done), TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
