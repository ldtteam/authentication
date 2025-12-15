using LDTTeam.Authentication.Models.App.Rewards;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for calculating and updating user rewards.
/// </summary>
public interface IRewardsCalculationService
{
    /// <summary>
    /// Asynchronously recalculates the rewards for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose rewards should be recalculated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RecalculateRewardsAsync(Guid userId);
    
    /// <summary>
    /// Recalculates the specified reward for all users.
    /// </summary>
    /// <param name="type">The type of the reward.</param>
    /// <param name="reward">The name of the reward</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RecalculateRewardForAllUsers(RewardType type, string reward);
    
    /// <summary>
    /// Reset the cached reward calculation lambdas.
    /// </summary>
    public void ClearCache();
}