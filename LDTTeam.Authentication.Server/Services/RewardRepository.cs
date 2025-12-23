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

    public async Task<Reward?> GetAsync(RewardType type, string name, CancellationToken token = default)
    {
        var key = GetKey(type, name);
        if (_cache.TryGetValue<Reward>(key, out var cached))
            return cached;
        var reward = await _db.Rewards.AsNoTracking().FirstOrDefaultAsync(r => r.Type == type && r.Name == name, token);
        if (reward != null)
            _cache.Set(key, reward, _defaultOptions);
        return reward;
    }

    public async Task<Reward> UpsertAsync(Reward reward, CancellationToken token = default)
    {
        var existing = await _db.Rewards.FindAsync([reward.Type, reward.Name], token);
        if (existing is null)
        {
            await _db.Rewards.AddAsync(reward, token);
        }
        else
        {
            existing.Type = reward.Type;
            existing.Name = reward.Name;
            _db.Rewards.Update(existing);
        }
        await _db.SaveChangesAsync(token);
        // Update cache
        var key = GetKey(reward.Type, reward.Name);
        _cache.Set(key, reward, _defaultOptions);
        // Invalidate by-type and all caches
        _cache.Remove(GetTypeKey(reward.Type));
        _cache.Remove(AllKey);
        return reward;
    }

    public async Task DeleteAsync(RewardType type, string name, CancellationToken token = default)
    {
        var existing = await _db.Rewards.FindAsync([type, name], token);
        if (existing is null) return;
        _db.Rewards.Remove(existing);
        await _db.SaveChangesAsync(token);
        // Remove from cache
        var key = GetKey(type, name);
        _cache.Remove(key);
        // Invalidate by-type and all caches
        _cache.Remove(GetTypeKey(type));
        _cache.Remove(AllKey);
    }

    public async Task<IEnumerable<Reward>> GetAllByTypeAsync(RewardType type, CancellationToken token = default)
    {
        var key = GetTypeKey(type);
        if (_cache.TryGetValue<List<Reward>>(key, out var cached))
            return cached;
        var rewards = await _db.Rewards.AsNoTracking().Where(r => r.Type == type).ToListAsync(token);
        _cache.Set(key, rewards, _defaultOptions);
        return rewards;
    }

    public async Task<IEnumerable<Reward>> GetAllAsync(CancellationToken token = default)
    {
        if (_cache.TryGetValue<List<Reward>>(AllKey, out var cached))
            return cached;
        var rewards = await _db.Rewards.AsNoTracking().ToListAsync(token);
        _cache.Set(AllKey, rewards, _defaultOptions);
        return rewards;
    }
}

