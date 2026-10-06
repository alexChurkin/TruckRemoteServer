using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Input;
using TruckRemoteServer.Network;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.Settings;
using TruckRemoteServer.Telemetry;
using TruckRemoteServer.Updates;

namespace TruckRemoteServer.Tests
{
    public class FakeKeyboard : IKeyboard
    {
        private readonly List<string> events = new List<string>();

        public List<string> Events
        {
            get { lock (events) return events.ToList(); }
        }

        public void Clear()
        {
            lock (events) events.Clear();
        }

        public void Press(GameKey key) => Add("press " + key);

        public void Release(GameKey key) => Add("release " + key);

        public void Click(GameKey key) => Add("click " + key);

        private void Add(string e)
        {
            lock (events) events.Add(e);
        }
    }

    public class FakeJoystick : IVirtualJoystick
    {
        public bool IsAvailable { get; set; } = true;
        public bool HasPedalAxes { get; set; } = true;
        public int Steering = -1;
        public int Gas = -1;
        public int Brake = -1;
        public int Initializations;
        public bool Released;

        public bool Initialize()
        {
            Initializations++;
            return IsAvailable;
        }

        public void SetSteering(int value) => Steering = value;

        public void SetPedals(int gas, int brake)
        {
            Gas = gas;
            Brake = brake;
        }

        public void Release() => Released = true;

        public event Action<uint> ForceFeedback;

        public void RaiseForceFeedback(uint duration) => ForceFeedback?.Invoke(duration);
    }

    public class FakeTelemetry : ITelemetrySource
    {
        public volatile TruckTelemetry Truck = new TruckTelemetry();

        public TruckTelemetry Read() => Truck;
    }

    public class NoTimerResolution : ITimerResolution
    {
        public IDisposable Acquire() => new Nothing();

        private sealed class Nothing : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public class FakeSettings : ISettingsStore
    {
        public int Port { get; set; }
        public bool FirewallPromptShown { get; set; }
        public bool JoystickPromptShown { get; set; }
        public bool SetupWizardShown { get; set; }
        public string Language { get; set; } = "";
        public bool MinimizeToTray { get; set; }
        public int Saves;

        public void Save() => Saves++;
    }

    public class FakeFirewall : IFirewall
    {
        public FirewallStatus Status = FirewallStatus.Allowed;
        public bool AllowAccepted = true;
        public int AllowCalls;

        public FirewallStatus Check(string programPath, int port) => Status;

        public bool AllowProgram(string programPath)
        {
            AllowCalls++;
            if (AllowAccepted) Status = FirewallStatus.Allowed;
            return AllowAccepted;
        }
    }

    public class FakeNetwork : INetworkInfo
    {
        public List<IPAddress> Addresses = new List<IPAddress> { IPAddress.Parse("192.168.1.10") };

        public IList<IPAddress> GetLocalAddresses() => Addresses;

        public event EventHandler AddressesChanged;

        public void RaiseChanged() => AddressesChanged?.Invoke(this, EventArgs.Empty);
    }

    public class FakePluginSetup : ITelemetryPluginSetup
    {
        public bool IsInstalled { get; set; } = true;
        public Exception InstallError;
        public int Installs;

        public void Install()
        {
            Installs++;
            if (InstallError != null) throw InstallError;
            IsInstalled = true;
        }
    }

    public class FakeJoystickSetup : IJoystickSetup
    {
        public bool Ready = true;
        //The user gives administrator rights
        public bool Accepted = true;
        //The setup makes vJoy ready
        public bool Works = true;
        public int Setups;

        public bool NeedsSetup() => !Ready;

        public bool Setup()
        {
            Setups++;
            if (!Accepted) return false;
            if (Works) Ready = true;
            return true;
        }
    }

    public class FakeControlsSetup : IGameControlsSetup
    {
        public int Applies;

        public void Apply() => Applies++;
    }

    public class FakeView : IMainView
    {
        public int? Port;
        public bool MinimizeToTray;
        public IList<string> Addresses;
        public int AddressesPort;
        public ServerState State;
        public int StatePort;
        public string Language;
        public bool FirewallWarning;
        public bool FirewallBusy;
        public bool AnswerAllowFirewall = true;
        public int FirewallQuestions;
        public bool JoystickWarning;
        public bool JoystickBusy;
        public bool AnswerInstallJoystick = true;
        public int JoystickQuestions;
        public List<Warning> Warnings = new List<Warning>();

        public event EventHandler Shown;
        public event EventHandler Closing;
        public event EventHandler StartRequested;
        public event EventHandler StopRequested;
        public event EventHandler<int> PortChanged;
        public event EventHandler<string> LanguageChanged;
        public event EventHandler<bool> MinimizeToTrayChanged;
        public event EventHandler AllowFirewallRequested;
        public event EventHandler InstallJoystickRequested;
        public event EventHandler SetupWizardRequested;
        public event EventHandler UpdateRequested;
        public event EventHandler CheckForUpdatesRequested;

        //null: no banner
        public string Update;
        public bool UpdateBusy;
        public int UpToDateMessages;
        public bool Exited;

        public void RequestUpdate() => UpdateRequested?.Invoke(this, EventArgs.Empty);

        public void CheckForUpdates() => CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);

        public void ShowUpdate(string version, bool busy)
        {
            Update = version;
            UpdateBusy = busy;
        }

        public void ShowUpToDate() => UpToDateMessages++;

        public void CloseForUpdate() => Exited = true;

        public FakeWizardView Wizard = new FakeWizardView();
        public int Wizards;

        public void OpenSetupWizard() => SetupWizardRequested?.Invoke(this, EventArgs.Empty);

        public ISetupWizardView CreateSetupWizard()
        {
            Wizards++;
            return Wizard;
        }

        public void Show() => Shown?.Invoke(this, EventArgs.Empty);
        public void Close() => Closing?.Invoke(this, EventArgs.Empty);
        public void Start() => StartRequested?.Invoke(this, EventArgs.Empty);
        public void Stop() => StopRequested?.Invoke(this, EventArgs.Empty);
        public void ChangePort(int port) => PortChanged?.Invoke(this, port);
        public void AllowFirewall() => AllowFirewallRequested?.Invoke(this, EventArgs.Empty);
        public void InstallJoystick() => InstallJoystickRequested?.Invoke(this, EventArgs.Empty);
        public void ChangeLanguage(string language) => LanguageChanged?.Invoke(this, language);
        public void ChangeMinimizeToTray(bool enabled) => MinimizeToTrayChanged?.Invoke(this, enabled);

        public void ShowLanguage(string language) => Language = language;

        public void ShowSettings(int port, bool minimizeToTray)
        {
            Port = port;
            MinimizeToTray = minimizeToTray;
        }

        public void ShowAddresses(IList<string> addresses, int port)
        {
            Addresses = addresses;
            AddressesPort = port;
        }

        public void ShowState(ServerState state, int port)
        {
            State = state;
            StatePort = port;
        }

        public void ShowFirewallWarning(bool visible, bool busy)
        {
            FirewallWarning = visible;
            FirewallBusy = busy;
        }

        public void ShowJoystickWarning(bool visible, bool busy)
        {
            JoystickWarning = visible;
            JoystickBusy = busy;
        }

        public bool AskInstallJoystick()
        {
            JoystickQuestions++;
            return AnswerInstallJoystick;
        }

        public bool AskAllowFirewall()
        {
            FirewallQuestions++;
            return AnswerAllowFirewall;
        }

        public void ShowWarning(Warning warning, string detail) => Warnings.Add(warning);

        public void RunOnUiThread(Action action) => action();
    }

    public class FakeUpdater : IUpdater
    {
        public ReleaseInfo Latest;
        public bool InstallWorks = true;
        public int Installs;
        public int PagesOpened;

        public ReleaseInfo GetLatestRelease() => Latest;

        public bool Install(ReleaseInfo release)
        {
            Installs++;
            return InstallWorks && release.Exe != null;
        }

        public void OpenPage(ReleaseInfo release) => PagesOpened++;
    }

    public class FakeWizardView : ISetupWizardView
    {
        public readonly Dictionary<SetupStep, StepState> Steps = new Dictionary<SetupStep, StepState>();
        public readonly Dictionary<SetupStep, string> Details = new Dictionary<SetupStep, string>();
        public IList<string> Addresses;
        public ControlsSnapshot Controls;
        //What the user does while the window is open
        public Action WhileShown;
        public int Shows;

        public event EventHandler<SetupStep> FixRequested;
        public event EventHandler Tick;

        public void Fix(SetupStep step) => FixRequested?.Invoke(this, step);

        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);

        public void ShowStep(SetupStep setupStep, StepState state, string detail)
        {
            Steps[setupStep] = state;
            Details[setupStep] = detail;
        }

        public void ShowAddresses(IList<string> addresses, int port) => Addresses = addresses;

        public void ShowControls(ControlsSnapshot controls) => Controls = controls;

        public void ShowModal()
        {
            Shows++;
            WhileShown?.Invoke();
        }

        public void RunOnUiThread(Action action) => action();
    }
}
