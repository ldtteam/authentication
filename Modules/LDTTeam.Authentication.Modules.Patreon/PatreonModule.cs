using System;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Extensions;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Patreon.Condition;
using LDTTeam.Authentication.Modules.Patreon.Config;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.EventHandlers;
using LDTTeam.Authentication.Modules.Patreon.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
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

        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            return services.AddDbContext<PatreonDatabaseContext>(x =>
                    x.UseNpgsql(configuration.GetConnectionString("postgres_patreon"),
                        b => b.MigrationsAssembly("LDTTeam.Authentication.Modules.Patreon")))
                .AddScoped<PatreonRefreshEventHandler>()
                .AddTransient<PatreonService>()
                .AddStartupTask<PatreonDatabaseMigrationTask>();
        }

        public void EventsSubscription(IServiceProvider services, EventsService events)
        {
            events.RefreshContentEvent += async (scope, modules) =>
            {
                if (modules != null &&
                    modules.All(x => !x.Equals("patreon", StringComparison.InvariantCultureIgnoreCase)))
                    return;

                await scope.ServiceProvider.GetRequiredService<PatreonRefreshEventHandler>().ExecuteAsync();
            };

            events.ConditionRegistration += () =>
            {
                Conditions.Registry.Add(new PatreonCondition());
                return Task.CompletedTask;
            };
        }
    }
}