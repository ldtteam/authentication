using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Server.Data
{
    public class DatabaseContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<EndpointMetric> Metrics { get; set; }
        public DbSet<HistoricalEndpointMetric> HistoricalMetrics { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<ConditionInstance> ConditionInstances { get; set; }
        
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<HistoricalEndpointMetric>()
                .HasOne(x => x.Metric)
                .WithMany(x => x.HistoricalMetrics);

            builder.Entity<Reward>()
                .HasMany(x => x.Conditions)
                .WithOne(x => x.Reward)
                .HasForeignKey(x => x.RewardId);

            builder.Entity<ConditionInstance>()
                .HasKey(x => new {x.RewardId, x.ModuleName, x.ConditionName});

            builder.Entity<IdentityUserLogin<string>>()
                .HasIndex(x => x.LoginProvider);
        }
    }
}
