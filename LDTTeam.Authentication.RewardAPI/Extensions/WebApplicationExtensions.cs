using LDTTeam.Authentication.RewardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardAPI.Extensions;

// ReSharper disable once InconsistentNaming
public static class WebApplicationExtensions
{
    extension(WebApplication host)
    {
        public void MigrateDatabase()
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseContext>>();
            logger.LogWarning("Migrating database if necessary...");
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Database.Migrate();
            logger.LogInformation("Database migration complete.");
        }
    }
}