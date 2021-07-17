using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Modules.Patreon.Data
{
    public class PatreonDatabaseContext : DbContext
    {
        public DbSet<DbToken> Token { get; set; } = null!;
        public DbSet<DbPatreonMember> PatreonMembers { get; set; } = null!;
        
        public PatreonDatabaseContext(DbContextOptions<PatreonDatabaseContext> options) : base(options)
        {
        }
    }
}