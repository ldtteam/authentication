using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.Discord.Models
{
    // <rewardId, discordUsers>
    public record Event(Dictionary<string, List<ulong>> UserRewardMappings);
}