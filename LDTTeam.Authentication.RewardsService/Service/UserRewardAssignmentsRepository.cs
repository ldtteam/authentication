using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.RewardsService.Data;
using LDTTeam.Authentication.RewardsService.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// In-memory and database-backed implementation of IUserRewardAssignmentsRepository.
/// Caches per-user reward lists in <see cref="IMemoryCache"/> and keeps them consistent on write operations.
/// </summary>
public class UserRewardAssignmentsRepository(DatabaseContext dbContext, IMemoryCache cache) : IUserRewardAssignmentsRepository
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
    private static string GetCacheKey(Guid userId) => $"UserRewards_{userId}";

    private async Task<List<(RewardType Type, string Reward)>> QueryUserRewardsAsync(Guid userId)
    {
        return await dbContext.RewardAssignments
            .Where(x => x.UserId == userId)
            .Select(x => new ValueTuple<RewardType, string>(x.Type, x.Reward))
            .ToListAsync();
    }

    public async Task AddUserRewardsAsync(Guid userId, List<(RewardType Type, string Reward)> rewards)
    {
        foreach (var (type, reward) in rewards)
        {
            if (!await dbContext.RewardAssignments.AnyAsync(x => x.UserId == userId && x.Type == type && x.Reward == reward))
            {
                await dbContext.RewardAssignments.AddAsync(new UserRewardAssignment { UserId = userId, Type = type, Reward = reward });
            }
        }
        await dbContext.SaveChangesAsync();
        var updatedRewards = await QueryUserRewardsAsync(userId);
        cache.Set(GetCacheKey(userId), updatedRewards, _cacheDuration);
    }

    public async Task RemoveUserRewardsAsync(Guid userId, List<(RewardType Type, string Reward)> rewards)
    {
        var assignmentsRemoved = false;
        foreach (var (rewardType, reward) in rewards)
        {
            var assignment = await dbContext.RewardAssignments
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Type == rewardType && x.Reward == reward);
            
            if (assignment == null) continue;
            
            dbContext.RewardAssignments.Remove(assignment);
            assignmentsRemoved = true;

        }
        
        if (assignmentsRemoved)
        {
            await dbContext.SaveChangesAsync();
            var updatedRewards = await QueryUserRewardsAsync(userId);
            cache.Set(GetCacheKey(userId), updatedRewards, _cacheDuration);
        }
        
    }

    public async Task<List<(RewardType Type, string Reward)>> GetUserRewardsAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        if (cache.TryGetValue(cacheKey, out List<(RewardType, string)>? rewards))
            return rewards ?? new List<(RewardType, string)>();
        rewards = await QueryUserRewardsAsync(userId);
        cache.Set(cacheKey, rewards, _cacheDuration);
        return rewards;
    }

    /// <summary>
    /// Removes all assignments for the given reward and type across all users.
    /// Invalidates any per-user caches that contained the removed assignments.
    /// </summary>
    public async Task RemoveAllAssignmentsForRewardAsync(string messageReward, RewardType messageType)
    {
        // Find all matching assignments
        var assignments = await dbContext.RewardAssignments
            .Where(x => x.Reward == messageReward && x.Type == messageType)
            .ToListAsync();

        if (assignments.Count == 0) return;

        // Collect affected user ids to invalidate cache
        var affectedUserIds = assignments.Select(a => a.UserId).Distinct().ToList();

        dbContext.RewardAssignments.RemoveRange(assignments);
        await dbContext.SaveChangesAsync();

        // Invalidate caches for affected users
        foreach (var userId in affectedUserIds)
        {
            cache.Remove(GetCacheKey(userId));
        }
    }
}
