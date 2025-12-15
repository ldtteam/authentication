using System.Runtime.InteropServices.ComTypes;
using JasperFx;
using JasperFx.Resources;
using LDTTeam.Authentication.Messages;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Postgresql;
using WolverineHostBuilder = Wolverine.HostBuilderExtensions;

namespace LDTTeam.Authentication.Utils.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddWolverine()
        {
            builder.UseWolverine(options =>
            {
                options.PersistMessagesWithPostgresql(builder.Configuration.CreateConnectionString("wolverine")).EnableMessageTransport();
                options.AutoBuildMessageStorageOnStartup = AutoCreate.All;
                options.Durability.EnableInboxPartitioning = true;
                options.PublishAllMessages().ToPostgresqlQueue("messages");
                options.ListenToPostgresqlQueue("messages")
                    .CircuitBreaker()
                    .MaximumMessagesToReceive(50);
                options.Discovery.IncludeAssembly(typeof(LDTTeamAuthenticationAssemblyMarker).Assembly);
            });
            builder.Services.AddResourceSetupOnStartup();
            return builder;
        }
    }
}