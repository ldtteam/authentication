using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace LDTTeam.Authentication.Server.Services;

public class MigrationService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (true)
            return;
        
        var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MigrationService>>();
        
        logger.LogWarning("Starting user synchronization...");
        var users = await database.Users.OrderBy(u => u.UserName).ToListAsync(cancellationToken: stoppingToken);
        int index = 0;
        foreach (var applicationUser in users)
        {
            if (applicationUser.UserName!.EqualsIgnoreCase("AnnetteTodd"))
            {
                logger.LogWarning("Found AnnetteTodd user, processing...");
            }
            
            await messageBus.SendAsync(
                new NewUserCreatedOrUpdated(Guid.Parse(applicationUser.Id), applicationUser.UserName ?? throw new InvalidDataException("No username set"))
            );

            index++;
            if (index % 50 == 0)
            {
                logger.LogWarning("Synchronized {Index} / {Total} users...", index, users.Count);
                await Task.Delay(1000, stoppingToken); // Small delay to avoid overwhelming the message bus
            }
        }
        logger.LogWarning("User synchronization complete.");
    }
}