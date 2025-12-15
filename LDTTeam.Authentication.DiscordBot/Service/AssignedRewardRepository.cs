using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.DiscordBot.Service;

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

    public async Task<IEnumerable<AssignedReward>> GetForUserAsync(Guid userId, CancellationToken token = default)
    {
        var key = $"AssignedRewards:User:{userId}";
        if (_cache.TryGetValue<IEnumerable<AssignedReward>>(key, out var cached)) return cached!;

        var items = await _db.AssignedRewards.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(token);
        _cache.Set(key, items, _defaultOptions);
        return items;
    }

    public async Task<bool> HasRewardAsync(Guid userId, string reward, RewardType type, CancellationToken token = default)
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
        var existing = await _db.AssignedRewards.FindAsync(new object[] { assignment.UserId, assignment.Reward, assignment.Type }, token);
        if (existing is null)
        {
            await _db.AssignedRewards.AddAsync(assignment, token);
            await _db.SaveChangesAsync(token);
        }

        // Invalidate caches for that user
        _cache.Remove($"AssignedRewards:User:{assignment.UserId}");
        _cache.Remove($"AssignedRewards:User:{assignment.UserId}:Reward:{assignment.Reward}:{assignment.Type}");
    }

    public async Task RemoveAsync(Guid userId, string reward, RewardType type, CancellationToken token = default)
    {
        var existing = await _db.AssignedRewards.FindAsync([userId, reward, type], token);
        if (existing is null) return;

        _db.AssignedRewards.Remove(existing);
        await _db.SaveChangesAsync(token);

        _cache.Remove($"AssignedRewards:User:{userId}");
        _cache.Remove($"AssignedRewards:User:{userId}:Reward:{reward}:{type}");
    }
}

