using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.Rewards;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for managing reward calculations in the rewards service.
/// Provides methods to add, remove, and retrieve reward calculations.
/// </summary>
public interface IRewardCalculationsRepository
{
    /// <summary>
    /// Asynchronously adds or updates a reward calculation.
    /// </summary>
    /// <param name="reward">The reward name.</param>
    /// <param name="type">The reward type.</param>
    /// <param name="lambda">The calculation lambda.</param>
    Task AddOrUpdateRewardCalculationAsync(string reward, RewardType type, string lambda);

    /// <summary>
    /// Asynchronously removes a reward calculation.
    /// </summary>
    /// <param name="reward">The reward name.</param>
    /// <param name="type">The reward type.</param>
    Task RemoveRewardCalculationAsync(string reward, RewardType type);

    /// <summary>
    /// Asynchronously retrieves all reward calculations.
    /// </summary>
    /// <returns>A list of all reward calculations.</returns>
    Task<List<(string Reward, RewardType Type, string Lambda)>> GetAllRewardCalculationsAsync();
}

