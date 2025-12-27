using LDTTeam.Authentication.DiscordBot.AutoCompletion;
using LDTTeam.Authentication.DiscordBot.Commands;
using LDTTeam.Authentication.DiscordBot.Config;
using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Responders;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Remora.Commands.Extensions;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Options;
using Remora.Discord.Hosting.Services;
using Remora.Discord.Rest;
using static Remora.Extensions.Options.Immutable.ImmutableOptionServiceCollectionExtensions;

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
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IAssignedRewardRepository, AssignedRewardRepository>();
            builder.Services.AddScoped<IRoleRewardRepository, RoleRewardRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRewardRepository, RewardRepository>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscordOptions()
        {
            builder.Services.AddOptions<DiscordConfig>()
                .BindConfiguration("Discord");
            return builder;
        }

        public IHostApplicationBuilder AddServer()
        {
            builder.Services.AddSingleton<IServerProvider, ConfigBasedServerProvider>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscordEventLogging()
        {
            builder.Services.AddSingleton<ILoggingChannelProvider, ConfigBasedLoggingChannelProvider>();
            builder.Services.AddSingleton<DiscordEventLoggingService>();
            builder.Services.AddHostedService<DiscordRetryFailedLogService>();
            builder.Services.AddSingleton<IDiscordFailedLogQueueService, DiscordFailedLogQueueService>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscordRoleRewardManagement()
        {
            builder.Services.AddScoped<DiscordRoleRewardService>();
            builder.Services.AddScoped<DiscordRoleAssignmentService>();
            builder.Services.AddResponder<AssignRolesOnJoinResponder>();
            return builder;
        }

        public IHostApplicationBuilder AddDiscord()
        {
            builder.Services
                .AddSingleton<IAsyncTokenStore, ConfigBasedDiscordTokenService>();

            builder.Services
                .AddDiscordGateway()
                .AddSingleton<DiscordService>()
                .AddDiscordCommands(true)
                .AddCommandTree()
                .WithCommandGroup<RewardsCommands>()
                .WithCommandGroup<TierCommands>()
                .WithCommandGroup<ContextCommands>()
                .Finish()
                .AddDiscordCaching()
                .AddAutocompleteProvider<RewardsAutoCompleteProvider>()
                .AddAutocompleteProvider<DiscordRoleRewardsAutoCompleteProvider>();
            
            builder.Services.Configure(() => new DiscordServiceOptions());
            
            builder.Services
                .AddSingleton<IHostedService, DiscordService>(serviceProvider =>
                    serviceProvider.GetRequiredService<DiscordService>());
            
            return builder;
        }
    }
}