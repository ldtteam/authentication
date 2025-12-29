using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Service;
using Wolverine;

namespace LDTTeam.Authentication.PatreonSyncService.Services;

public class HostedPatreonSyncer(
    IPatreonMembershipService membershipService,
    IUserRepository userRepository,
    IMembershipRepository membershipRepository,
    DatabaseContext context,
    IHostApplicationLifetime lifecycleService,
    IMessageBus bus,
    ILogger<HostedPatreonSyncer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Patreon membership synchronization...");
        await membershipService.UpdateAllStatuses();
        
        logger.LogInformation("Patreon membership synchronization complete. Stopping application...");
        lifecycleService.StopApplication();
    }
}