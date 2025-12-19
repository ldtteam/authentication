using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.RewardAPI.Data;
using LDTTeam.Authentication.RewardAPI.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardAPI.Service;

/// <summary>
/// Repository interface for managing <see cref="ProviderLogin"/> entities, supporting async CRUD operations and caching.
/// </summary>
public interface IProviderLoginRepository
{
    /// <summary>
    /// Retrieves a provider login by provider and provider user ID, using cache if available.
    /// </summary>
    /// <param name="provider">The account provider (e.g., Discord, Patreon).</param>
    /// <param name="providerUserId">The user ID on the provider platform.</param>
    /// <returns>The provider login if found, otherwise null.</returns>
    public Task<ProviderLogin?> GetByProviderAndProviderUserIdAsync(AccountProvider provider, string providerUserId);

    /// <summary>
    /// Adds or updates a provider login in the database (upsert) and clears the cache for that login.
    /// </summary>
    /// <param name="login">The provider login entity to add or update.</param>
    public Task UpsertAsync(ProviderLogin login);

    /// <summary>
    /// Deletes a provider login from the database and clears the cache for that login.
    /// </summary>
    /// <param name="login">The provider login entity to delete.</param>
    public Task DeleteAsync(ProviderLogin login);

    /// <summary>
    /// Clears the cache entry for a specific provider login.
    /// </summary>
    /// <param name="provider">The account provider.</param>
    /// <param name="providerUserId">The user ID on the provider platform.</param>
    public Task ClearCacheFor(AccountProvider provider, string providerUserId);
}

public class ProviderLoginRepository(DatabaseContext databaseContext, IMemoryCache memoryCache) : IProviderLoginRepository
{
    private readonly MemoryCacheEntryOptions _defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) };
    
    public async Task<ProviderLogin?> GetByProviderAndProviderUserIdAsync(AccountProvider provider, string providerUserId)
    {
        var cacheKey = $"ProviderLogin_{provider}_{providerUserId}";
        if (!memoryCache.TryGetValue(cacheKey, out ProviderLogin? login) && login != null)
            return login;
        
        login = await databaseContext.Logins
            .Where(l => l.Provider == provider && l.ProviderUserId == providerUserId)
            .Include(l => l.User)
            .Include(l => l.User.Rewards)
            .FirstOrDefaultAsync();

        if (login != null)
            memoryCache.Set(cacheKey, login, _defaultOptions);
        
        return login;
    }

    public async Task UpsertAsync(ProviderLogin login)
    {
        var exists = await databaseContext.Logins.AsNoTracking().AnyAsync(l => l.Provider == login.Provider && l.ProviderUserId == login.ProviderUserId);
        if (exists)
        {
            databaseContext.Logins.Update(login);
        }
        else
        {
            await databaseContext.Logins.AddAsync(login);
        }
        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"ProviderLogin_{login.Provider}_{login.ProviderUserId}");
    }

    public async Task DeleteAsync(ProviderLogin login)
    {
        databaseContext.Logins.Remove(login);
        await databaseContext.SaveChangesAsync();
        memoryCache.Remove($"ProviderLogin_{login.Provider}_{login.ProviderUserId}");
    }

    public Task ClearCacheFor(AccountProvider provider, string providerUserId)
    {
        memoryCache.Remove($"ProviderLogin_{provider}_{providerUserId}");
        return Task.CompletedTask;
    }
}
