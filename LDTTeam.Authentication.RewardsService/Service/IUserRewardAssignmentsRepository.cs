using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.Rewards;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for managing user reward assignments in the rewards service.
/// Provides methods to add, remove, and retrieve rewards associated with a user.
/// Implementations are expected to be asynchronous and should consider local caching for performance.
/// </summary>
public interface IUserRewardAssignmentsRepository
{
    /// <summary>
    /// Asynchronously adds the specified rewards to the user with the given userId.
    /// Duplicate assignments are ignored.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="rewards">A list of (RewardType, Reward) tuples to add to the user.</param>
    Task AddUserRewardsAsync(Guid userId, List<(RewardType Type, string Reward)> rewards);

    /// <summary>
    /// Asynchronously removes the specified rewards from the user with the given userId.
    /// Only the listed (type, reward) pairs will be removed; other assignments for the user remain untouched.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="rewards">A list of (RewardType, Reward) tuples to remove from the user.</param>
    Task RemoveUserRewardsAsync(Guid userId, List<(RewardType Type, string Reward)> rewards);

    /// <summary>
    /// Asynchronously retrieves the list of rewards associated with the user with the given userId.
    /// The returned list contains tuples of (RewardType, Reward) representing each assignment.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of (RewardType, Reward) tuples associated with the user.</returns>
    Task<List<(RewardType Type, string Reward)>> GetUserRewardsAsync(Guid userId);

    /// <summary>
    /// Removes all assignments for the specified reward across all users.
    /// This is useful when a reward is deleted or renamed and all existing assignments should be cleared.
    /// </summary>
    /// <param name="messageReward">The reward identifier to remove from all users.</param>
    /// <param name="messageType">The reward type associated with the reward to remove.</param>
    Task RemoveAllAssignmentsForRewardAsync(string messageReward, RewardType messageType);
}
