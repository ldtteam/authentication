using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.RewardsService.Service;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.RewardsService.Handlers;

public partial class UserHandler(
    IUserTiersRepository userTiersRepository,
    IUserLifetimeContributionsRepository userLifetimeContributionsRepository,
    IRewardsCalculationService rewardsCalculationService,
    ILogger<UserHandler> logger)
{
    public async Task Handle(UserTiersAdded message)
    {
        var userId = message.UserId;
        var tiers = message.Tiers.ToList();
        LogHandlingUserTiersAddedForUseridUseridWithTiersTiers(logger, message.UserId, string.Join(", ", tiers));
        await userTiersRepository.AddUserTiersAsync(userId, message.Provider, tiers);
        await rewardsCalculationService.RecalculateRewardsAsync(userId);
    }

    public async Task Handle(UserTiersRemoved message)
    {
        var userId = message.UserId;
        var tiers = message.Tiers.ToList();
        LogHandlingUserTiersRemovedForUseridUseridWithTiersTiers(logger, userId, string.Join(", ", tiers));
        await userTiersRepository.RemoveUserTiersAsync(userId, message.Provider, tiers);
        await rewardsCalculationService.RecalculateRewardsAsync(userId);
    }
    
    public async Task Handle(UserLifetimeContributionIncreased message)
    {
        LogHandlingUserLifetimeContributionIncreased(logger, message.UserId, message.AdditionalContributionAmount);
        var current = await userLifetimeContributionsRepository.GetUserLifetimeContributionAsync(message.UserId);
        var updated = current + message.AdditionalContributionAmount;
        await userLifetimeContributionsRepository.SetUserLifetimeContributionAsync(message.UserId, updated);
        await rewardsCalculationService.RecalculateRewardsAsync(message.UserId);
    }

    public async Task Handle(UserLifetimeContributionUpdated message)
    {
        LogHandlingUserLifetimeContributionUpdated(logger, message.UserId, message.NewLifetimeContributionAmount);
        await userLifetimeContributionsRepository.SetUserLifetimeContributionAsync(message.UserId, message.NewLifetimeContributionAmount);
        await rewardsCalculationService.RecalculateRewardsAsync(message.UserId);
    }

    #region Logging

    [LoggerMessage(LogLevel.Information, "Handling UserTiersAdded for UserId: {userId} with Tiers: {tiers}")]
    static partial void LogHandlingUserTiersAddedForUseridUseridWithTiersTiers(ILogger<UserHandler> logger, Guid userId, string tiers);

    [LoggerMessage(LogLevel.Information, "Handling UserTiersRemoved for UserId: {userId} with Tiers: {tiers}")]
    static partial void LogHandlingUserTiersRemovedForUseridUseridWithTiersTiers(ILogger<UserHandler> logger, Guid userId, string tiers);

    [LoggerMessage(LogLevel.Information, "Handling UserLifetimeContributionIncreased for UserId: {userId} with AdditionalContributionAmount: {amount}")]
    static partial void LogHandlingUserLifetimeContributionIncreased(ILogger<UserHandler> logger, Guid userId, decimal amount);

    [LoggerMessage(LogLevel.Information, "Handling UserLifetimeContributionUpdated for UserId: {userId} with NewLifetimeContributionAmount: {amount}")]
    static partial void LogHandlingUserLifetimeContributionUpdated(ILogger<UserHandler> logger, Guid userId, decimal amount);

    #endregion

}