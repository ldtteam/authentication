using LDTTeam.Authentication.Modules.GitHub.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Modules.GitHub.Data
{
    public class GitHubDatabaseContext : DbContext
    {
        public DbSet<DbGitHubTeam> Teams { get; set; } = null!;
        public DbSet<DbGitHubUser> Users { get; set; } = null!;

        public GitHubDatabaseContext(DbContextOptions<GitHubDatabaseContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DbGitHubTeam>()
                .HasMany(x => x.UserRelationships)
                .WithOne(x => x.Team!)
                .HasForeignKey(x => x.TeamId);

            builder.Entity<DbGitHubUser>()
                .HasMany(x => x.TeamRelationships)
                .WithOne(x => x.User!)
                .HasForeignKey(x => x.UserId);

            builder.Entity<DbGithubTeamUser>()
                .HasKey(x => new {x.TeamId, x.UserId});
        }
    }
}