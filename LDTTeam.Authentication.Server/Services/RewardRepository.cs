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
/// Repository-style service for working with <see cref="IMemoryCache"/> entities.
/// All methods are asynchronous and use the application's <see cref="Memory"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching"/> to reduce database load and ensure cache consistency.
/// </summary>
public interface IRewardRepository
{
    /// <summary>
    /// Gets a reward by its <see cref="RewardType"/> and name.
    /// </summary>
    /// <param name="type">The reward type.</param>
    /// <param name="name">The reward name.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="KnownReward"/> or <c>null</c> if not found.</returns>
    Task<KnownReward?> GetAsync(RewardType type, string name, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a reward in the database.
    /// On success the returned <see cref="KnownReward"/> will reflect any values stored.
    /// </summary>
    /// <param name="knownReward">The reward to create or update.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The upserted <see cref="KnownReward"/>.</returns>
    Task<KnownReward> UpsertAsync(KnownReward knownReward, CancellationToken token = default);

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
    Task<IEnumerable<KnownReward>> GetAllByTypeAsync(RewardType type, CancellationToken token = default);

    /// <summary>
    /// Gets all rewards in the database.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>All rewards.</returns>
    Task<IEnumerable<KnownReward>> GetAllAsync(CancellationToken token = default);
}

public class RewardRepository : IRewardRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) };

    public RewardRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string GetKey(RewardType type, string name) => $"Reward:{type}:{name}";
    private static string GetTypeKey(RewardType type) => $"Reward:Type:{type}";
    private const string AllKey = "Reward:All";

    public async Task<KnownReward?> GetAsync(RewardType type, string name, CancellationToken token = default)
    {
        var key = GetKey(type, name);
        if (_cache.TryGetValue<KnownReward>(key, out var cached))
            return cached;
        var reward = await _db.KnownRewards.AsNoTracking().FirstOrDefaultAsync(r => r.Type == type && r.Name == name, token);
        if (reward != null)
            _cache.Set(key, reward, _defaultOptions);
        return reward;
    }

    public async Task<KnownReward> UpsertAsync(KnownReward knownReward, CancellationToken token = default)
    {
        var existing = await _db.KnownRewards.FindAsync([knownReward.Type, knownReward.Name], token);
        if (existing is null)
        {
            await _db.KnownRewards.AddAsync(knownReward, token);
        }
        else
        {
            existing.Type = knownReward.Type;
            existing.Name = knownReward.Name;
            _db.KnownRewards.Update(existing);
        }
        await _db.SaveChangesAsync(token);
        // Update cache
        var key = GetKey(knownReward.Type, knownReward.Name);
        _cache.Set(key, knownReward, _defaultOptions);
        // Invalidate by-type and all caches
        _cache.Remove(GetTypeKey(knownReward.Type));
        _cache.Remove(AllKey);
        return knownReward;
    }

    public async Task DeleteAsync(RewardType type, string name, CancellationToken token = default)
    {
        var existing = await _db.KnownRewards.FindAsync([type, name], token);
        if (existing is null) return;
        _db.KnownRewards.Remove(existing);
        await _db.SaveChangesAsync(token);
        // Remove from cache
        var key = GetKey(type, name);
        _cache.Remove(key);
        // Invalidate by-type and all caches
        _cache.Remove(GetTypeKey(type));
        _cache.Remove(AllKey);
    }

    public async Task<IEnumerable<KnownReward>> GetAllByTypeAsync(RewardType type, CancellationToken token = default)
    {
        var key = GetTypeKey(type);
        if (_cache.TryGetValue<List<KnownReward>>(key, out var cached))
            return cached;
        var rewards = await _db.KnownRewards.AsNoTracking().Where(r => r.Type == type).ToListAsync(token);
        _cache.Set(key, rewards, _defaultOptions);
        return rewards;
    }

    public async Task<IEnumerable<KnownReward>> GetAllAsync(CancellationToken token = default)
    {
        if (_cache.TryGetValue<List<KnownReward>>(AllKey, out var cached))
            return cached;
        var rewards = await _db.KnownRewards.AsNoTracking().ToListAsync(token);
        _cache.Set(AllKey, rewards, _defaultOptions);
        return rewards;
    }
}

