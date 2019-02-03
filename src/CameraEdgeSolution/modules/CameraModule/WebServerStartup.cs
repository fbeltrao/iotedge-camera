using CameraModule.Models;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CameraModule
{
    class WebServerStartup
    {
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<WebServerStartup>();

        public WebServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMediatR((c) => c.AsSingleton(), typeof(WebServerStartup).Assembly);
            services.AddModuleClient(new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only));        
            services.AddSingleton<CameraConfiguration>(CameraConfiguration.CreateFromEnvironmentVariables());
            services.AddSingleton<ICamera, PiCamera>();
            //services.AddSingleton<ICamera, TestCamera>();
            //services.AddSingleton<IoTHubModuleConnector>();
            // services.AddSingleton<IHostedService, IoTHubModuleConnector>(sp => {
            //     return (IoTHubModuleConnector)sp.GetService(typeof(IoTHubModuleConnector));
            // });

            services.AddSignalR();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseSignalR(routes =>
            {
                routes.MapHub<CameraHub>("/cameraHub");
            });

            app.UseModuleClientMediatorExtensions();
            app.UseCamera();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
