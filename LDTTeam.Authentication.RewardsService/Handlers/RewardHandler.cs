using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.Rewards;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.RewardsService.Service;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.RewardsService.Handlers;

public partial class RewardHandler(
    IRewardCalculationsRepository rewardCalculationsRepository, 
    IRewardsCalculationService rewardsCalculationService,
    IUserRewardAssignmentsRepository userRewardAssignmentsRepository,
    ILogger<RewardHandler> logger)
{
    public async Task Handle(RewardCreatedOrUpdated message)
    {
        LogHandlingRewardCreatedOrUpdated(logger, message.Reward, message.Type, message.Lambda);
        await rewardCalculationsRepository.AddOrUpdateRewardCalculationAsync(message.Reward, message.Type, message.Lambda);
        rewardsCalculationService.ClearCache();
        await rewardsCalculationService.RecalculateRewardForAllUsers(message.Type, message.Reward);
    }

    public async Task Handle(RewardRemoved message)
    {
        LogHandlingRewardRemoved(logger, message.Reward, message.Type);
        await rewardCalculationsRepository.RemoveRewardCalculationAsync(message.Reward, message.Type);
        rewardsCalculationService.ClearCache();
        await userRewardAssignmentsRepository.RemoveAllAssignmentsForRewardAsync(message.Reward, message.Type);
        await rewardsCalculationService.RecalculateRewardForAllUsers(message.Type, message.Reward);
    }

    #region Logging

    [LoggerMessage(LogLevel.Information, "Handling RewardCreatedOrUpdated for Reward: {reward}, Type: {type}, Lambda: {lambda}")]
    static partial void LogHandlingRewardCreatedOrUpdated(ILogger<RewardHandler> logger, string reward, RewardType type, string lambda);

    [LoggerMessage(LogLevel.Information, "Handling RewardRemoved for Reward: {reward}, Type: {type}")]
    static partial void LogHandlingRewardRemoved(ILogger<RewardHandler> logger, string reward, RewardType type);

    #endregion
}

