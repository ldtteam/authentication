using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

/// <summary>
/// Repository-style service for working with <see cref="Membership"/> entities in the Patreon service.
/// All methods are asynchronous and use the application's <see cref="DatabaseContext"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> to reduce database load.
/// </summary>
public interface IMembershipRepository
{
    /// <summary>
    /// Gets a membership by its <see cref="Membership.MembershipId"/>.
    /// </summary>
    Task<Membership?> GetByIdAsync(Guid membershipId, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a membership in the database.
    /// </summary>
    Task<Membership> CreateOrUpdateAsync(Membership membership, CancellationToken token = default);
    
    /// <summary>
    /// Deletes the membership with the specified id if it exists.
    /// </summary>
    Task DeleteAsync(Guid membershipId, CancellationToken token = default);

    /// <summary>
    /// Gets all membership ids in the database.
    /// </summary>
    Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default);
}

public class MembershipRepository : IMembershipRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public MembershipRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Membership?> GetByIdAsync(Guid membershipId, CancellationToken token = default)
    {
        var cacheKey = $"membership:id:{membershipId}";
        if (_cache.TryGetValue<Membership>(cacheKey, out var membership))
            return membership;
        membership = await _db.Memberships
            .Include(m => m.Tiers)
            .FirstOrDefaultAsync(m => m.MembershipId == membershipId, token);
        if (membership != null)
            _cache.Set(cacheKey, membership, CacheDuration);
        return membership;
    }

    public async Task<Membership> CreateOrUpdateAsync(Membership membership, CancellationToken token = default)
    {
        var existing = await _db.Memberships.AnyAsync(m => m.MembershipId == membership.MembershipId, token);
        if (!existing)
        {
            _db.Memberships.Add(membership);
        }
        else
        {
            _db.Memberships.Update(membership);
        }
        await _db.SaveChangesAsync(token);
        // Update cache
        _cache.Set($"membership:id:{membership.MembershipId}", membership, CacheDuration);
        return membership;
    }

    public async Task DeleteAsync(Guid membershipId, CancellationToken token = default)
    {
        var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.MembershipId == membershipId, token);
        if (membership != null)
        {
            _db.Memberships.Remove(membership);
            await _db.SaveChangesAsync(token);
            _cache.Remove($"membership:id:{membershipId}");
        }
    }

    public async Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default)
    {
        return await _db.Memberships.Select(m => m.MembershipId).ToListAsync(token);
    }
}
