using System;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Discord.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Discord
{
    public class DiscordModule : IModule
    {
        public string ModuleName => "Discord";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration, AuthenticationBuilder builder)
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
    }
}