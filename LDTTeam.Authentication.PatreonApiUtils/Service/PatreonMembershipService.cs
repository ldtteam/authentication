using ImTools;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Model.App;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.Extensions.Options;
using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

public interface IPatreonMembershipService
{
    Task UpdateStatusFor(Guid userId);

    Task UpdateStatusForMember(Guid membershipId);

    Task UpdateAllStatuses();
}

public class PatreonMembershipService(
    IUserRepository userRepository,
    IPatreonDataService patreonDataService,
    IMembershipRepository membershipRepository,
    IMessageBus bus,
    IOptions<PatreonConfig> config,
    ILogger<PatreonMembershipService> logger) : IPatreonMembershipService
{
    public async Task UpdateStatusFor(Guid userId)
    {
        logger.LogInformation("Updating Patreon membership status for User ID {UserId}", userId);
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null || !user.MembershipId.HasValue)
        {
            logger.LogWarning("User ID {UserId} not found or has no Membership ID", userId);
            return;
        }

        await UpdateStatusForMember(user.MembershipId!.Value, user);
    }

    public Task UpdateStatusForMember(Guid membershipId)
    {
        return UpdateStatusForMember(membershipId, null);
    }

    private async Task UpdateStatusForMember(Guid membershipId, User? user)
    {
        logger.LogInformation("Updating Patreon membership status for Membership ID {MembershipId}", membershipId);
        var patreonInformation = await patreonDataService.GetFor(membershipId);

        if (patreonInformation == null)
        {
            logger.LogWarning("No Patreon information found for Membership ID {MembershipId}", membershipId);
            return;
        }

        var membershipUserData = await CreateOrUpdateMembership(membershipId, patreonInformation.Value, user);
        if (!membershipUserData.HasValue)
        {
            logger.LogWarning("Could not create or update membership for Membership ID {MembershipId}", membershipId);
            return;
        }
        
        var membership = membershipUserData.Value.Membership;
        user ??= membershipUserData.Value.User;

        await membershipRepository.CreateOrUpdateAsync(membership);

        if (user.MembershipId == null)
        {
            user.MembershipId = membershipId;
            await userRepository.CreateOrUpdateAsync(user);
        }
        
        await bus.PublishAsync(new MembershipDataUpdated(membership.MembershipId));
    }
    
    private async Task<(Membership Membership, User User)?> CreateOrUpdateMembership(Guid membershipId, PatreonContribution patreonInformation,
        User? user = null)
    {
        user ??= await userRepository.GetByMembershipIdAsync(membershipId);
        if (user == null)
        {
            if (patreonInformation.PatreonId != null)
                user ??= await userRepository.GetByPatreonIdAsync(patreonInformation.PatreonId);
            
            if (user == null)
            {
                logger.LogWarning(
                    "No user found for Membership ID {MembershipId} when creating or updating membership",
                    membershipId);
                return null;
            }
        }

        if (user.MembershipId != null && user.MembershipId != membershipId)
        {
            logger.LogCritical(
                "Data inconsistency: User ID {UserId} has Membership ID {UserMembershipId} but Patreon data returned for Membership ID {MembershipId}",
                user.UserId, user.MembershipId, membershipId);
            return null;
        }

        var membership = await membershipRepository.GetByIdAsync(membershipId);
        if (membership == null)
        {
            membership = new Membership
            {
                MembershipId = membershipId,
                LifetimeCents = patreonInformation.LifetimeCents,
                IsGifted = patreonInformation.IsGifted,
                LastChargeDate = patreonInformation.LastChargeDate?.ToUniversalTime(),
                LastChargeSuccessful = patreonInformation.LastChargeSuccessful,
                Tiers = BuildTiers(patreonInformation)
            };
        }
        else
        {
            membership.LifetimeCents = patreonInformation.LifetimeCents;
            membership.IsGifted = patreonInformation.IsGifted;
            membership.LastChargeDate = patreonInformation.LastChargeDate?.ToUniversalTime();
            membership.LastChargeSuccessful = patreonInformation.LastChargeSuccessful;
            membership.Tiers = BuildTiers(patreonInformation);
        }

        return (membership, user);
    }

    private List<TierMembership> BuildTiers(PatreonContribution patreonInformation)
    {
        var tiers = config.Value.Tiers;
        var tierNames = patreonInformation.Tiers
            .OrderBy(t => tiers.IndexOf(t))
            .ToList();
        
        if (tierNames.Count == 0)
            return [];
        
        var initialTier = tierNames[0];
        var initialIndex = tiers.IndexOf(initialTier);

        if (initialIndex != 0)
        {
            //We need all tiers up but not including the highest tier
            var allTiers = tiers.Take(initialIndex + 1).ToList();
            tierNames = allTiers.Union(tierNames)
                .OrderBy(t => tiers.IndexOf(t))
                .ToList();
        }
        
        return tierNames
            .Select(tier => new TierMembership() 
        {
            Tier = tier
        }).ToList();
    }

    public async Task UpdateAllStatuses()
    {
        await foreach (var patreonInformation in patreonDataService.All())
        {
            var membershipUserData = await CreateOrUpdateMembership(
                patreonInformation.MembershipId, patreonInformation);
            
            if (!membershipUserData.HasValue)
                continue;

            var membership = membershipUserData.Value.Membership;
            var user = membershipUserData.Value.User;

            await membershipRepository.CreateOrUpdateAsync(membership);

            if (user.MembershipId == null)
            {
                user.MembershipId = membership.MembershipId;
                await userRepository.CreateOrUpdateAsync(user);
            }
            
            await bus.PublishAsync(new MembershipDataUpdated(membership.MembershipId));
        }
    }
}