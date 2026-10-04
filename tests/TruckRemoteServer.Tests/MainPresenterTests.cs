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

        public MainPresenterTests()
        {
            mapper = new ControllerInputMapper(new FakeKeyboard(), joystick);
            server = new ControllerServer(mapper, new FakeTelemetry(), joystick, new NoTimerResolution(),
                NullLogger<ControllerServer>.Instance);
            new MainPresenter(view, server, mapper, joystick, settings, firewall, network, pluginSetup, "server.exe")
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
            Assert.True(view.Running);
            Assert.Equal("Enabled", view.Status);
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
            view.ChangePort(18300);

            Assert.Equal(30, mapper.SteeringSensitivity);
            Assert.Equal(30, settings.Sensitivity);
            Assert.Equal(18300, settings.Port);
            Assert.True(settings.Saves >= 2);
        }

        [Fact]
        public void StopAndStartChangeTheStatus()
        {
            view.Show();
            view.Stop();
            Assert.False(view.Running);
            Assert.Equal("Disabled", view.Status);

            view.Start();
            Assert.True(view.Running);
        }

        [Fact]
        public void MissingPluginIsInstalledAndItsFailureDoesntStopTheServer()
        {
            pluginSetup.IsInstalled = false;
            pluginSetup.InstallError = new InvalidOperationException("no game");
            view.Show();

            Assert.Equal(1, pluginSetup.Installs);
            Assert.Single(view.Warnings);
            Assert.True(view.Running);
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
