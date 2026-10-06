using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Input;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.Protocol;
using Xunit;

namespace TruckRemoteServer.Tests
{
    public sealed class SetupWizardTests : IDisposable
    {
        private readonly FakeWizardView view = new FakeWizardView();
        private readonly FakeJoystick joystick = new FakeJoystick();
        private readonly FakeSettings settings = new FakeSettings { Port = 0 };
        private readonly FakeFirewall firewall = new FakeFirewall();
        private readonly FakePluginSetup pluginSetup = new FakePluginSetup();
        private readonly FakeJoystickSetup joystickSetup = new FakeJoystickSetup();
        private readonly ControllerInputMapper mapper;
        private readonly ControllerServer server;
        private readonly SetupWizard wizard;

        public SetupWizardTests()
        {
            mapper = new ControllerInputMapper(new FakeKeyboard(), joystick);
            server = new ControllerServer(mapper, new FakeTelemetry(), joystick, new NoTimerResolution(),
                NullLogger<ControllerServer>.Instance);
            wizard = new SetupWizard(view, server, mapper, joystick, settings, firewall, new FakeNetwork(),
                pluginSetup, joystickSetup, "server.exe", work =>
                {
                    work();
                    return Task.CompletedTask;
                });
        }

        public void Dispose() => server.Stop();

        [Fact]
        public void ReadyPcHasEveryStepDoneButThePhone()
        {
            server.Start(0);
            wizard.Check();

            Assert.Equal(StepState.Done, view.Steps[SetupStep.TelemetryPlugin]);
            Assert.Equal(StepState.Done, view.Steps[SetupStep.Joystick]);
            Assert.Equal(StepState.Done, view.Steps[SetupStep.Firewall]);
            Assert.Equal(StepState.Done, view.Steps[SetupStep.GameControls]);
            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.Phone]);
            Assert.Equal(new[] { "192.168.1.10" }, view.Addresses);
            Assert.Null(view.Controls);
        }

        [Fact]
        public void MissingPartsAreFixedByTheirButtons()
        {
            pluginSetup.IsInstalled = false;
            joystickSetup.Ready = false;
            joystick.IsAvailable = false;
            firewall.Status = FirewallStatus.NoRule;
            wizard.Check();

            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.TelemetryPlugin]);
            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.Joystick]);
            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.Firewall]);

            view.Fix(SetupStep.TelemetryPlugin);
            view.Fix(SetupStep.Joystick);
            view.Fix(SetupStep.Firewall);

            Assert.Equal(StepState.Done, view.Steps[SetupStep.TelemetryPlugin]);
            Assert.Equal(StepState.Done, view.Steps[SetupStep.Joystick]);
            Assert.Equal(StepState.Done, view.Steps[SetupStep.Firewall]);
            Assert.Equal(1, joystick.Initializations);
        }

        [Fact]
        public void FailedFixesSayWhy()
        {
            pluginSetup.IsInstalled = false;
            pluginSetup.InstallError = new InvalidOperationException("No game found");
            joystickSetup.Ready = false;
            joystickSetup.Works = false;
            wizard.Check();

            view.Fix(SetupStep.TelemetryPlugin);
            view.Fix(SetupStep.Joystick);

            Assert.Equal(StepState.Failed, view.Steps[SetupStep.TelemetryPlugin]);
            Assert.Equal("No game found", view.Details[SetupStep.TelemetryPlugin]);
            Assert.Equal(StepState.Failed, view.Steps[SetupStep.Joystick]);
        }

        [Fact]
        public void RefusedRightsChangeNothing()
        {
            joystickSetup.Ready = false;
            joystickSetup.Accepted = false;
            firewall.Status = FirewallStatus.Blocked;
            firewall.AllowAccepted = false;
            wizard.Check();

            view.Fix(SetupStep.Joystick);
            view.Fix(SetupStep.Firewall);

            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.Joystick]);
            Assert.Equal(StepState.NeedsAction, view.Steps[SetupStep.Firewall]);
        }

        [Fact]
        public void UnknownFirewallIsNotAProblem()
        {
            firewall.Status = FirewallStatus.Unknown;
            wizard.Check();

            Assert.Equal(StepState.Done, view.Steps[SetupStep.Firewall]);
        }

        [Fact]
        public void StoppedServerFailsThePhoneStep()
        {
            wizard.Check();

            Assert.Equal(StepState.Failed, view.Steps[SetupStep.Phone]);
        }

        [Fact]
        public void ConnectedPhoneShowsItsControlsLive()
        {
            server.Start(0);
            wizard.Check();
            using (var phone = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                byte[] hello = Encoding.UTF8.GetBytes("TruckRemoteHello2");
                phone.Send(hello, hello.Length, new IPEndPoint(IPAddress.Loopback, server.Port));
                WaitFor(() => server.Status.ControllerConnected);

                mapper.Apply(new ControllerMessage
                {
                    Steering = -9.80665f / 2,
                    SteeringIsFinal = true,
                    HasPedalLevels = true,
                    GasLevel = 0.25,
                    BrakeLevel = 0
                });
                view.RaiseTick();
            }

            Assert.Equal(StepState.Done, view.Steps[SetupStep.Phone]);
            Assert.InRange(view.Controls.Steering, -0.51, -0.49);
            Assert.Equal(0.25, view.Controls.Gas, 3);
            Assert.Equal(0, view.Controls.Brake);
        }

        [Fact]
        public void DigitalPedalsAreFullOrNothing()
        {
            mapper.Apply(new ControllerMessage { GasPressed = true });

            Assert.Equal(1, mapper.Controls.Gas);
            Assert.Equal(0, mapper.Controls.Brake);
            mapper.ReleaseControls();
            Assert.Same(ControlsSnapshot.Released, mapper.Controls);
        }

        [Fact]
        public void TheWizardIsShownOnceByItself()
        {
            wizard.Run();

            Assert.Equal(1, view.Shows);
            Assert.True(settings.SetupWizardShown);
        }

        private static void WaitFor(Func<bool> condition)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                if (clock.ElapsedMilliseconds > 3000) throw new TimeoutException();
                Thread.Sleep(10);
            }
        }
    }
}
