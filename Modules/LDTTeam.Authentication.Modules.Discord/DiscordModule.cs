using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Extensions;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Discord.Commands;
using LDTTeam.Authentication.Modules.Discord.Config;
using LDTTeam.Authentication.Modules.Discord.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord
{
    public class DiscordModule : IModule
    {
        public string ModuleName => "Discord";

        public AuthenticationBuilder ConfigureAuthentication(
            IConfiguration        configuration,
            AuthenticationBuilder builder
        )
        {
            DiscordConfig? discordConfig = configuration.GetSection("discord").Get<DiscordConfig>();

            if (discordConfig == null)
                throw new Exception("discord not set in configuration!");

            return builder.AddDiscord(o =>
            {
                o.ClientId = discordConfig.ClientId;
                o.ClientSecret = discordConfig.ClientSecret;

                o.SaveTokens = true;
            });
        }

        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            DiscordConfig? discordConfig = configuration.GetSection("discord").Get<DiscordConfig>();

            if (discordConfig == null)
                throw new Exception("discord not set in configuration!");

            return services.AddHostedService<WebhookLoggingQueueService>()
                .AddHostedService<DiscordBackgroundService>()
                .AddHostedService<DiscordSyncRolesBackgroundService>()
                .AddScoped<DiscordRoleSyncService>()
                .AddStartupTask<DiscordStartupTask>()
                .AddDiscordGateway(_ => discordConfig.BotToken)
                .AddDiscordCommands(true)
                .AddCommandGroup<MyRewardsCommands>()
                .AddCommandGroup<RewardsCommands>()
                .AddCommandGroup<RefreshCommand>()
                .AddDiscordCaching();
        }

        public void EventsSubscription(IServiceProvider services, EventsService events, CancellationToken token)
        {
            events.PostRefreshContentEvent += async sp =>
                await sp.ServiceProvider.GetRequiredService<DiscordRoleSyncService>()
                    .RunSync(token);
        }
    }
}