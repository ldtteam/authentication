using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.Discord.Config
{
    public class DiscordConfig
    {
        public required string ClientId { get; set; }
        
        public required string ClientSecret { get; set; }
    }
}