using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// EF Core backed implementation of <see cref="IRoleRewardRepository"/> using <see cref="IMemoryCache"/>.
/// </summary>
public class RoleRewardRepository : IRoleRewardRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public RoleRewardRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<(Snowflake Role, Snowflake Server)>> GetRoleForRewardAsync(string reward, CancellationToken token = default)
    {
        var key = $"RoleReward:Reward:{reward}";
        if (_cache.TryGetValue<IEnumerable<(Snowflake Role, Snowflake Server)>>(key, out var cached)) return cached ?? [];

        var mapping = await _db.RoleRewards
            .Where(r => r.Reward == reward)
            .ToListAsync(cancellationToken: token);

        cached = mapping.Select(role => (role.Role, role.Server)).ToList();
        _cache.Set(key, cached, _defaultOptions);
        return cached;
    }

    public async Task<IEnumerable<RoleRewards>> GetAllAsync(CancellationToken token = default)
    {
        var key = "RoleReward:All";
        if (_cache.TryGetValue<IEnumerable<RoleRewards>>(key, out var cached)) return cached!;

        var all = await _db.RoleRewards.AsNoTracking().ToListAsync(token);
        _cache.Set(key, all, _defaultOptions);
        return all;
    }

    public async Task UpsertAsync(RoleRewards mapping, CancellationToken token = default)
    {
        var existing = await _db.RoleRewards.FindAsync([mapping.Reward, mapping.Role, mapping.Server], token);
        if (existing is null)
        {
            await _db.RoleRewards.AddAsync(mapping, token);
        }
        else
        {
            // nothing to do - mapping already exists
        }

        await _db.SaveChangesAsync(token);

        // Invalidate caches
        _cache.Remove($"RoleReward:Reward:{mapping.Reward}");
        _cache.Remove("RoleReward:All");
    }

    public async Task RemoveAsync(string reward, Snowflake role, Snowflake server, CancellationToken token = default)
    {
        var existing = await _db.RoleRewards.FindAsync([reward, role], token);
        if (existing is null)
            return;
        
        _db.RoleRewards.Remove(existing);
        await _db.SaveChangesAsync(token);
        
        _cache.Remove($"RoleReward:Reward:{reward}");
        _cache.Remove("RoleReward:All");
    }

    public async Task RemoveAllAsync(string reward, CancellationToken token = default)
    {
        var existing = await _db.RoleRewards.Where(r => r.Reward == reward).AsNoTracking().ToListAsync(cancellationToken: token);
        if (existing.Count <= 0) return;

        _db.RoleRewards.RemoveRange(existing);
        await _db.SaveChangesAsync(token);

        _cache.Remove($"RoleReward:Reward:{reward}");
        _cache.Remove("RoleReward:All");
    }
}

