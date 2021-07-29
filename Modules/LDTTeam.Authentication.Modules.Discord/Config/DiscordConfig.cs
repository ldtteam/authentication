using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.Discord.Config
{
    public class DiscordConfig
    {
        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }
        
        public string BotToken { get; set; }
        
        public ulong LoggingChannel { get; set; }
        
        // <DiscordServerId, <RewardId, RoleId>>
        public Dictionary<string, Dictionary<string, List<ulong>>> RoleMappings { get; set; } = new();
        
        public bool RemoveUsersFromRoles { get; set; }

        public List<ulong> UserExceptions { get; set; } = new();
    }
}