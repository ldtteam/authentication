using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Logging;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Discord.Config;
using LDTTeam.Authentication.Modules.Discord.Models;
using LDTTeam.Authentication.Modules.Discord.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace LDTTeam.Authentication.Modules.Discord
{
    public class DiscordModule : IModule
    {
        public string ModuleName => "Discord";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
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
            return services.AddHostedService<WebhookLoggingQueueService>()
                .AddSingleton(Channel.CreateBounded<Embed>(new BoundedChannelOptions(500)));
        }

        public void EventsSubscription(IServiceProvider services, EventsService events)
        {
            events.PostRefreshContentEvent += async sp =>
            {
                IConditionService conditionService = sp.ServiceProvider.GetRequiredService<IConditionService>();
                Dictionary<string, List<string>> rewards = await conditionService.GetRewardsForProvider("discord");

                Event rewardEvent = new(rewards
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value
                            .Where(y => ulong.TryParse(y, out ulong _))
                            .Select(ulong.Parse).ToList()
                    )
                );

                ConnectionFactory factory = new() {HostName = "localhost"};
                using IConnection connection = factory.CreateConnection();
                using IModel model = connection.CreateModel();

                model.QueueDeclare("events",
                    false,
                    false,
                    false,
                    null);

                string message = JsonSerializer.Serialize(rewardEvent);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                model.BasicPublish("",
                    "events",
                    null,
                    messageBytes);
            };
        }
    }
}