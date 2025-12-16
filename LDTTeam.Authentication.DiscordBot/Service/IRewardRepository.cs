using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.DiscordBot.Model.Data;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Repository-style service for working with <see cref="Reward"/> entities.
/// All methods are asynchronous and use the application's <see cref="Data.DatabaseContext"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> to reduce database load and ensure cache consistency.
/// </summary>
public interface IRewardRepository
{
    /// <summary>
    /// Gets a reward by its <see cref="RewardType"/> and name.
    /// </summary>
    /// <param name="type">The reward type.</param>
    /// <param name="name">The reward name.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="Reward"/> or <c>null</c> if not found.</returns>
    Task<Reward?> GetAsync(RewardType type, string name, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a reward in the database.
    /// On success the returned <see cref="Reward"/> will reflect any values stored.
    /// </summary>
    /// <param name="reward">The reward to create or update.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The upserted <see cref="Reward"/>.</returns>
    Task<Reward> UpsertAsync(Reward reward, CancellationToken token = default);

    /// <summary>
    /// Deletes the reward with the specified type and name if it exists.
    /// </summary>
    /// <param name="type">The reward type.</param>
    /// <param name="name">The reward name.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(RewardType type, string name, CancellationToken token = default);

    /// <summary>
    /// Gets all rewards of a given <see cref="RewardType"/>.
    /// </summary>
    /// <param name="type">The reward type.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>All rewards of the specified type.</returns>
    Task<IEnumerable<Reward>> GetAllByTypeAsync(RewardType type, CancellationToken token = default);

    /// <summary>
    /// Gets all rewards in the database.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>All rewards.</returns>
    Task<IEnumerable<Reward>> GetAllAsync(CancellationToken token = default);
}
