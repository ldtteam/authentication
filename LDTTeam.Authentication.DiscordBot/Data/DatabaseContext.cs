using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<AssignedReward> AssignedRewards { get; set; } = null!;
        public DbSet<RoleRewards> RoleRewards { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {    
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<RewardType>().HaveConversion<string>();
            configurationBuilder.Properties<Snowflake>().HaveConversion<SnowflakeConverter>();
        }
    }
    
    [UsedImplicitly]
    public class SnowflakeConverter()
        : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Snowflake, ulong>(v => v.Value,
            v => new Snowflake(v));
}