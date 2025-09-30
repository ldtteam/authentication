using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Discord.Config;
using Microsoft.Extensions.Configuration;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.Modules.Discord.Services;

public record DiscordRoleRewardService(
    IConditionService ConditionService,
    IConfiguration Configuration
    ) {

    public IEnumerable<(Snowflake Server, Snowflake Role)> Rewards
    {
        get
        {
            var discordConfig = Configuration.GetSection("discord").Get<DiscordConfig>();
            if (discordConfig == null)
                throw new Exception("Discord integration not configuration. Can not retrieve active reward roles!");
        
            foreach (var server in discordConfig.RoleMappings.Keys)
            {
                if (!ulong.TryParse(server, out ulong serverId))
                    throw new Exception($"Invalid server ID {server} in configuration. Can not retrieve active reward roles!");
            
                var serverSnowflake = new Snowflake(serverId);
                foreach (var (_, roles) in discordConfig.RoleMappings[server])
                {
                    foreach (var roleId in roles)
                    {
                        yield return (serverSnowflake, new Snowflake(roleId));
                    }
                }
            }
        }
    }

    public async IAsyncEnumerable<(Snowflake Server, Snowflake Role)> Active(Snowflake member, [EnumeratorCancellation] CancellationToken token)
    {
        var discordConfig = Configuration.GetSection("discord").Get<DiscordConfig>();
        if (discordConfig == null)
            throw new Exception("Discord integration not configuration. Can not retrieve active reward roles!");
        
        await foreach (var reward in ConditionService.GetActiveRewardsForUser("discord", member.ToString(), token))
        {
            foreach (var (server, rewardMappings) in discordConfig.RoleMappings)
            {
                if (!ulong.TryParse(server, out ulong serverId))
                    throw new Exception($"Invalid server ID {server} in configuration. Can not retrieve active reward roles!");
                
                if (!rewardMappings.ContainsKey(reward))
                    continue;

                var serverSnowflake = new Snowflake(serverId);
                foreach (var role in rewardMappings[reward])
                {
                    yield return (serverSnowflake, new Snowflake(role));
                }
            }
        }
    }
}