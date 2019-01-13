namespace CameraModule
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;

    class Program
    {
        static async Task CreateGenericHost(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddEnvironmentVariables();
                    configHost.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<CameraConfiguration>(CameraConfiguration.CreateFromEnvironmentVariables());

                    services.AddSingleton<ICamera, PiCamera>();
                    //services.AddSingleton<ICamera, TestCamera>();
                    services.AddSingleton<IoTHubModuleConnector>();
                    services.AddSingleton<IHostedService, IoTHubModuleConnector>(sp => {
                        return (IoTHubModuleConnector)sp.GetService(typeof(IoTHubModuleConnector));
                    });
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    //configLogging.AddConsole();
                    //configLogging.AddDebug();
                })
                .UseConsoleLifetime();

            await hostBuilder.RunConsoleAsync();
        }

        static async Task Main(string[] args)
        {
            var webServerFlag = Environment.GetEnvironmentVariable("webserver");
            if (string.Equals(webServerFlag, "true", StringComparison.InvariantCultureIgnoreCase) || string.Equals(webServerFlag, "1"))
            {
                 WebServerStartup.CreateWebHostBuilder(args).Build().Run();
            }
            else
            {
                await CreateGenericHost(args);      
            }
        }
    }
}
