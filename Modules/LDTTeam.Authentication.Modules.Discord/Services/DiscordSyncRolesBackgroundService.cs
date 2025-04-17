using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Modules.Discord.Services;

public class DiscordSyncRolesBackgroundService(
    IServiceProvider serviceProvider
) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            DiscordRoleSyncService syncService = scope.ServiceProvider.GetRequiredService<DiscordRoleSyncService>();

            await syncService.RunSync(cancellationToken);
        }
    }
}