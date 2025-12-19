using JetBrains.Annotations;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.RewardAPI.Model.Data;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.RewardAPI.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<AssignedReward> AssignedRewards { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ProviderLogin> Logins { get; set; } = null!;
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {    
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<RewardType>().HaveConversion<string>();
            configurationBuilder.Properties<AccountProvider>().HaveConversion<string>();
        }
    }
}