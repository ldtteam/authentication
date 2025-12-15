using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LDTTeam.Authentication.RewardsService.Data
{
    public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
    {
        public DatabaseContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<DatabaseContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;User Id=postgres;Password=postgres;",
                b => b.MigrationsAssembly("LDTTeam.Authentication.RewardsService"));

            return new DatabaseContext(optionsBuilder.Options);
        }
    }
}