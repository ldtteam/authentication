using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

/// <summary>
/// Repository-style service for working with <see cref="User"/> entities in the Patreon service.
/// All methods are asynchronous and use the application's <see cref="DatabaseContext"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> to reduce database load.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by the application's internal <see cref="User.UserId"/>.
    /// </summary>
    /// <param name="userId">The user's GUID identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Gets a user by their Patreon <see cref="User.PatreonId"/>.
    /// </summary>
    /// <param name="patreonId">The Patreon user id (string).</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetByPatreonIdAsync(string patreonId, CancellationToken token = default);

    /// <summary>
    /// Gets a user by their Membership <see cref="User.MembershipId"/>
    /// </summary>
    /// <param name="membershipId">The membership id in our patreon campaign (which is not the user id in patreon)</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetByMembershipIdAsync(Guid membershipId, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a user in the database.
    /// On success the returned <see cref="User"/> will reflect any values stored.
    /// </summary>
    /// <param name="user">The user to create or update.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The upserted <see cref="User"/>.</returns>
    Task<User> CreateOrUpdateAsync(User user, CancellationToken token = default);

    /// <summary>
    /// Deletes the user with the specified id if it exists.
    /// </summary>
    /// <param name="userId">The user's GUID identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(Guid userId, CancellationToken token = default);
    
    /// <summary>
    /// Gets all Patreon ids for all users in the database.
    /// </summary>
    Task<IEnumerable<string>> GetAllPatreonIdsAsync(CancellationToken token = default);

    /// <summary>
    /// Gets all membership ids for our campaign members.
    /// </summary>
    Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default);
}

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public UserRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken token = default)
    {
        var cacheKey = $"user:id:{userId}";
        if (_cache.TryGetValue<User>(cacheKey, out var user))
            return user;
        user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, token);
        if (user != null)
            _cache.Set(cacheKey, user, CacheDuration);
        return user;
    }

    public async Task<User?> GetByPatreonIdAsync(string patreonId, CancellationToken token = default)
    {
        var cacheKey = $"user:patreon:{patreonId}";
        if (_cache.TryGetValue<User>(cacheKey, out var user))
            return user;
        user = await _db.Users.FirstOrDefaultAsync(u => u.PatreonId == patreonId, token);
        if (user != null)
            _cache.Set(cacheKey, user, CacheDuration);
        return user;
    }

    public async Task<User?> GetByMembershipIdAsync(Guid membershipId, CancellationToken token = default)
    {
        var cacheKey = $"user:patreon:{membershipId}";
        if (_cache.TryGetValue<User>(cacheKey, out var user))
            return user;
        user = await _db.Users.FirstOrDefaultAsync(u => u.MembershipId == membershipId, token);
        if (user != null)
            _cache.Set(cacheKey, user, CacheDuration);
        return user;   
    }

    public async Task<User> CreateOrUpdateAsync(User user, CancellationToken token = default)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId, token);
        if (existing == null)
        {
            _db.Users.Add(user);
        }
        else
        {
            existing.PatreonId = user.PatreonId;
            existing.Username = user.Username;
            existing.MembershipId = user.MembershipId;
        }
        await _db.SaveChangesAsync(token);
        // Update both cache keys
        _cache.Set($"user:id:{user.UserId}", user, CacheDuration);
        if (!string.IsNullOrEmpty(user.PatreonId))
            _cache.Set($"user:patreon:{user.PatreonId}", user, CacheDuration);
        if (user.MembershipId.HasValue)
            _cache.Set($"user:membership:{user.MembershipId}", user, CacheDuration);
        return user;
    }

    public async Task DeleteAsync(Guid userId, CancellationToken token = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, token);
        if (user != null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(token);
            _cache.Remove($"user:id:{userId}");
            if (!string.IsNullOrEmpty(user.PatreonId))
                _cache.Remove($"user:patreon:{user.PatreonId}");
            if (user.MembershipId.HasValue)
                _cache.Remove($"user:membership:{user.MembershipId}");
        }
    }

    public async Task<IEnumerable<string>> GetAllPatreonIdsAsync(CancellationToken token = default)
    {
        return await _db.Users.Where(u => u.PatreonId != null).Select(u => u.PatreonId!).ToListAsync(token);
    }

    public async Task<IEnumerable<Guid>> GetAllMembershipIdsAsync(CancellationToken token = default)
    {
        return await _db.Users.Where(u => u.MembershipId.HasValue).Select(u => u.MembershipId!.Value).ToListAsync(token);
    }
}
