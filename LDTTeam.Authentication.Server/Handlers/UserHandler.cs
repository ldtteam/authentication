using System;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Server.Models.Data;
using LDTTeam.Authentication.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Server.Handlers;

public partial class UserHandler(
    IAssignedRewardRepository assignedRewardRepository,
    UserManager<ApplicationUser> userRepository,
    ILogger<UserHandler> logger) {

    public async Task Handle(UserRewardAdded message)
    {
        var user = await userRepository.FindByIdAsync(message.UserId.ToString());
        if (user == null)
        {
            LogReceivedUserrewardaddedForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }

        await assignedRewardRepository.AssignAsync(new AssignedReward()
        {
            UserId = message.UserId.ToString(),
            Reward = message.Reward,
            Type = message.RewardType
        });
        
        LogAssignedRewardRewardOfTypeTypeToUserIdUserid(logger, message.Reward, message.RewardType, message.UserId);
    }
    
    public async Task Handle(UserRewardRemoved message)
    {
        var user = await userRepository.FindByIdAsync(message.UserId.ToString());
        if (user == null)
        {
            LogReceivedUserrewardremovedForNonExistentUserIdUserid(logger, message.UserId);
            return;
        }
        
        await assignedRewardRepository.RemoveAsync(message.UserId.ToString(), message.Reward, message.RewardType);

        LogUnassignedRewardRewardOfTypeTypeToUserIdUserid(logger, message.Reward, message.RewardType, message.UserId);
    }

    [LoggerMessage(LogLevel.Error, "Received UserRewardAdded for non-existent user ID {userId}")]
    static partial void LogReceivedUserrewardaddedForNonExistentUserIdUserid(ILogger<UserHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "Assigned reward {reward} of type {type} to user ID {userId}")]
    static partial void LogAssignedRewardRewardOfTypeTypeToUserIdUserid(ILogger<UserHandler> logger, string reward, RewardType type, Guid userId);

    [LoggerMessage(LogLevel.Error, "Received UserRewardRemoved for non-existent user ID {userId}")]
    static partial void LogReceivedUserrewardremovedForNonExistentUserIdUserid(ILogger<UserHandler> logger, Guid userId);

    [LoggerMessage(LogLevel.Information, "Unassigned reward {reward} of type {type} to user ID {userId}")]
    static partial void LogUnassignedRewardRewardOfTypeTypeToUserIdUserid(ILogger<UserHandler> logger, string reward, RewardType type, Guid userId);
}