using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

/// <summary>
/// Repository-style service for working with <see cref="Reward"/> entities in the Patreon service.
/// All methods are asynchronous and use the application's <see cref="DatabaseContext"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> to reduce database load.
/// </summary>
public interface IRewardRepository
{
    /// <summary>
    /// Gets a reward by its <see cref="Reward.MembershipId"/>.
    /// </summary>
    Task<Reward?> GetByIdAsync(Guid membershipId, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a reward in the database.
    /// </summary>
    Task<Reward> CreateOrUpdateAsync(Reward reward, CancellationToken token = default);
    
    /// <summary>
    /// Deletes the reward with the specified id if it exists.
    /// </summary>
    Task DeleteAsync(Guid membershipId, CancellationToken token = default);

    /// <summary>
    /// Gets all reward ids in the database.
    /// </summary>
    Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default);
}

public class RewardRepository : IRewardRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public RewardRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Reward?> GetByIdAsync(Guid membershipId, CancellationToken token = default)
    {
        var cacheKey = $"reward:id:{membershipId}";
        if (_cache.TryGetValue<Reward>(cacheKey, out var reward))
            return reward;
        reward = await _db.Rewards
            .Include(m => m.Tiers)
            .FirstOrDefaultAsync(m => m.MembershipId == membershipId, token);
        if (reward != null)
            _cache.Set(cacheKey, reward, CacheDuration);
        return reward;
    }

    public async Task<Reward> CreateOrUpdateAsync(Reward reward, CancellationToken token = default)
    {
        var existing = await _db.Rewards.FirstOrDefaultAsync(m => m.MembershipId == reward.MembershipId, token);
        if (existing == null)
        {
            _db.Rewards.Add(reward);
        }
        else
        {
            _db.Rewards.Update(reward);
        }
        
        await _db.SaveChangesAsync(token);
        
        // Update cache
        _cache.Set($"reward:id:{reward.MembershipId}", reward, CacheDuration);
        return reward;
    }

    public async Task DeleteAsync(Guid membershipId, CancellationToken token = default)
    {
        var reward = await _db.Rewards.FirstOrDefaultAsync(m => m.MembershipId == membershipId, token);
        if (reward != null)
        {
            _db.Rewards.Remove(reward);
            await _db.SaveChangesAsync(token);
            _cache.Remove($"reward:id:{membershipId}");
        }
    }

    public async Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default)
    {
        return await _db.Rewards.AsNoTracking().Select(m => m.MembershipId).ToListAsync(token);
    }
}
