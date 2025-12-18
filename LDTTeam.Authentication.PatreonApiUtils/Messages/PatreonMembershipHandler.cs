using LDTTeam.Authentication.PatreonApiUtils.Service;

namespace LDTTeam.Authentication.PatreonApiUtils.Messages;

public partial class PatreonMembershipHandler(IUserRepository userRepository, IMembershipRepository membershipRepository, ILogger<PatreonMembershipHandler> logger)
{
    public async Task Handle(PatreonMembershipCreatedOrUpdated message)
    {
        LogHandlingPatreonMembershipCreatedOrUpdatedForUserIdUseridAndMembershipIdMembershipid(logger, message.UserId, message.MembershipId);
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
            return;

        user.MembershipId = message.MembershipId;
        await userRepository.CreateOrUpdateAsync(user);
    }
    
    public async Task Handle(PatreonMembershipRemoved message)
    {
        LogHandlingPatreonMembershipDeletedForUserIdUserid(logger, message.UserId);
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
            return;

        if (!user.MembershipId.HasValue)
            return;
        
        await membershipRepository.DeleteAsync(user.MembershipId.Value);

        user.MembershipId = null;
        await userRepository.CreateOrUpdateAsync(user);
    }

    [LoggerMessage(LogLevel.Information, "Handling Patreon membership created or updated for User ID {userId} and Membership ID {membershipId}")]
    static partial void LogHandlingPatreonMembershipCreatedOrUpdatedForUserIdUseridAndMembershipIdMembershipid(ILogger<PatreonMembershipHandler> logger, Guid userId, Guid membershipId);

    [LoggerMessage(LogLevel.Information, "Handling Patreon membership deleted for User ID {userId}")]
    static partial void LogHandlingPatreonMembershipDeletedForUserIdUserid(ILogger<PatreonMembershipHandler> logger, Guid userId);
}