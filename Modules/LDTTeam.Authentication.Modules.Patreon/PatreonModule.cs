using System;
using System.Text.Json.Nodes;
using AspNet.Security.OAuth.Patreon;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Patreon.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
                
                o.Events.OnCreatingTicket += async (context) =>
                {
                    if (context.Scheme.Name != PatreonAuthenticationDefaults.AuthenticationScheme)
                        return;
                    
                    using var scope = builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()
                        .CreateScope();

                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<PatreonModule>>();
                    var payload = JsonObject.Create(context.User);
                    logger.LogInformation("Patreon user payload: {Payload}", payload?.ToJsonString());
                };
            });
        }
    }
}