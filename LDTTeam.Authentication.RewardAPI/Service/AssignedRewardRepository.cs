using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.RewardAPI.Data;
using LDTTeam.Authentication.RewardAPI.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardAPI.Service;

/// <summary>
/// Repository interface for managing <see cref="AssignedReward"/> entities, supporting async CRUD operations and caching.
/// </summary>
public interface IAssignedRewardRepository
{
    /// <summary>
    /// Retrieves an assigned reward by its composite key (user, reward, type), using cache if available.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="reward">The reward identifier.</param>
    /// <param name="type">The type of the reward.</param>
    /// <returns>The assigned reward if found, otherwise null.</returns>
    Task<AssignedReward?> GetByKeyAsync(Guid userId, string reward, RewardType type);

    /// <summary>
    /// Retrieves all assigned rewards for a specific user, using cache if available.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A collection of assigned rewards for the user.</returns>
    Task<IEnumerable<AssignedReward>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Adds or updates an assigned reward in the database (upsert) and clears the relevant cache entries.
    /// </summary>
    /// <param name="assignedReward">The assigned reward entity to add or update.</param>
    Task UpsertAsync(AssignedReward assignedReward);

    /// <summary>
    /// Deletes an assigned reward from the database and clears the relevant cache entries.
    /// </summary>
    /// <param name="assignedReward">The assigned reward entity to delete.</param>
    Task DeleteAsync(AssignedReward assignedReward);

    /// <summary>
    /// Clears the cache entry for a specific assigned reward.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="reward">The reward identifier.</param>
    /// <param name="type">The type of the reward.</param>
    Task ClearCacheFor(Guid userId, string reward, RewardType type);

    /// <summary>
    /// Clears the cache entries for all assigned rewards of a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    Task ClearCacheForUser(Guid userId);
}

/// <summary>
/// Repository for managing <see cref="AssignedReward"/> entities, supporting async CRUD operations and caching.
/// </summary>
public class AssignedRewardRepository(DatabaseContext databaseContext, IMemoryCache memoryCache) : IAssignedRewardRepository
{
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) };

    public async Task<AssignedReward?> GetByKeyAsync(Guid userId, string reward, RewardType type)
    {
        var cacheKey = $"AssignedReward_{userId}_{reward}_{type}";
        if (memoryCache.TryGetValue(cacheKey, out AssignedReward? assignedReward) && assignedReward != null)
            return assignedReward;

        assignedReward = await databaseContext.AssignedRewards
            .Include(ar => ar.User)
            .FirstOrDefaultAsync(ar => ar.UserId == userId && ar.Reward == reward && ar.Type == type);

        if (assignedReward != null)
            memoryCache.Set(cacheKey, assignedReward, _defaultOptions);

        return assignedReward;
    }

    public async Task<IEnumerable<AssignedReward>> GetByUserIdAsync(Guid userId)
    {
        var cacheKey = $"AssignedRewards_User_{userId}";
        if (memoryCache.TryGetValue(cacheKey, out IEnumerable<AssignedReward>? rewards) && rewards != null)
            return rewards;

        rewards = await databaseContext.AssignedRewards
            .Where(ar => ar.UserId == userId)
            .Include(ar => ar.User)
            .ToListAsync();

        memoryCache.Set(cacheKey, rewards, _defaultOptions);
        return rewards;
    }

    public async Task UpsertAsync(AssignedReward assignedReward)
    {
        var existingReward = await databaseContext.AssignedRewards
            .AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.UserId == assignedReward.UserId && ar.Reward == assignedReward.Reward && ar.Type == assignedReward.Type);

        if (existingReward != null)
        {
            // Update
            databaseContext.AssignedRewards.Update(assignedReward);
        }
        else
        {
            // Add
            await databaseContext.AssignedRewards.AddAsync(assignedReward);
        }

        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"AssignedReward_{assignedReward.UserId}_{assignedReward.Reward}_{assignedReward.Type}");
        memoryCache.Remove($"AssignedRewards_User_{assignedReward.UserId}");
    }

    public async Task DeleteAsync(AssignedReward assignedReward)
    {
        databaseContext.AssignedRewards.Remove(assignedReward);
        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"AssignedReward_{assignedReward.UserId}_{assignedReward.Reward}_{assignedReward.Type}");
        memoryCache.Remove($"AssignedRewards_User_{assignedReward.UserId}");
    }

    public Task ClearCacheFor(Guid userId, string reward, RewardType type)
    {
        memoryCache.Remove($"AssignedReward_{userId}_{reward}_{type}");
        return Task.CompletedTask;
    }

    public Task ClearCacheForUser(Guid userId)
    {
        memoryCache.Remove($"AssignedRewards_User_{userId}");
        return Task.CompletedTask;
    }
}
