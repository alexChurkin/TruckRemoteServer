using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TruckRemoteServer.Firewall;
using TruckRemoteServer.Infrastructure;
using TruckRemoteServer.Input;
using TruckRemoteServer.Network;
using TruckRemoteServer.Presentation;
using TruckRemoteServer.Settings;
using TruckRemoteServer.Telemetry;
using TruckRemoteServer.UI;
using TruckRemoteServer.Updates;

namespace TruckRemoteServer
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            //The server started itself with administrator rights to install vJoy
            if (args.Length > 0 && args[0] == VJoySetup.ElevatedArgument) return VJoySetup.RunElevated();
            //Started by an update: the old server releases the port first
            GitHubUpdater.FinishUpdate(Application.ExecutablePath, args);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ServiceProvider services = ConfigureServices())
            {
                var form = services.GetRequiredService<MainForm>();
                //Started with Windows: the server works minimized
                if (Array.IndexOf(args, RegistryAutostart.Argument) >= 0) form.WindowState = FormWindowState.Minimized;
                //The presenter subscribes to the view, it lives as long as the form
                services.GetRequiredService<MainPresenter>().Initialize();
                Application.Run(form);
            }
            return 0;
        }

        //Composition root: the platform implementations of the core interfaces are chosen here
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddDebug().SetMinimumLevel(LogLevel.Information));

            services.AddSingleton<KeyBindings>();
            services.AddSingleton<GameUnits>();
            services.AddSingleton<IKeyboard, SendInputKeyboard>();
            services.AddSingleton<IVirtualJoystick, VJoyJoystick>();
            services.AddSingleton<ITelemetrySource, ScsTelemetrySource>();
            services.AddSingleton<ITimerResolution, TimerResolution>();
            services.AddSingleton<ISettingsStore, PropertiesSettingsStore>();
            services.AddSingleton<IFirewall, WindowsFirewall>();
            services.AddSingleton<INetworkInfo, SystemNetworkInfo>();
            services.AddSingleton<ITelemetryPluginSetup, TelemetryPluginSetup>();
            services.AddSingleton<IJoystickSetup, VJoySetup>();
            services.AddSingleton<IGameControlsSetup, GameControlsSetup>();
            services.AddSingleton<IAutostart>(new RegistryAutostart(Application.ExecutablePath));
            Version version = typeof(Program).Assembly.GetName().Version;
            services.AddSingleton<IUpdater>(provider => new GitHubUpdater(Application.ExecutablePath, version,
                provider.GetRequiredService<ILogger<GitHubUpdater>>()));

            services.AddSingleton<ControllerInputMapper>();
            services.AddSingleton<ControllerServer>();

            services.AddSingleton<MainForm>();
            services.AddSingleton<IMainView>(provider => provider.GetRequiredService<MainForm>());
            services.AddSingleton(provider => new MainPresenter(
                provider.GetRequiredService<IMainView>(),
                provider.GetRequiredService<ControllerServer>(),
                provider.GetRequiredService<ControllerInputMapper>(),
                provider.GetRequiredService<IVirtualJoystick>(),
                provider.GetRequiredService<ISettingsStore>(),
                provider.GetRequiredService<IFirewall>(),
                provider.GetRequiredService<INetworkInfo>(),
                provider.GetRequiredService<ITelemetryPluginSetup>(),
                provider.GetRequiredService<IJoystickSetup>(),
                provider.GetRequiredService<IGameControlsSetup>(),
                Application.ExecutablePath,
                provider.GetRequiredService<IUpdater>(),
                provider.GetRequiredService<IAutostart>(),
                version));
            return services.BuildServiceProvider();
        }
    }
}
