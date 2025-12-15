using LDTTeam.Authentication.RewardsService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.RewardsService.Extensions;

// ReSharper disable once InconsistentNaming
public static class IHostExtensions
{
    
    public static void MigrateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseContext>>();
        logger.LogWarning("Migrating database if necessary...");
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migration complete.");
    }
}