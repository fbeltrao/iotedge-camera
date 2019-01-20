using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MMALSharp;

namespace CameraModule.Models
{
    // Notification that module client changed
    public class ModuleTwinChangedNotification : INotification
    {
        private TwinCollection twin;
        public TwinCollection Twin => twin;

        public ModuleTwinChangedNotification(TwinCollection twin)
        {
            this.twin = twin;
        }
    }

    public class ModuleDirectMethodRequest : IRequest<MethodResponse>
    {
        public MethodRequest MethodRequest { get; }

        public ModuleDirectMethodRequest(MethodRequest methodRequest)
        {
            MethodRequest = methodRequest;
        }

    }

    public class ModuleMessageRequest : IRequest<MessageResponse>
    {
        public Message Message { get;  }

        public ModuleMessageRequest(Message message)
        {
            this.Message = message;
        }

    }

    public static class MediatorExtensionsForModuleClient
    {
        // Adds ModuleClient as services collection
        public static void AddModuleClient(this IServiceCollection services, ITransportSettings[] transportSettings)
        {
            var moduleClient = ModuleClient.CreateFromEnvironmentAsync(transportSettings).GetAwaiter().GetResult();
            moduleClient.OpenAsync().GetAwaiter().GetResult();
            Logger.Log("Initialized module");

            services.AddSingleton(typeof(ModuleClient), moduleClient);

        }

        // Adds ModuleClient as services collection
        public static void AddModuleClient(this IServiceCollection services, ITransportSettings transportSettings)
        {
            var moduleClient = ModuleClient.CreateFromEnvironmentAsync(new ITransportSettings[] { transportSettings }).GetAwaiter().GetResult();
            moduleClient.OpenAsync().GetAwaiter().GetResult();
            Logger.Log("Initialized module");
            
            services.AddSingleton(typeof(ModuleClient), moduleClient);
        }


        public static IApplicationBuilder UseModuleClientMediatorExtensions(this IApplicationBuilder app)
        {
            var moduleClient = app.ApplicationServices.GetService<ModuleClient>();
            if (moduleClient == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                    "'IServiceCollection.AddModuleClient' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            var logger = app.ApplicationServices.GetService<ILogger>();

            moduleClient.SetDesiredPropertyUpdateCallbackAsync(async (p, c) => {
                var mediator = app.ApplicationServices.GetService<IMediator>();
                if (mediator != null)
                {
                    await mediator.Publish(new ModuleTwinChangedNotification(p));
                }
            }, null);
            logger?.LogDebug("Module twin changes notification initialized for mediatR");


            moduleClient.SetMethodDefaultHandlerAsync(async (methodRequest, _) => {
                var mediator = app.ApplicationServices.GetService<IMediator>();
                if (mediator != null)
                {
                    return await mediator.Send(new ModuleDirectMethodRequest(methodRequest));
                }

                return new MethodResponse((int)HttpStatusCode.NotFound);

            }, null);
            logger?.LogDebug("Module direct methods event handler initialized for mediatR");


            moduleClient.SetMessageHandlerAsync(async (message, _) => {
                var mediator = app.ApplicationServices.GetService<IMediator>();
                if (mediator != null)
                {
                    return await mediator.Send(new ModuleMessageRequest(message));
                }

                return MessageResponse.None;
            }, null);
            logger?.LogDebug("Module events handler initialized for mediatR");

            return app;
        }


    }
}