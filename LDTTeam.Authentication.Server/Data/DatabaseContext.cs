using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Server.Data
{
    public class DatabaseContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<ConditionInstance> ConditionInstances { get; set; }
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Reward>()
                .HasMany(x => x.Conditions)
                .WithOne(x => x.Reward)
                .HasForeignKey(x => x.RewardId);

            builder.Entity<ConditionInstance>()
                .HasKey(x => new {x.RewardId, x.ModuleName, x.ConditionName});
        }
    }
}
