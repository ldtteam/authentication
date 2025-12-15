using System.Runtime.Serialization;
using LDTTeam.Authentication.RewardsService.Data;
using LDTTeam.Authentication.RewardsService.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace LDTTeam.Authentication.RewardsService.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDatabase()
        {
            builder.Services.AddDbContext<DatabaseContext>(x =>
            {
                x.UseNpgsql(builder.Configuration.CreateConnectionString("rewards"),
                    b => b.MigrationsAssembly("LDTTeam.Authentication.RewardsService"));
            });
            return builder;
        }

        public IHostApplicationBuilder AddRepositories()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IUserTiersRepository, UserTiersRepository>();
            builder.Services.AddSingleton<IRewardCalculationsRepository, RewardCalculationsRepository>();
            builder.Services.AddSingleton<IUserLifetimeContributionsRepository, UserLifetimeContributionsRepository>();
            builder.Services.AddSingleton<IUserRewardAssignmentsRepository, UserRewardAssignmentsRepository>();
            return builder;
        }
        
        public IHostApplicationBuilder AddCalculationService()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IRewardsCalculationService, RewardsCalculationService>();
            return builder;
        }
    }
}