using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Server.Data;
using LDTTeam.Authentication.Server.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.Server.Services;

/// <summary>
/// Service for managing assigned rewards for users.
/// Uses EF Core as the backing store and should be implemented with an <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
/// </summary>
public interface IAssignedRewardRepository
{
    /// <summary>
    /// Gets all assigned rewards for a user.
    /// </summary>
    Task<IEnumerable<AssignedReward>> GetForUserAsync(string userId, CancellationToken token = default);

    /// <summary>
    /// Checks whether a user has the specified reward.
    /// </summary>
    Task<bool> HasRewardAsync(string userId, string reward, RewardType type, CancellationToken token = default);

    /// <summary>
    /// Assigns a reward to a user.
    /// If the reward already exists this becomes a no-op.
    /// </summary>
    Task AssignAsync(AssignedReward assignment, CancellationToken token = default);

    /// <summary>
    /// Removes a reward from a user.
    /// </summary>
    Task RemoveAsync(string userId, string reward, RewardType type, CancellationToken token = default);
}

/// <summary>
/// EF Core backed implementation for managing <see cref="AssignedReward"/> entities.
/// Uses <see cref="IMemoryCache"/> to cache per-user assignments.
/// </summary>
public class AssignedRewardRepository : IAssignedRewardRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) };

    public AssignedRewardRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<AssignedReward>> GetForUserAsync(string userId, CancellationToken token = default)
    {
        var key = $"AssignedRewards:User:{userId}";
        if (_cache.TryGetValue<IEnumerable<AssignedReward>>(key, out var cached)) return cached!;

        var items = await _db.AssignedRewards.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(token);
        _cache.Set(key, items, _defaultOptions);
        return items;
    }

    public async Task<bool> HasRewardAsync(string userId, string reward, RewardType type, CancellationToken token = default)
    {
        var key = $"AssignedRewards:User:{userId}:Reward:{reward}:{type}";
        if (_cache.TryGetValue<bool?>(key, out var cachedBool) && cachedBool.HasValue) return cachedBool.Value;

        var exists = await _db.AssignedRewards.AnyAsync(a => a.UserId == userId && a.Reward == reward && a.Type == type, token);
        _cache.Set(key, exists, _defaultOptions);
        // also invalidate user list to keep it consistent
        _cache.Remove($"AssignedRewards:User:{userId}");
        return exists;
    }

    public async Task AssignAsync(AssignedReward assignment, CancellationToken token = default)
    {
        var existing = await _db.AssignedRewards.FindAsync([assignment.UserId, assignment.Type, assignment.Reward], token);
        if (existing is null)
        {
            await _db.AssignedRewards.AddAsync(assignment, token);
            await _db.SaveChangesAsync(token);
        }

        // Invalidate caches for that user
        _cache.Remove($"AssignedRewards:User:{assignment.UserId}");
        _cache.Remove($"AssignedRewards:User:{assignment.UserId}:Reward:{assignment.Reward}:{assignment.Type}");
    }

    public async Task RemoveAsync(string userId, string reward, RewardType type, CancellationToken token = default)
    {
        var existing = await _db.AssignedRewards.FindAsync([userId, type, reward], token);
        if (existing is null) return;

        _db.AssignedRewards.Remove(existing);
        await _db.SaveChangesAsync(token);

        _cache.Remove($"AssignedRewards:User:{userId}");
        _cache.Remove($"AssignedRewards:User:{userId}:Reward:{reward}:{type}");
    }
}

