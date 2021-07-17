using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LDTTeam.Authentication.Modules.GitHub.Data
{
    // ReSharper disable once UnusedType.Global
    public class GitHubDatabaseContextFactory: IDesignTimeDbContextFactory<GitHubDatabaseContext>
    {
        public GitHubDatabaseContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<GitHubDatabaseContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;User Id=postgres;Password=password;",
                b => b.MigrationsAssembly("LDTTeam.Authentication.Modules.GitHub"));

            return new GitHubDatabaseContext(optionsBuilder.Options);
        }
    }
}