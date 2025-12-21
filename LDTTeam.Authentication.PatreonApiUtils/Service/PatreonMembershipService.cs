using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Model.App;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
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

        var membership = await CreateOrUpdateMembership(membershipId, patreonInformation.Value, user);
        if (membership == null)
        {
            logger.LogWarning("Could not create or update membership for Membership ID {MembershipId}", membershipId);
            return;
        }

        await membershipRepository.CreateOrUpdateAsync(membership);
        await bus.PublishAsync(new MembershipDataUpdated(membership.MembershipId));
    }

    private async Task<Membership?> CreateOrUpdateMembership(Guid membershipId, PatreonContribution patreonInformation,
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

            if (!user.MembershipId.HasValue)
            {
                logger.LogInformation("Assigning Membership ID {MembershipId} to User ID {UserId}",
                    membershipId, user.UserId);
                user.MembershipId = membershipId;
                
                await userRepository.CreateOrUpdateAsync(user);
            }
        }

        if (user.MembershipId != membershipId)
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
                LastChargeDate = patreonInformation.LastChargeDate,
                LastChargeSuccessful = patreonInformation.LastChargeSuccessful,
                User = user,
                Tiers = patreonInformation.Tiers.Select(tier => new TierMembership()
                {
                    Tier = tier
                })
            };
        }
        else
        {
            membership.LifetimeCents = patreonInformation.LifetimeCents;
            membership.IsGifted = patreonInformation.IsGifted;
            membership.LastChargeDate = patreonInformation.LastChargeDate;
            membership.LastChargeSuccessful = patreonInformation.LastChargeSuccessful;
            membership.Tiers = patreonInformation.Tiers.Select(tier => new TierMembership()
            {
                Tier = tier
            });
        }

        return membership;
    }

    public async Task UpdateAllStatuses()
    {
        await foreach (var patreonInformation in patreonDataService.All())
        {
            var membership = await CreateOrUpdateMembership(
                patreonInformation.MembershipId, patreonInformation);
            
            if (membership == null)
                continue;

            await membershipRepository.CreateOrUpdateAsync(membership);
            await bus.PublishAsync(new MembershipDataUpdated(membership.MembershipId));
        }
    }
}