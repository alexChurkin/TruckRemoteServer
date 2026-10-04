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
        private readonly FakeSettings settings = new FakeSettings { Port = 0, Sensitivity = 70 };
        private readonly FakeFirewall firewall = new FakeFirewall();
        private readonly FakeNetwork network = new FakeNetwork();
        private readonly FakePluginSetup pluginSetup = new FakePluginSetup();
        private readonly ControllerInputMapper mapper;
        private readonly ControllerServer server;
        private readonly MainPresenter presenter;

        public MainPresenterTests()
        {
            mapper = new ControllerInputMapper(new FakeKeyboard(), joystick);
            server = new ControllerServer(mapper, new FakeTelemetry(), joystick, new NoTimerResolution(),
                NullLogger<ControllerServer>.Instance);
            presenter = new MainPresenter(view, server, mapper, joystick, settings, firewall, network, pluginSetup, "server.exe")
            {
                RunInBackground = work => System.Threading.Tasks.Task.FromResult(work())
            };
        }

        public void Dispose() => server.Stop();

        [Fact]
        public void ShownWindowGetsSettingsAddressesAndRunningServer()
        {
            view.Show();

            Assert.Equal(0, view.Port);
            Assert.Equal(70, view.Sensitivity);
            Assert.Equal(70, mapper.SteeringSensitivity);
            Assert.Equal(new[] { "192.168.1.10" }, view.Addresses);
            Assert.Equal(ServerState.WaitingForController, view.State);
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
        public void SettingsChangesAreSaved()
        {
            view.Show();
            view.ChangeSensitivity(30);

            Assert.Equal(30, mapper.SteeringSensitivity);
            Assert.Equal(30, settings.Sensitivity);
            Assert.True(settings.Saves >= 1);
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
    }
}
