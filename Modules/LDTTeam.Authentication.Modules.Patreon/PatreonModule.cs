using System;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Patreon.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Patreon
{
    public class PatreonModule : IModule
    {
        public string ModuleName => "Patreon";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
        {
            PatreonConfig? patreonConfig = configuration.GetSection("patreon").Get<PatreonConfig>();

            if (patreonConfig == null)
                throw new Exception("patreon not set in configuration!");

            return builder.AddPatreon(o =>
            {
                o.ClientId = patreonConfig.ClientId;
                o.ClientSecret = patreonConfig.ClientSecret;

                o.SaveTokens = true;
            });
        }
    }
}