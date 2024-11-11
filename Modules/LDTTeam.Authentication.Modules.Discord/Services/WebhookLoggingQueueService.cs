using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Logging;
using LDTTeam.Authentication.Modules.Discord.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.Modules.Discord.Services
{
    public class WebhookLoggingQueueService : BackgroundService
    {
        private readonly ILoggingQueue _loggingQueue;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IConfiguration _configuration;

        public WebhookLoggingQueueService(ILoggingQueue loggingQueue, IDiscordRestChannelAPI channelApi,
            IConfiguration configuration)
        {
            _loggingQueue = loggingQueue;
            _channelApi = channelApi;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DiscordConfig? discordConfig = _configuration.GetSection("discord").Get<DiscordConfig>();

            if (discordConfig == null)
                throw new Exception("discord not set in configuration!");

            Snowflake channelSnowflake = new(discordConfig.LoggingChannel);

            while (!stoppingToken.IsCancellationRequested)
            {
                Embed embed = await _loggingQueue.DequeueAsync(stoppingToken);

                await _channelApi.CreateMessageAsync(
                    channelSnowflake,
                    embeds: new[] {embed},
                    ct: stoppingToken
                );
            }
        }
    }
}