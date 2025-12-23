using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Patreon;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Extensions;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;

namespace LDTTeam.Authentication.Modules.Patreon
{
    public class PatreonModule : IModule
    {
        public string ModuleName => "Patreon";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
        {
            return builder.AddPatreon(o =>
            {
                using var configScope = builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()
                    .CreateScope();

                var patreonConfig = configScope.ServiceProvider.GetRequiredService<IOptions<PatreonConfig>>().Value;
                
                o.ClientId = patreonConfig.ClientId;
                o.ClientSecret = patreonConfig.ClientSecret;
                
                o.Scope.Add("identity.memberships");
                o.Includes.Add("memberships");

                o.SaveTokens = true;
                
                o.Events.OnCreatingTicket += async (context) =>
                {
                    using var scope = builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()
                        .CreateScope();
                    
                    if (context.Scheme.Name != PatreonAuthenticationDefaults.AuthenticationScheme)
                        return;
                    
                    var membershipId = await FindPatreonMembershipId(JsonObject.Create(context.User), scope.ServiceProvider);
                    if (membershipId.HasValue)
                    {
                        context.Identity?.AddClaim(new Claim("patreon_membership_id",
                            membershipId.Value.ToString()));
                    }
                };
            });
        }
        
        private static async Task<Guid?> FindPatreonMembershipId(JsonObject? payload, IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<PatreonModule>>();
            var patreonApiService = services.GetRequiredService<IPatreonDataService>();
            var patreonConfig = services.GetRequiredService<IOptions<PatreonConfig>>();
            
            if (payload == null)
                return null;

            try
            {
                var data = payload["data"]?["relationships"]?["memberships"]?["data"]?.AsArray();
                if (data == null || data.Count == 0)
                    return null;
                
                foreach (var jsonNode in data)
                {
                    if (jsonNode?["type"]?.GetValue<string>() != "member")
                        continue;
                    
                    var membershipId = jsonNode["id"]?.GetValue<string>();
                    if (membershipId == null)
                        continue;
                    
                    try
                    {
                        var membershipData = await patreonApiService.GetFor(Guid.Parse(membershipId));
                        if (membershipData.HasValue && membershipData.Value.CampaignId == patreonConfig.Value.CampaignId)
                        {
                            return Guid.Parse(membershipId);
                        }
                    } catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while retrieving Patreon membership data for ID: {MembershipId}", membershipId);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while extracting Patreon membership ID from payload: {Payload}", payload.ToJsonString());
                return null;
            }
        }

        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services,
            WebApplicationBuilder builder)
        {
            builder.AddPatreonConfiguration();
            builder.AddPatreonDatabase();
            builder.AddPatreonTokenManagement();
            builder.AddPatreonApiService();
            return services;
        }

        public async Task OnUserSignIn(ClaimsPrincipal infoPrincipal, ApplicationUser user,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<PatreonModule>>();
            var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
            
            var claim =infoPrincipal.Claims
                .FirstOrDefault(c => c.Type == "patreon_membership_id");
            if (claim == null)
            {
                logger.LogWarning("No Patreon membership ID claim found for user {User}", infoPrincipal.Identity?.Name);
                return;
            }
            
            logger.LogInformation("User {User} has Patreon membership ID: {MembershipId}", infoPrincipal.Identity?.Name, claim.Value);
            await messageBus.PublishAsync(
                new PatreonMembershipCreatedOrUpdated(Guid.Parse(user.Id), Guid.Parse(claim.Value)));
        }
    }
}