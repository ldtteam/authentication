using LDTTeam.Authentication.DiscordBot.Commands;
using LDTTeam.Authentication.DiscordBot.Config;
using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Remora.Commands.Extensions;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Rest;

namespace LDTTeam.Authentication.DiscordBot.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDatabase()
        {
            builder.Services.AddDbContext<DatabaseContext>(x =>
            {
                x.UseNpgsql(builder.Configuration.CreateConnectionString("discord"),
                    b => b.MigrationsAssembly("LDTTeam.Authentication.DiscordBot"));
            });
            return builder;
        }

        public IHostApplicationBuilder AddRepositories()
        {
            builder.Services.AddSingleton<IAssignedRewardRepository, AssignedRewardRepository>();
            builder.Services.AddSingleton<IRoleRewardRepository, RoleRewardRepository>();
            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscordOptions()
        {
            builder.Services.AddOptions<DiscordConfig>()
                .BindConfiguration("Discord");
            return builder;;
        }
        
        public IHostApplicationBuilder AddServer()
        {
            builder.Services.AddSingleton<IServerProvider, ConfigBasedServerProvider>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscordEventLogging()
        {
            builder.Services.AddSingleton<ILoggingChannelProvider, ConfigBasedLoggingChannelProvider>();
            return builder;
        }
        
        public IHostApplicationBuilder AddDiscordRoleRewardManagement()
        {
            builder.Services.AddSingleton<DiscordRoleRewardService>();
            builder.Services.AddSingleton<DiscordRoleAssignmentService>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscord()
        {
            builder.Services
                .TryAddSingleton<IAsyncTokenStore, ConfigBasedDiscordTokenService>();
                
            builder.Services
                .AddDiscordGateway()
                .AddDiscordCommands(true)
                .AddCommandTree()
                .WithCommandGroup<RewardsCommands>()
                .Finish()
                .AddDiscordCaching();
            return builder;
        }
    }
}