using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Repository-style service for working with <see cref="User"/> entities.
/// All methods are asynchronous and use the application's <see cref="Data.DatabaseContext"/> as the backing store.
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
    /// Gets a user by their Discord <see cref="User.Snowflake"/>.
    /// </summary>
    /// <param name="snowflake">The Discord snowflake.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetBySnowflakeAsync(Snowflake snowflake, CancellationToken token = default);

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
    /// Gets all Discord snowflakes for all users in the database.
    /// </summary>
    Task<IEnumerable<Snowflake>> GetAllUserSnowflakesAsync(CancellationToken token = default);
}

/// <summary>
/// EF Core backed implementation of <see cref="IUserRepository"/> using an <see cref="IMemoryCache"/> for local caching.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _db;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) };

    public UserRepository(DatabaseContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken token = default)
    {
        var key = $"User:Id:{userId}";
        if (_cache.TryGetValue<User?>(key, out var cached)) return cached;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, token);
        _cache.Set(key, user, _defaultOptions);
        if (user != null) _cache.Set($"User:Snowflake:{user.Snowflake}", user, _defaultOptions);
        return user;
    }

    public async Task<User?> GetBySnowflakeAsync(Snowflake snowflake, CancellationToken token = default)
    {
        var key = $"User:Snowflake:{snowflake}";
        if (_cache.TryGetValue<User?>(key, out var cached)) return cached;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Snowflake == snowflake, token);
        _cache.Set(key, user, _defaultOptions);
        if (user != null) _cache.Set($"User:Id:{user.UserId}", user, _defaultOptions);
        return user;
    }

    public async Task<IEnumerable<Snowflake>> GetAllUserSnowflakesAsync(CancellationToken token = default)
    {
        var key = "User:AllSnowflakes";
        if (_cache.TryGetValue<List<Snowflake>>(key, out var cached))
            return cached ?? Enumerable.Empty<Snowflake>();

        var snowflakes = await _db.Users.AsNoTracking()
            .Select(u => u.Snowflake)
            .Where(u => u.HasValue)
            .Select(u => u.Value)
            .ToListAsync(token);
        _cache.Set(key, snowflakes, _defaultOptions);
        return snowflakes;
    }

    public async Task<User> CreateOrUpdateAsync(User user, CancellationToken token = default)
    {
        var existing = await _db.Users.FindAsync([user.UserId], token);
        if (existing is null)
        {
            await _db.Users.AddAsync(user, token);
        }
        else
        {
            existing.Snowflake = user.Snowflake;
            _db.Users.Update(existing);
        }

        await _db.SaveChangesAsync(token);

        // update cache
        _cache.Set($"User:Id:{user.UserId}", user, _defaultOptions);
        _cache.Set($"User:Snowflake:{user.Snowflake}", user, _defaultOptions);

        // update cached snowflake list
        var snowflakeListKey = "User:AllSnowflakes";
        if (_cache.TryGetValue<List<Snowflake>>(snowflakeListKey, out var snowflakeList) && user.Snowflake != null)
        {
            var snowflakeValue = user.Snowflake.Value;
            if (snowflakeList != null && !snowflakeList.Contains(snowflakeValue))
            {
                snowflakeList.Add(snowflakeValue);
                _cache.Set(snowflakeListKey, snowflakeList, _defaultOptions);
            }
        }

        return user;
    }

    public async Task DeleteAsync(Guid userId, CancellationToken token = default)
    {
        var existing = await _db.Users.FindAsync(new object[] { userId }, token);
        if (existing is null) return;

        _db.Users.Remove(existing);
        await _db.SaveChangesAsync(token);

        _cache.Remove($"User:Id:{userId}");
        _cache.Remove($"User:Snowflake:{existing.Snowflake}");

        // update cached snowflake list
        var snowflakeListKey = "User:AllSnowflakes";
        if (_cache.TryGetValue<List<Snowflake>>(snowflakeListKey, out var snowflakeList) && existing.Snowflake != null)
        {
            var snowflakeValue = existing.Snowflake.Value;
            if (snowflakeList != null && snowflakeList.Remove(snowflakeValue))
            {
                _cache.Set(snowflakeListKey, snowflakeList, _defaultOptions);
            }
        }
    }

    
}
