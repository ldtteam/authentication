using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.RewardAPI.Model.Data;
using LDTTeam.Authentication.RewardAPI.Service;
using User = LDTTeam.Authentication.RewardAPI.Model.Data.User;

namespace LDTTeam.Authentication.RewardAPI.Handlers;

public partial class UserHandler(
    IUserRepository userRepository,
    IProviderLoginRepository providerLoginRepository,
    IAssignedRewardRepository assignedRewardRepository,
    ILogger<UserHandler> logger)
{

    public async Task Handle(NewUserCreatedOrUpdated message)
    {
        var user = await userRepository.GetByIdAsync(message.Id);
        if (user == null)
        {
            user = new User
            {
                UserId = message.Id,
                Username = message.UserName
            };
        }
        else
        {
            user.Username = message.UserName;
        }

        await userRepository.UpsertAsync(user);
    }

    public async Task Handle(ExternalLoginConnectedToUser message)
    {
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogReceivedExternalloginconnectedtouserForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        var providerLogin =
            await providerLoginRepository.GetByProviderAndProviderUserIdAsync(message.Provider, message.ProviderKey);
        if (providerLogin == null)
        {
            providerLogin = new ProviderLogin
            {
                UserId = user.UserId,
                Provider = message.Provider,
                ProviderUserId = message.ProviderKey,
                User = user
            };

            await providerLoginRepository.UpsertAsync(providerLogin);
            return;
        }

        if (providerLogin.ProviderUserId != message.ProviderKey)
        {
            logger.LogWarning(
                "Received ExternalLoginConnectedToUser with already registered key: {key}, for user: {userId}/{userName}",
                message.ProviderKey, user.UserId, user.Username);
        }
    }

    public async Task Handle(ExternalLoginDisconnectedFromUser message)
    {
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogReceivedExternallogindisconnectedfromuserForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        var providerLogin =
            await providerLoginRepository.GetByProviderAndProviderUserIdAsync(message.Provider, message.ProviderKey);
        if (providerLogin == null)
        {
            logger.LogWarning(
                "Received ExternalLoginDisconnectedFromUser for user: {userId}/{userName}, which has no linked login for provider {provider} with key {key}",
                user.UserId, user.Username, message.Provider, message.ProviderKey);
            return;
        }

        await providerLoginRepository.DeleteAsync(providerLogin);
    }

    public async Task Handle(UserRewardAdded message)
    {
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogReceivedUserrewardaddedForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        var existingReward =
            await assignedRewardRepository.GetByKeyAsync(message.UserId, message.Reward, message.RewardType);
        if (existingReward != null)
        {
            return;
        }

        var assignedReward = new AssignedReward
        {
            UserId = message.UserId,
            Reward = message.Reward,
            Type = message.RewardType,
            User = user
        };

        await assignedRewardRepository.UpsertAsync(assignedReward);
    }

    public async Task Handle(UserRewardRemoved message)
    {
        var user = await userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            LogReceivedUserrewardremovedForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        var existingReward =
            await assignedRewardRepository.GetByKeyAsync(message.UserId, message.Reward, message.RewardType);
        if (existingReward == null)
        {
            logger.LogWarning(
                "Received UserRewardRemoved for user ID {userId} which does not have reward {reward} of type {type}",
                message.UserId, message.Reward, message.RewardType);
            return;
        }

        await assignedRewardRepository.DeleteAsync(existingReward);
    }

    [LoggerMessage(LogLevel.Error, "Received ExternalLoginConnectedToUser for non-existent user ID {userId}")]
    static partial void LogReceivedExternalloginconnectedtouserForNonExistentUserIdUserid(ILogger<UserHandler> logger,
        Guid userId);

    [LoggerMessage(LogLevel.Error, "Received UserRewardAdded for non-existent user ID {userId}")]
    static partial void LogReceivedUserrewardaddedForNonExistentUserIdUserid(ILogger<UserHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Error, "Received UserRewardRemoved for non-existent user ID {userId}")]
    static partial void
        LogReceivedUserrewardremovedForNonExistentUserIdUserid(ILogger<UserHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Error, "Received ExternalLoginDisconnectedFromUser for non-existent user ID {userId}")]
    static partial void LogReceivedExternallogindisconnectedfromuserForNonExistentUserIdUserid(
        ILogger<UserHandler> logger, Guid userId);
}