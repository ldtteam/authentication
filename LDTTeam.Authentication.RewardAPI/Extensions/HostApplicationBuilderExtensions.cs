using LDTTeam.Authentication.RewardAPI.Data;
using LDTTeam.Authentication.RewardAPI.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardAPI.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddRepositories()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IProviderLoginRepository, ProviderLoginRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAssignedRewardRepository, AssignedRewardRepository>();
            return builder;
        }
        
        public IHostApplicationBuilder AddDatabase()
        {
            builder.Services.AddDbContext<DatabaseContext>(x =>
            {
                x.UseNpgsql(builder.Configuration.CreateConnectionString("RewardApi"),
                    b => b.MigrationsAssembly("LDTTeam.Authentication.RewardAPI"));
            });
            return builder;
        }
    }
}