using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.RewardsService.Model.Data;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<UserLifetimeContributions> LifeTimeContributions { get; set; } = null!;
        public DbSet<UserTierAssignment> TierAssignments { get; set; } = null!;
        public DbSet<UserRewardAssignment> RewardAssignments { get; set; } = null!;
        public DbSet<RewardCalculation> RewardCalculations { get; set; } = null!;
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {    
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<RewardType>().HaveConversion<string>();
        }
    }
}