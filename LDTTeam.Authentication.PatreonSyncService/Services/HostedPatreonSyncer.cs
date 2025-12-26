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
        
        logger.LogInformation("Legacy Contribution migration starting...");
        var legacyContributions = context.LegacyContributionInformations.ToList();
        foreach (var contribution in legacyContributions)
        {
            var user = await userRepository.GetByPatreonIdAsync(contribution.Id.ToString(), stoppingToken);
            if (user == null)
            {
                logger.LogWarning("No user found for Legacy Contribution Patreon ID: {PatreonId}", contribution.Id);
                continue;
            }

            if (!user.MembershipId.HasValue)
            {
                logger.LogWarning("User ID: {UserId} has no Membership ID, skipping Legacy Contribution migration for Patreon ID: {PatreonId}", user.UserId, contribution.Id);
                continue;
            }
            
            logger.LogInformation("Migrating Legacy Contribution for User ID: {UserId}, Patreon ID: {PatreonId}, Amount: {Amount}", user.UserId, contribution.Id, contribution.Lifetime);
            var membership = await membershipRepository.GetByIdAsync(user.MembershipId.Value, stoppingToken);
            if (membership == null)
            {
                logger.LogWarning("No membership found for Membership ID: {MembershipId}, skipping Legacy Contribution migration for User ID: {UserId}", user.MembershipId, user.UserId);
                continue;
            }

            if (membership.LifetimeCents < contribution.Lifetime)
            {
                logger.LogWarning("Membership ID: {MembershipId} has lower lifetime cents ({MembershipLifetime}) than Legacy Contribution ({LegacyLifetime}), skipping migration for User ID: {UserId}", membership.MembershipId, membership.LifetimeCents, contribution.Lifetime, user.UserId);
                membership.LifetimeCents = contribution.Lifetime;
                await membershipRepository.CreateOrUpdateAsync(membership, stoppingToken);

                await bus.PublishAsync(new MembershipDataUpdated(membership.MembershipId));
            }
        }

        logger.LogInformation("Patreon membership synchronization complete. Stopping application...");
        lifecycleService.StopApplication();
    }
}