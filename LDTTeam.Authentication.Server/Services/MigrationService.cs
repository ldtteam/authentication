using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
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
        var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MigrationService>>();
        
        logger.LogWarning("Starting external account synchronization...");
        var logins = await database.UserLogins.ToListAsync();
        int index = 0;
        foreach (var login in logins)
        {
            if (!Enum.TryParse(login.LoginProvider, out AccountProvider provider))
            {
                logger.LogError("Skipping user with invalid LoginProvider: {LoginProvider}", login.LoginProvider);
                continue;
            }
            await messageBus.SendAsync(
                new ExternalLoginConnectedToUser(Guid.Parse(login.UserId), provider, login.ProviderKey)
            );

            index++;
            if (index % 50 == 0)
            {
                logger.LogWarning("Synchronized {Index} / {Total} users...", index, logins.Count);
                await Task.Delay(1000, stoppingToken);
            }
        }
        logger.LogWarning("External account synchronization complete.");
    }
}