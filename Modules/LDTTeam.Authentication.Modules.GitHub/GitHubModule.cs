using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.GitHub.Condition;
using LDTTeam.Authentication.Modules.GitHub.Services;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Extensions;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.GitHub.Config;
using LDTTeam.Authentication.Modules.GitHub.Data;
using LDTTeam.Authentication.Modules.GitHub.EventHandlers;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.GitHub
{
    public class GitHubModule : IModule
    {
        public string ModuleName => "GitHub";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
        {
            GitHubConfig? githubConfig = configuration.GetSection("github").Get<GitHubConfig>();

            if (githubConfig == null)
                throw new Exception("github not set in configuration!");
            
            return builder.AddGitHub(o =>
            {
                o.ClientId = githubConfig.ClientId;
                o.ClientSecret = githubConfig.ClientSecret;

                o.SaveTokens = true;
            });
        }

        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            return services
                .AddDbContext<GitHubDatabaseContext>(x =>
                    x.UseNpgsql(configuration.CreateConnectionString("github"),
                        b => b.MigrationsAssembly("LDTTeam.Authentication.Modules.GitHub")))
                .AddScoped<GithubRefreshEventHandler>()
                .AddTransient<GitHubService>()
                .AddStartupTask<GitHubDatabaseMigrationTask>();
        }

        public void EventsSubscription(IServiceProvider services, EventsService events, CancellationToken token)
        {
            events.RefreshContentEvent += async (scope, modules) =>
            {
                if (modules != null && modules.All(x => !x.Equals("github", StringComparison.InvariantCultureIgnoreCase)))
                    return;

                try
                {
                    await scope.ServiceProvider.GetRequiredService<GithubRefreshEventHandler>().ExecuteAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            };

            events.ConditionRegistration += () =>
            {
                Conditions.Registry.Add(new GitHubCondition());
                return Task.CompletedTask;
            };
        }
    }
}