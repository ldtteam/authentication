using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Service;

namespace LDTTeam.Authentication.PatreonRewardsService.Handlers;

public partial class PatreonMembershipHandler(
    IUserRepository userRepository,
    IMembershipRepository membershipRepository,
    IPatreonMembershipService membershipService,
    ILogger<PatreonMembershipHandler> logger)
{
    public async Task Handle(PatreonMembershipCreatedOrUpdated message)
    {
        LogHandlingPatreonMembershipCreatedOrUpdatedForUserIdUseridAndMembershipIdMembershipid(logger, message.UserId, message.MembershipId);

        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogUserIdUseridNotFoundWhileHandlingPatreonMembershipCreationOrUpdate(logger, message.UserId);
            return;
        }
        
        var membership = await membershipRepository.GetByIdAsync(message.MembershipId);
        if (membership != null)
        {
            if (!user.MembershipId.HasValue)
            {
                var userByMembership = await userRepository.GetByMembershipIdAsync(message.MembershipId);
                if (userByMembership != null)
                {
                    LogMembershipIdMembershipidIsAlreadyAssociatedWithAnotherUserIdOtheruserid(logger, message.MembershipId, userByMembership.UserId);
                    return;
                }

                user.MembershipId = message.MembershipId;
                await userRepository.CreateOrUpdateAsync(user);
                LogAssociatedExistingMembershipIdMembershipidWithUserIdUserid(logger, message.MembershipId, user.UserId);

                //On reattach we need to ensure old data is cleared
                await membershipService.ForceRemoveMembershipOf(message.UserId, message.MembershipId);
                LogForcedRemovalOfOldMembershipDataForUserUseridAndMembershipMembershipidOn(logger, message.UserId, message.MembershipId);
            }
        }

        await membershipService.UpdateStatusForMember(message.MembershipId);
    }
    
    public async Task Handle(PatreonMembershipRemoved message)
    {
        LogHandlingPatreonMembershipDeletedForUserIdUserid(logger, message.UserId);
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogUserIdUseridNotFoundWhileHandlingPatreonMembershipRemoval(logger, message.UserId);
            return;
        }

        if (!user.MembershipId.HasValue)
        {
            LogUserIdUseridHasNoMembershipIdWhileHandlingPatreonMembershipRemoval(logger, message.UserId);
            return;
        }
        
        var oldMembershipId = user.MembershipId.Value;
        await membershipRepository.DeleteAsync(user.MembershipId.Value);
        
        LogRemovedMembershipMembershipidForUserUserid(logger, oldMembershipId, user.UserId);

        user.MembershipId = null;
        await userRepository.CreateOrUpdateAsync(user);
        LogClearedMembershipIdForUserUserid(logger, user.UserId);
        
        await membershipService.ForceRemoveMembershipOf(user.UserId, oldMembershipId);
        LogForcedRemovalOfMembershipDataForUserUseridAndMembershipMembershipid(logger, user.UserId, oldMembershipId);
    }

    [LoggerMessage(LogLevel.Information, "Handling Patreon membership created or updated for User ID {userId} and Membership ID {membershipId}")]
    static partial void LogHandlingPatreonMembershipCreatedOrUpdatedForUserIdUseridAndMembershipIdMembershipid(ILogger<PatreonMembershipHandler> logger, Guid userId, Guid membershipId);

    [LoggerMessage(LogLevel.Information, "Handling Patreon membership deleted for User ID {userId}")]
    static partial void LogHandlingPatreonMembershipDeletedForUserIdUserid(ILogger<PatreonMembershipHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Error, "User ID {userId} not found while handling Patreon membership removal")]
    static partial void LogUserIdUseridNotFoundWhileHandlingPatreonMembershipRemoval(ILogger<PatreonMembershipHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "User ID {userId} has no Membership ID while handling Patreon membership removal")]
    static partial void LogUserIdUseridHasNoMembershipIdWhileHandlingPatreonMembershipRemoval(ILogger<PatreonMembershipHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "Removed membership {membershipId} for User {userId}")]
    static partial void LogRemovedMembershipMembershipidForUserUserid(ILogger<PatreonMembershipHandler> logger, Guid membershipId, Guid userId);

    [LoggerMessage(LogLevel.Information, "Cleared Membership ID for User {userId}")]
    static partial void LogClearedMembershipIdForUserUserid(ILogger<PatreonMembershipHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "Forced removal of membership data for User {userId} and Membership {membershipId}")]
    static partial void LogForcedRemovalOfMembershipDataForUserUseridAndMembershipMembershipid(ILogger<PatreonMembershipHandler> logger, Guid userId, Guid membershipId);

    [LoggerMessage(LogLevel.Error, "User ID {userId} not found while handling Patreon membership creation or update")]
    static partial void LogUserIdUseridNotFoundWhileHandlingPatreonMembershipCreationOrUpdate(ILogger<PatreonMembershipHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Warning, "Membership ID {membershipId} is already associated with another user ID {otherUserId}")]
    static partial void LogMembershipIdMembershipidIsAlreadyAssociatedWithAnotherUserIdOtheruserid(ILogger<PatreonMembershipHandler> logger, Guid membershipId, Guid otherUserId);

    [LoggerMessage(LogLevel.Information, "Associated existing Membership ID {membershipId} with User ID {userId}")]
    static partial void LogAssociatedExistingMembershipIdMembershipidWithUserIdUserid(ILogger<PatreonMembershipHandler> logger, Guid membershipId, Guid userId);

    [LoggerMessage(LogLevel.Warning, "Forced removal of old membership data for User {userId} and Membership {membershipId} on reattachment")]
    static partial void LogForcedRemovalOfOldMembershipDataForUserUseridAndMembershipMembershipidOn(ILogger<PatreonMembershipHandler> logger, Guid userId, Guid membershipId);
}