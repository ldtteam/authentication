using LDTTeam.Authentication.RewardsService.Data;
using LDTTeam.Authentication.RewardsService.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.User;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for managing user tiers in the rewards service.
/// Provides methods to add, remove, and retrieve tiers associated with a user.
/// </summary>
public interface IUserTiersRepository
{
    /// <summary>
    /// Asynchronously adds the specified tiers to the user with the given userId.
    /// </summary>
    public Task AddUserTiersAsync(Guid userId, AccountProvider accountProvider, List<string> tiers);

    /// <summary>
    /// Asynchronously removes the specified tiers from the user with the given userId.
    /// </summary>
    public Task RemoveUserTiersAsync(Guid userId, AccountProvider accountProvider, List<string> tiers);

    /// <summary>
    /// Asynchronously retrieves the list of tiers associated with the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of tier names associated with the user.</returns>
    public Task<List<string>> GetUserTiersAsync(Guid userId);

    /// <summary>
    /// Asynchronously retrieves all user IDs that have any tiers assigned.
    /// </summary>
    /// <returns>The list of all user ids</returns>
    public Task<List<Guid>> GetAllUsers();
}

/// <summary>
/// In-memory and database-backed implementation of IUserTiersRepository.
/// </summary>
public class UserTiersRepository(DatabaseContext dbContext, IMemoryCache cache) : IUserTiersRepository
{
    private const string AllUsersCacheKey = "AllUsersWithTiers";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(6);

    private async Task<List<string>> QueryUserTiersAsync(Guid userId)
    {
        return await dbContext.TierAssignments
            .Where(x => x.UserId == userId)
            .Select(x => x.Tier)
            .Distinct()
            .ToListAsync();
    }

    public async Task AddUserTiersAsync(Guid userId, AccountProvider provider, List<string> tiers)
    {
        foreach (var tier in tiers)
        {
            if (!await dbContext.TierAssignments.AnyAsync(x => x.UserId == userId && x.Tier == tier && x.Provider == provider))
            {
                await dbContext.TierAssignments.AddAsync(new UserTierAssignment { UserId = userId, Provider = provider, Tier = tier });
            }
        }
        await dbContext.SaveChangesAsync();
        var updatedTiers = await QueryUserTiersAsync(userId);
        cache.Set(GetCacheKey(userId), updatedTiers, _cacheDuration);
        cache.Remove(AllUsersCacheKey);
    }

    public async Task RemoveUserTiersAsync(Guid userId, AccountProvider provider, List<string> tiers)
    {
        var assignments = await dbContext.TierAssignments
            .Where(x => x.UserId == userId && x.Provider == provider && tiers.Contains(x.Tier))
            .ToListAsync();
        if (assignments.Count > 0)
        {
            dbContext.TierAssignments.RemoveRange(assignments);
            await dbContext.SaveChangesAsync();
            var updatedTiers = await QueryUserTiersAsync(userId);
            cache.Set(GetCacheKey(userId), updatedTiers, _cacheDuration);
        }
        cache.Remove(AllUsersCacheKey);
    }

    public async Task<List<string>> GetUserTiersAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        if (cache.TryGetValue(cacheKey, out List<string>? tiers)) return tiers ?? new List<string>();
        tiers = await QueryUserTiersAsync(userId);
        cache.Set(cacheKey, tiers, _cacheDuration);
        return tiers;
    }

    public async Task<List<Guid>> GetAllUsers()
    {
        if (cache.TryGetValue(AllUsersCacheKey, out List<Guid>? cachedUsers))
        {
            return cachedUsers ?? new List<Guid>();
        }

        var users = await dbContext.TierAssignments
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        cache.Set(AllUsersCacheKey, users, _cacheDuration);
        return users;
    }

    private static string GetCacheKey(Guid userId) => $"UserTiers_{userId}";
}