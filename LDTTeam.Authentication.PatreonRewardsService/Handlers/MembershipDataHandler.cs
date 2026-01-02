using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using LDTTeam.Authentication.PatreonApiUtils.Service;
using Wolverine;

namespace LDTTeam.Authentication.PatreonRewardsService.Handlers;

public partial class MembershipDataHandler(
    IMembershipRepository membershipRepository,
    IRewardRepository rewardRepository,
    IMessageBus bus,
    IUserRepository userRepository,
    ILogger<MembershipDataHandler> logger
    )
{
    public async Task Handle(MembershipDataRemoved message)
    {
        var reward = await rewardRepository.GetByIdAsync(message.OldMembershipId);
        if (reward != null)
        {
            await rewardRepository.DeleteAsync(message.OldMembershipId);
            
            var removedTiers = reward.Tiers.Select(t => t.Tier).Distinct().ToList();
            var removedLifetimeCents = reward.LifetimeCents;
            
            await bus.PublishAsync(new UserLifetimeContributionIncreased(
                message.UserId,
                AccountProvider.Patreon,
                -removedLifetimeCents
            ));
            
            if (removedTiers.Count != 0)
            {
                await bus.PublishAsync(new UserTiersRemoved(
                    message.UserId,
                    AccountProvider.Patreon,
                    removedTiers
                ));
            }
        }
    }
    
    public async Task Handle(MembershipDataUpdated message)
    {
        var membership = await membershipRepository.GetByIdAsync(message.MembershipId);
        if (membership == null)
        {
            LogMembershipWithIdMembershipidNotFound(logger, message.MembershipId);
            return;
        }

        var reward = await rewardRepository.GetByIdAsync(message.MembershipId);
        
        var addedLifetimeCents = membership.LifetimeCents - (reward?.LifetimeCents ?? 0);
        
        List<string> newTiers;
        List<string> removedTiers;

        if (reward == null)
        {
            //New member has no rewards yet.
            //Check the membership for valid tiers and assign rewards accordingly.
            if ((!membership.LastChargeSuccessful || !membership.Tiers.Any()) && membership is { LifetimeCents: 0, IsGifted: false })
            {
                LogMembershipWithIdMembershipidHasNoSuccessfulChargesOrTiersNoRewardsToAssign(logger,
                    message.MembershipId);
                return;
            }

            newTiers = membership.Tiers.Select(t => t.Tier).Distinct().ToList();
            removedTiers = [];
            
            reward = new Reward
            {
                MembershipId = membership.MembershipId,
                LifetimeCents = membership.LifetimeCents,
                IsGifted = membership.IsGifted,
                LastSyncDate = DateTime.UtcNow,
                Tiers = membership.Tiers.Select(t => new RewardMembership
                {
                    Tier = t.Tier
                }).ToList()
            };
            
            await rewardRepository.CreateOrUpdateAsync(reward);
        }
        else
        {
            newTiers = membership.Tiers.Select(t => t.Tier).Distinct()
                .Except(reward.Tiers.Select(t => t.Tier).Distinct())
                .ToList();
            removedTiers = reward.Tiers.Select(t => t.Tier).Distinct()
                .Except(membership.Tiers.Select(t => t.Tier).Distinct())
                .ToList();
            
            //Membership data updated, sync rewards accordingly.
            reward.LifetimeCents = membership.LifetimeCents;
            reward.IsGifted = membership.IsGifted;
            reward.Tiers = membership.Tiers.Select(t => new RewardMembership
            {
                Tier = t.Tier
            }).ToList();
            
            await rewardRepository.CreateOrUpdateAsync(reward);
        }

        LogUpdatingRewardsForMembershipIdMembershipid(logger, message.MembershipId);

        if (newTiers.Count != 0 || removedTiers.Count != 0 || addedLifetimeCents != 0)
        {
            var user = await userRepository.GetByMembershipIdAsync(message.MembershipId);
            if (user == null)
            {
                LogDataInconsistencyNoUserFoundForMembershipIdMembershipid(logger, message.MembershipId);
                return;
            }
            
            LogRewardsUpdatedForMembershipIdMembershipid(logger, message.MembershipId);
            if (addedLifetimeCents != 0)
                await bus.PublishAsync(new UserLifetimeContributionIncreased(
                    user.UserId,
                    AccountProvider.Patreon,
                    addedLifetimeCents
                ));
            
            if (newTiers.Count != 0)
                await bus.PublishAsync(new UserTiersAdded(
                    user.UserId,
                    AccountProvider.Patreon,
                    newTiers
                ));
            
            if (removedTiers.Count != 0)
                await bus.PublishAsync(new UserTiersRemoved(
                    user.UserId,
                    AccountProvider.Patreon,
                    removedTiers
                ));
        }
    }

    [LoggerMessage(LogLevel.Critical, "Membership with ID {membershipId} not found")]
    static partial void LogMembershipWithIdMembershipidNotFound(ILogger<MembershipDataHandler> logger, Guid membershipId);

    [LoggerMessage(LogLevel.Information, "Membership with ID {membershipId} has no successful charges or tiers, no rewards to assign")]
    static partial void LogMembershipWithIdMembershipidHasNoSuccessfulChargesOrTiersNoRewardsToAssign(ILogger<MembershipDataHandler> logger, Guid membershipId);

    [LoggerMessage(LogLevel.Information, "Updating rewards for Membership ID {membershipId}")]
    static partial void LogUpdatingRewardsForMembershipIdMembershipid(ILogger<MembershipDataHandler> logger, Guid membershipId);

    [LoggerMessage(LogLevel.Information, "Rewards updated for Membership ID {membershipId}")]
    static partial void LogRewardsUpdatedForMembershipIdMembershipid(ILogger<MembershipDataHandler> logger, Guid membershipId);

    [LoggerMessage(LogLevel.Critical, "Data inconsistency: No user found for Membership ID {MembershipId}")]
    static partial void LogDataInconsistencyNoUserFoundForMembershipIdMembershipid(ILogger<MembershipDataHandler> logger, Guid MembershipId);
}