using System;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Input;
using TruckRemoteServer.Presentation;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public sealed class MainPresenterTests : IDisposable
    {
        private readonly FakeView view = new FakeView();
        private readonly FakeJoystick joystick = new FakeJoystick();
        //The wizard of the first start is tested apart (see FirstStartOpensTheSetupWizard)
        private readonly FakeSettings settings = new FakeSettings { Port = 0, SetupWizardShown = true };
        private readonly FakeFirewall firewall = new FakeFirewall();
        private readonly FakeNetwork network = new FakeNetwork();
        private readonly FakePluginSetup pluginSetup = new FakePluginSetup();
        private readonly FakeJoystickSetup joystickSetup = new FakeJoystickSetup();
        private readonly FakeControlsSetup controlsSetup = new FakeControlsSetup();
        private readonly ControllerInputMapper mapper;
        private readonly ControllerServer server;
        private readonly MainPresenter presenter;

        public MainPresenterTests()
        {
            mapper = new ControllerInputMapper(new FakeKeyboard(), joystick);
            server = new ControllerServer(mapper, new FakeTelemetry(), joystick, new NoTimerResolution(),
                NullLogger<ControllerServer>.Instance);
            presenter = new MainPresenter(view, server, mapper, joystick, settings, firewall, network, pluginSetup,
                joystickSetup, controlsSetup, "server.exe")
            {
                RunInBackground = work =>
                {
                    work();
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            };
        }

        public void Dispose() => server.Stop();

        [Fact]
        public void ShownWindowGetsSettingsAddressesAndRunningServer()
        {
            view.Show();

            Assert.Equal(0, view.Port);
            Assert.Equal(new[] { "192.168.1.10" }, view.Addresses);
            Assert.Equal(ServerState.WaitingForController, view.State);
        }

        [Fact]
        public void FirstStartOpensTheSetupWizardInsteadOfTheQuestions()
        {
            settings.SetupWizardShown = false;
            firewall.Status = FirewallStatus.NoRule;
            joystickSetup.Ready = false;
            //The user fixes the firewall in the wizard
            view.Wizard.WhileShown = () => view.Wizard.Fix(SetupStep.Firewall);
            view.Show();

            Assert.Equal(1, view.Wizards);
            Assert.Equal(0, view.FirewallQuestions);
            Assert.Equal(0, view.JoystickQuestions);
            Assert.True(settings.SetupWizardShown);
            //The banners show what is left after the wizard
            Assert.False(view.FirewallWarning);
            Assert.True(view.JoystickWarning);
        }

        [Fact]
        public void LaterStartsDoNotOpenTheWizardButTheMenuDoes()
        {
            view.Show();
            Assert.Equal(0, view.Wizards);

            view.OpenSetupWizard();
            Assert.Equal(1, view.Wizards);
        }

        [Fact]
        public void AddressesAreUpdatedWhenTheNetworkChanges()
        {
            view.Show();
            network.Addresses.Add(IPAddress.Parse("10.0.0.5"));
            network.RaiseChanged();

            Assert.Equal(new[] { "192.168.1.10", "10.0.0.5" }, view.Addresses);
        }

        [Fact]
        public void BlockedFirewallIsOfferedToBeFixedOnce()
        {
            firewall.Status = FirewallStatus.NoRule;
            view.Show();

            Assert.Equal(1, view.FirewallQuestions);
            Assert.Equal(1, firewall.AllowCalls);
            Assert.False(view.FirewallWarning);
            Assert.True(settings.FirewallPromptShown);
        }

        [Fact]
        public void DeclinedFirewallFixLeavesTheLink()
        {
            firewall.Status = FirewallStatus.Blocked;
            view.AnswerAllowFirewall = false;
            view.Show();

            Assert.True(view.FirewallWarning);
            Assert.Equal(0, firewall.AllowCalls);

            //Later the link fixes it without asking
            view.AllowFirewall();
            Assert.Equal(1, firewall.AllowCalls);
            Assert.False(view.FirewallWarning);
        }

        [Fact]
        public void CancelledElevationKeepsTheWarning()
        {
            firewall.Status = FirewallStatus.NoRule;
            firewall.AllowAccepted = false;
            settings.FirewallPromptShown = true;
            view.Show();

            view.AllowFirewall();

            Assert.True(view.FirewallWarning);
            Assert.False(view.FirewallBusy);
            Assert.Empty(view.Warnings);
        }

        [Fact]
        public void StopAndStartChangeTheStatus()
        {
            view.Show();
            view.Stop();
            Assert.Equal(ServerState.Stopped, view.State);

            view.Start();
            Assert.Equal(ServerState.WaitingForController, view.State);
        }

        [Fact]
        public void MissingPluginIsInstalledAndItsFailureDoesntStopTheServer()
        {
            pluginSetup.IsInstalled = false;
            pluginSetup.InstallError = new InvalidOperationException("no game");
            view.Show();

            Assert.Equal(1, pluginSetup.Installs);
            Assert.Equal(new[] { Warning.TelemetryPluginNotInstalled }, view.Warnings);
            Assert.Equal(ServerState.WaitingForController, view.State);
        }

        [Fact]
        public void RunningServerMovesToTheNewPort()
        {
            view.Show();
            int newPort = FreePort();
            view.ChangePort(newPort);

            Assert.Equal(newPort, settings.Port);
            Assert.Equal(newPort, server.Port);
            Assert.Equal(newPort, view.AddressesPort);
            Assert.Equal(ServerState.WaitingForController, view.State);
        }

        [Fact]
        public void BusyPortIsShownAndAnotherOneCanBeChosen()
        {
            using (var other = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Any, 0)))
            {
                settings.Port = ((IPEndPoint)other.Client.LocalEndPoint).Port;
                view.Show();
                Assert.Equal(ServerState.PortBusy, view.State);
                Assert.Equal(settings.Port, view.StatePort);

                view.ChangePort(FreePort());
                Assert.Equal(ServerState.WaitingForController, view.State);
            }
        }

        [Fact]
        public void LanguageIsShownBeforeTheWindowAndSaved()
        {
            settings.Language = "ru";
            presenter.Initialize();
            Assert.Equal("ru", view.Language);

            view.Show();
            view.ChangeLanguage("");
            Assert.Equal("", settings.Language);
            Assert.Equal("", view.Language);
        }

        [Fact]
        public void ShownWindowAddsMissingKeysToTheGames()
        {
            view.Show();

            Assert.Equal(1, controlsSetup.Applies);
        }

        [Fact]
        public void MinimizingToTrayIsOffByDefaultAndSaved()
        {
            view.Show();
            Assert.False(view.MinimizeToTray);

            view.ChangeMinimizeToTray(true);
            Assert.True(settings.MinimizeToTray);
            Assert.True(view.MinimizeToTray);
            Assert.True(settings.Saves > 0);
        }

        private static int FreePort()
        {
            using (var socket = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Any, 0)))
            {
                return ((IPEndPoint)socket.Client.LocalEndPoint).Port;
            }
        }

        [Fact]
        public void ClosingStopsTheServerAndReleasesTheJoystick()
        {
            view.Show();
            view.Close();

            Assert.False(server.Status.Running);
            Assert.True(joystick.Released);
        }

        [Fact]
        public void MissingVJoyIsOfferedToBeInstalledOnce()
        {
            joystickSetup.Ready = false;
            view.AnswerInstallJoystick = false;
            view.Show();

            Assert.Equal(1, view.JoystickQuestions);
            Assert.True(view.JoystickWarning);
            Assert.True(settings.JoystickPromptShown);
            Assert.Equal(0, joystickSetup.Setups);

            view.Close();
            view.Show();
            Assert.Equal(1, view.JoystickQuestions);
            Assert.True(view.JoystickWarning);
        }

        [Fact]
        public void InstalledVJoyHidesTheWarning()
        {
            joystickSetup.Ready = false;
            joystick.IsAvailable = false;
            view.Show();

            Assert.Equal(1, joystickSetup.Setups);
            Assert.False(view.JoystickWarning);
            Assert.False(view.JoystickBusy);
            Assert.Empty(view.Warnings);
        }

        [Fact]
        public void RefusedRightsKeepTheButtonWithoutError()
        {
            joystickSetup.Ready = false;
            joystickSetup.Accepted = false;
            settings.JoystickPromptShown = true;
            view.Show();

            view.InstallJoystick();

            Assert.Equal(1, joystickSetup.Setups);
            Assert.True(view.JoystickWarning);
            Assert.False(view.JoystickBusy);
            Assert.Empty(view.Warnings);
        }

        [Fact]
        public void FailedSetupIsReported()
        {
            joystickSetup.Ready = false;
            joystickSetup.Works = false;
            settings.JoystickPromptShown = true;
            view.Show();

            view.InstallJoystick();

            Assert.True(view.JoystickWarning);
            Assert.Equal(new[] { Warning.JoystickSetupFailed }, view.Warnings);
        }

        [Fact]
        public void ReadyVJoyIsNotMentioned()
        {
            view.Show();

            Assert.False(view.JoystickWarning);
            Assert.Equal(0, view.JoystickQuestions);
        }
    }
}
