using LDTTeam.Authentication.DiscordBot.Data;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

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

    public async Task<User> CreateOrUpdateAsync(User user, CancellationToken token = default)
    {
        var existing = await _db.Users.FindAsync(new object[] { user.UserId }, token);
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
    }
}

