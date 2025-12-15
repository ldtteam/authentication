using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.Models.App.Rewards;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Service for managing assigned rewards for users.
/// Uses EF Core as the backing store and should be implemented with an <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
/// </summary>
public interface IAssignedRewardRepository
{
    /// <summary>
    /// Gets all assigned rewards for a user.
    /// </summary>
    Task<IEnumerable<AssignedReward>> GetForUserAsync(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Checks whether a user has the specified reward.
    /// </summary>
    Task<bool> HasRewardAsync(Guid userId, string reward, RewardType type, CancellationToken token = default);

    /// <summary>
    /// Assigns a reward to a user.
    /// If the reward already exists this becomes a no-op.
    /// </summary>
    Task AssignAsync(AssignedReward assignment, CancellationToken token = default);

    /// <summary>
    /// Removes a reward from a user.
    /// </summary>
    Task RemoveAsync(Guid userId, string reward, RewardType type, CancellationToken token = default);
}

