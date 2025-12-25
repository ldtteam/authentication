using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using LDTTeam.Authentication.PatreonApiUtils.Service;

namespace LDTTeam.Authentication.PatreonApiUtils.Handlers;

public partial class UserHandler(
    IUserRepository userRepository,
    ILogger<UserHandler> logger) {

    public async Task Handle(NewUserCreatedOrUpdated message)
    {
        var user = await userRepository.GetByIdAsync(message.Id);
        if (user == null)
        {
            user = new User()
            {
                UserId = message.Id,
                Username = message.UserName,
                PatreonId = null,
                MembershipId = null
            };
        }
        else
        {
            user.Username = message.UserName;
        }
        
        await userRepository.CreateOrUpdateAsync(user);
        LogCreatedNewUserRecordForUserIdUseridWithUsernameUsername(logger, message.Id, message.UserName);
    }
    
    public async Task Handle(UserDeleted message)
    {
        var user = await userRepository.GetByIdAsync(message.Id);
        if (user == null)
            return;

        await userRepository.DeleteAsync(message.Id);
        LogDeletedUserRecordForUserIdUserid(logger, message.Id);
    }
    
    public async Task Handle(ExternalLoginConnectedToUser message)
    {
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogReceivedExternalloginconnectedtouserForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        if (message.Provider != AccountProvider.Patreon)
            return;
        
        if (user.PatreonId != null && user.PatreonId != message.ProviderKey)
        {
            LogReceivedExternalloginconnectedtouserForUserUseridUsernameWhichWouldAlterTheSnowflake(logger, user.UserId, user.Username, user.PatreonId, message.ProviderKey);
            return;
        }
        
        user.PatreonId = message.ProviderKey;
        await userRepository.CreateOrUpdateAsync(user);
        LogLinkedPatreonAccountPatreonidToUserIdUseridUsername(logger, message.ProviderKey, user.UserId, user.Username);
    }
    
    [LoggerMessage(LogLevel.Error, "Received ExternalLoginConnectedToUser for non-existent user ID {userId}")]
    static partial void LogReceivedExternalloginconnectedtouserForNonExistentUserIdUserid(ILogger<UserHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Error, "Received ExternalLoginConnectedToUser for user: {userId}/{userName}, which would alter the Snowflake from: {oldId} to {newId}")]
    static partial void LogReceivedExternalloginconnectedtouserForUserUseridUsernameWhichWouldAlterTheSnowflake(ILogger<UserHandler> logger, Guid userId, string userName, string oldId, string newId);

    [LoggerMessage(LogLevel.Information, "Created new user record for user ID {userId} with username {userName}")]
    static partial void LogCreatedNewUserRecordForUserIdUseridWithUsernameUsername(ILogger<UserHandler> logger, Guid userId, string userName);

    [LoggerMessage(LogLevel.Information, "Linked Patreon account {patreonId} to user ID {userId}/{userName}")]
    static partial void LogLinkedPatreonAccountPatreonidToUserIdUseridUsername(ILogger<UserHandler> logger, string patreonId, Guid userId, string userName);

    [LoggerMessage(LogLevel.Warning, "Deleted user record for user ID {userId}")]
    static partial void LogDeletedUserRecordForUserIdUserid(ILogger<UserHandler> logger, Guid userId);
}