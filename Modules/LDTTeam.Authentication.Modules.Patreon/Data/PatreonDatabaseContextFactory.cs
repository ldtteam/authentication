using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LDTTeam.Authentication.Modules.Patreon.Data
{
    public class PatreonDatabaseContextFactory : IDesignTimeDbContextFactory<PatreonDatabaseContext>
    {
        public PatreonDatabaseContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<PatreonDatabaseContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;User Id=postgres;Password=password;",
                b => b.MigrationsAssembly("LDTTeam.Authentication.Modules.Patreon"));

            return new PatreonDatabaseContext(optionsBuilder.Options);
        }
    }
}