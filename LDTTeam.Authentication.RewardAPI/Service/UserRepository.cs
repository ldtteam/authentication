using LDTTeam.Authentication.RewardAPI.Data;
using LDTTeam.Authentication.RewardAPI.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardAPI.Service;

/// <summary>
/// Repository for managing <see cref="User"/> entities, supporting async CRUD operations and caching.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier, using cache if available.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Adds or updates a user in the database (upsert) and clears the cache for that user.
    /// </summary>
    /// <param name="user">The user entity to add or update.</param>
    Task UpsertAsync(User user);

    /// <summary>
    /// Deletes a user from the database and clears the cache for that user.
    /// </summary>
    /// <param name="user">The user entity to delete.</param>
    Task DeleteAsync(User user);

    /// <summary>
    /// Clears the cache entry for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    Task ClearCacheFor(Guid userId);
}

public class UserRepository(DatabaseContext databaseContext, IMemoryCache memoryCache) : IUserRepository
{
    private readonly MemoryCacheEntryOptions _defaultOptions = new()
        { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) };

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        var cacheKey = $"User_{userId}";
        if (memoryCache.TryGetValue(cacheKey, out User? user) && user != null)
            return user;

        user = await databaseContext.Users
            .Include(u => u.Rewards)
            .Include(u => u.Logins)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user != null)
            memoryCache.Set(cacheKey, user, _defaultOptions);

        return user;
    }

    public async Task UpsertAsync(User user)
    {
        var exists = await databaseContext.Users.AsNoTracking().AnyAsync(u => u.UserId == user.UserId);
        if (exists)
        {
            databaseContext.Users.Update(user);
        }
        else
        {
            await databaseContext.Users.AddAsync(user);
        }
        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"User_{user.UserId}");
    }

    public async Task DeleteAsync(User user)
    {
        databaseContext.Users.Remove(user);
        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"User_{user.UserId}");
    }

    public Task ClearCacheFor(Guid userId)
    {
        memoryCache.Remove($"User_{userId}");
        return Task.CompletedTask;
    }
}
