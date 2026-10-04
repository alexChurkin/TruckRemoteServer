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

namespace TruckRemoteServer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ServiceProvider services = ConfigureServices())
            {
                var form = services.GetRequiredService<MainForm>();
                //The presenter subscribes to the view, it lives as long as the form
                services.GetRequiredService<MainPresenter>().Initialize();
                Application.Run(form);
            }
        }

        //Composition root: the platform implementations of the core interfaces are chosen here
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddDebug().SetMinimumLevel(LogLevel.Information));

            services.AddSingleton<IKeyboard, SendInputKeyboard>();
            services.AddSingleton<IVirtualJoystick, VJoyJoystick>();
            services.AddSingleton<ITelemetrySource, Ets2TelemetrySource>();
            services.AddSingleton<ITimerResolution, TimerResolution>();
            services.AddSingleton<ISettingsStore, PropertiesSettingsStore>();
            services.AddSingleton<IFirewall, WindowsFirewall>();
            services.AddSingleton<INetworkInfo, SystemNetworkInfo>();
            services.AddSingleton<ITelemetryPluginSetup, TelemetryPluginSetup>();

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
                Application.ExecutablePath));
            return services.BuildServiceProvider();
        }
    }
}
