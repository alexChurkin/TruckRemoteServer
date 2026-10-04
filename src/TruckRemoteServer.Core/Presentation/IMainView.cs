using System;
using System.Collections.Generic;

namespace TruckRemoteServer.Presentation
{
    public enum StatusKind
    {
        Ok,
        Warning,
        Error
    }

    //Main window of the server (passive view: no logic, everything is decided by MainPresenter)
    public interface IMainView
    {
        event EventHandler Shown;
        event EventHandler Closing;
        event EventHandler StartRequested;
        event EventHandler StopRequested;
        event EventHandler<int> PortChanged;
        event EventHandler<int> SensitivityChanged;
        event EventHandler AllowFirewallRequested;

        void ShowSettings(int port, int sensitivity);

        //The first address is the most likely one, all of them are available in a tooltip
        void ShowAddresses(IList<string> addresses);

        void ShowStatus(string text, StatusKind kind);

        void ShowRunning(bool running);

        void ShowFirewallWarning(bool visible, bool busy);

        bool AskAllowFirewall();

        void ShowWarning(string message);

        //Presenter gets events of the server and the system on other threads
        void RunOnUiThread(Action action);
    }
}
