using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.DiscordBot.Service;

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

