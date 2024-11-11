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
            DiscordConfig? discordConfig = configuration.GetSection("discord").Get<DiscordConfig>();

            if (discordConfig == null)
                throw new Exception("discord not set in configuration!");

            return services.AddHostedService<WebhookLoggingQueueService>()
                .AddHostedService<DiscordBackgroundService>()
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
            {
                IConditionService conditionService = sp.ServiceProvider.GetRequiredService<IConditionService>();
                Dictionary<string, List<string>> rewards = await conditionService.GetRewardsForProvider("discord", token);
                IDiscordRestGuildAPI guildApi = sp.ServiceProvider.GetRequiredService<IDiscordRestGuildAPI>();
                IConfiguration configuration = sp.ServiceProvider.GetRequiredService<IConfiguration>();

                DiscordConfig? discordConfig = configuration.GetSection("discord").Get<DiscordConfig>();

                if (discordConfig == null)
                    throw new Exception("discord not set in configuration!");

                foreach (string server in discordConfig.RoleMappings.Keys)
                {
                    if (!ulong.TryParse(server, out ulong serverId)) continue;

                    Snowflake serverSnowflake = new(serverId);

                    Result<IGuild> guildResult = await guildApi.GetGuildAsync(serverSnowflake, true);
                    if (!guildResult.IsSuccess || guildResult.Entity == null) continue;

                    Result<IReadOnlyList<IRole>> rolesResult = await guildApi.GetGuildRolesAsync(serverSnowflake);
                    if (!rolesResult.IsSuccess || rolesResult.Entity == null) continue;

                    List<IGuildMember> members = new();

                    bool errored = false;
                    // ReSharper disable once PossibleLossOfFraction
                    for (int i = 0;
                        i <= Math.Ceiling((double) (guildResult.Entity.ApproximateMemberCount.Value / 1000));
                        i++)
                    {
                        Optional<Snowflake> snowflake;
                        if (members.LastOrDefault()?.User.HasValue == true &&
                            members.LastOrDefault()?.User.Value.ID != null)
                            snowflake = new Optional<Snowflake>(members.LastOrDefault()!.User.Value.ID);
                        else
                            snowflake = new Optional<Snowflake>();

                        Result<IReadOnlyList<IGuildMember>> membersResult =
                            await guildApi.ListGuildMembersAsync(
                                serverSnowflake,
                                1000, snowflake);

                        if (!membersResult.IsSuccess || membersResult.Entity == null)
                        {
                            errored = true;
                            break;
                        }

                        members.AddRange(membersResult.Entity);
                    }

                    if (errored) continue;

                    Dictionary<string, List<Snowflake>> rewardRoles = discordConfig.RoleMappings[server]
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value.Select(y => new Snowflake(y)).ToList()
                        );

                    Dictionary<IGuildMember, IReadOnlyList<Snowflake>> memberRoles =
                        members.ToDictionary(
                            x => x,
                            x => x.Roles
                        );

                    foreach ((IGuildMember user, IReadOnlyList<Snowflake> roles) in memberRoles)
                    {
                        if (user.User.HasValue == false) continue;

                        Snowflake userSnowflake = user.User.Value.ID;

                        List<string> userRewards = rewards
                            .Where(x => x.Value.Contains(userSnowflake.ToString()))
                            .Select(x => x.Key)
                            .ToList();

                        // roles to award
                        List<Snowflake> rewardedRoles = rewardRoles
                            .Where(x => userRewards.Contains(x.Key))
                            .SelectMany(x => x.Value)
                            .Distinct()
                            .Select(x => x)
                            .ToList();

                        // roles not rewarded less rewardedRoles
                        List<Snowflake> notRewardedRoles = rewardRoles
                            .Where(x => !userRewards.Contains(x.Key))
                            .SelectMany(x => x.Value)
                            .Where(x => !rewardedRoles.Contains(x))
                            .Distinct()
                            .Select(x => x)
                            .ToList();

                        List<IRole> rolesToAdd =
                            (from rewardRole in rewardedRoles
                                let role = rolesResult.Entity.FirstOrDefault(x => x.ID == rewardRole)
                                where !roles.Contains(rewardRole) && role != null
                                select role).ToList()!;

                        List<IRole> rolesToRemove =
                            (from notRewardRole in notRewardedRoles
                                let role = rolesResult.Entity.FirstOrDefault(x => x.ID == notRewardRole)
                                where roles.Contains(notRewardRole) && role != null
                                select role).ToList()!;
                        
                        // don't add roles if only optionals being added
                        if (!rolesToAdd.All(x => discordConfig.OptionalRoles.Contains(x.ID.Value)))
                        {
                            foreach (IRole role in rolesToAdd)
                            {
                                await guildApi.AddGuildMemberRoleAsync(serverSnowflake, userSnowflake, role.ID,
                                    "LDTTeam Auth user has rewards for this role");
                            }
                        }

                        if (!discordConfig.RemoveUsersFromRoles ||
                            discordConfig.UserExceptions.Contains(userSnowflake.Value)) continue;

                        foreach (IRole role in rolesToRemove)
                        {
                            await guildApi.RemoveGuildMemberRoleAsync(serverSnowflake, userSnowflake, role.ID,
                                "LDTTeam Auth user does not have rewards for this role");
                        }
                    }
                }
            };
        }
    }
}