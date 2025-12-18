using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.PatreonApiUtils.Data;

public class DatabaseContext : DbContext
{
    public DbSet<PatreonTokenInformation> TokenInformation { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Membership> Memberships { get; set; } = null!;
    public DbSet<TierMembership> TierMemberships { get; set; } = null!;
    public DbSet<Reward> Rewards { get; set; } = null!;
    public DbSet<RewardMembership> RewardMemberships { get; set; } = null!;
        
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSeeding((context, _) =>
        {
            context.Set<PatreonTokenInformation>().Add(new PatreonTokenInformation
            {
                Id = 0,
                State = State.Invalid,
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.MinValue + TimeSpan.FromMinutes(31) // So it refreshes on first use (one more minute to be safe)
            });
        });
    }
}