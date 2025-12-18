using System.Net.Http.Headers;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LDTTeam.Authentication.PatreonApiUtils.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDatabase()
        {
            builder.Services.AddDbContext<DatabaseContext>(x =>
            {
                x.UseNpgsql(builder.Configuration.CreateConnectionString("patreon"),
                    b => b.MigrationsAssembly("LDTTeam.Authentication.PatreonApiUtils"));
            });
            return builder;
        }
        
        public IHostApplicationBuilder AddPatreonConfiguration()
        {
            builder.Services.AddOptions<PatreonConfig>()
                .BindConfiguration("Patreon");

            return builder;
        }
        
        public IHostApplicationBuilder AddPatreonTokenManagement()
        {
            builder.Services.AddMemoryCache();
            builder.Services.TryAddScoped<IPatreonTokenService, PatreonTokenService>(); 
            builder.Services.AddHttpClient("PatreonTokenClient", client =>
            {
                client.BaseAddress = new Uri("https://www.patreon.com/");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
                    new ProductHeaderValue("LDTTeam Authentication Patreon Service Token Handler", Environment.GetEnvironmentVariable("VERSION") ?? "Unknown"))
                );
            });

            return builder;
        }
        
        public IHostApplicationBuilder AddPatreonApiService()
        {
            builder.Services.AddHttpClient("PatreonApiClient", client =>
            {
                client.BaseAddress = new Uri("https://www.patreon.com/");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
                    new ProductHeaderValue("LDTTeam Authentication Patreon API Handler", Environment.GetEnvironmentVariable("VERSION") ?? "Unknown"))
                );
            });
            builder.Services.AddScoped<IPatreonDataService, PatreonDataService>();

            return builder;
        }
        
        public IHostApplicationBuilder AddPatreonMembershipService()
        {
            builder.Services.AddScoped<IPatreonMembershipService, PatreonMembershipService>();
            return builder;
        }
        
        public IHostApplicationBuilder AddRepositories()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
            builder.Services.AddScoped<IRewardRepository, RewardRepository>();
            return builder;
        }
    }
}