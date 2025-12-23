using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Server.Models.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Server.Data
{
    public class DatabaseContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<AssignedReward> AssignedRewards { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<AccountProvider>().HaveConversion<string>();
            configurationBuilder.Properties<RewardType>().HaveConversion<string>();
        }
    }
}
