using System;
using System.Collections.Generic;

namespace TruckRemoteServer.Presentation
{
    //What the window says about the server (the view turns it into text of the chosen language)
    public enum ServerState
    {
        Stopped,
        PortBusy,
        WaitingForController,
        ControllerConnected,
        ControllerConnectedWithoutJoystick,
        ControllerPaused
    }

    public enum Warning
    {
        //Detail: the reason
        TelemetryPluginNotInstalled,
        //Detail: the port
        FirewallRuleNotApplied,
        //No detail
        JoystickSetupFailed
    }

    //Main window of the server (passive view: no logic, everything is decided by MainPresenter)
    public interface IMainView
    {
        event EventHandler Shown;
        event EventHandler Closing;
        event EventHandler StartRequested;
        event EventHandler StopRequested;
        event EventHandler<int> PortChanged;
        event EventHandler<string> LanguageChanged;
        event EventHandler<bool> MinimizeToTrayChanged;
        event EventHandler<bool> StartWithWindowsChanged;
        event EventHandler AllowFirewallRequested;
        event EventHandler InstallJoystickRequested;
        event EventHandler SetupWizardRequested;
        event EventHandler UpdateRequested;
        event EventHandler CheckForUpdatesRequested;

        //"" - the language of Windows
        void ShowLanguage(string language);

        void ShowSettings(int port, bool minimizeToTray, bool startWithWindows);

        //The first address is the most likely one for the phone
        void ShowAddresses(IList<string> addresses, int port);

        void ShowState(ServerState state, int port);

        void ShowFirewallWarning(bool visible, bool busy);

        bool AskAllowFirewall();

        void ShowJoystickWarning(bool visible, bool busy);

        bool AskInstallJoystick();

        void ShowWarning(Warning warning, string detail);

        //A newer version of the server; null hides the banner. Busy: it is being downloaded
        void ShowUpdate(string version, bool busy);

        //The answer to "Check for updates" of the menu when there's nothing newer
        void ShowUpToDate();

        //The new version of the server has been started: this one closes
        void CloseForUpdate();

        //The window of the setup wizard over this one, not shown yet
        ISetupWizardView CreateSetupWizard();

        //Presenter gets events of the server and the system on other threads
        void RunOnUiThread(Action action);
    }
}
