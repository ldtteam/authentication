using System.Runtime.InteropServices.ComTypes;
using JasperFx;
using JasperFx.Resources;
using LDTTeam.Authentication.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Kafka;
using Wolverine.Postgresql;
using WolverineHostBuilder = Wolverine.HostBuilderExtensions;

namespace LDTTeam.Authentication.Utils.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddWolverine(Action<WolverineOptions>? configure = null)
        {
            builder.UseWolverine(options =>
            {
                options.PersistMessagesWithPostgresql(builder.Configuration.CreateConnectionString("wolverine"));
                if (builder.Environment.IsDevelopment())
                {
                    options.UseKafka("localhost:9092")
                        .AutoProvision();
                }
                else
                {
                    options.UseKafka("auth-comms-kafka-brokers:9092")
                        .AutoProvision();
                }
                
                options.PublishAllMessages()
                    .ToKafkaTopic("messages");
                options.ListenToKafkaTopic("messages");
                
                options.Discovery.IncludeAssembly(typeof(LDTTeamAuthenticationAssemblyMarker).Assembly);
                
                configure?.Invoke(options);
            });
            builder.Services.AddResourceSetupOnStartup();
            return builder;
        }

        public IHostApplicationBuilder AddLogging()
        {
            if (!builder.Environment.IsDevelopment())
                builder.Logging.AddJsonConsole();

            return builder;
        }

        public IHostApplicationBuilder AddConfiguration()
        {
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables("LDTTEAM_AUTH_");
            builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

            return builder;
        }
    }
}