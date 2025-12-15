using System;
using System.Threading.Tasks;
using LDTTeam.Authentication.RewardsService.Data;
using LDTTeam.Authentication.RewardsService.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// In-memory and database-backed implementation of IUserLifetimeContributionsRepository.
/// </summary>
public class UserLifetimeContributionsRepository(DatabaseContext dbContext, IMemoryCache cache) : IUserLifetimeContributionsRepository
{
    private const string AllUsersCacheKey = "AllUsersWithLifetimeContributions";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(10);
    private static string GetCacheKey(Guid userId) => $"UserLifetimeContribution_{userId}";

    public async Task SetUserLifetimeContributionAsync(Guid userId, decimal contribution)
    {
        var entity = await dbContext.LifeTimeContributions.FindAsync(userId);
        if (entity == null)
        {
            entity = new UserLifetimeContributions { UserId = userId, LifetimeContributions = contribution };
            await dbContext.LifeTimeContributions.AddAsync(entity);
        }
        else
        {
            entity.LifetimeContributions = contribution;
            dbContext.LifeTimeContributions.Update(entity);
        }
        await dbContext.SaveChangesAsync();
        cache.Set(GetCacheKey(userId), contribution, _cacheDuration);
        cache.Remove(AllUsersCacheKey);
    }

    public async Task<decimal> GetUserLifetimeContributionAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        if (cache.TryGetValue(cacheKey, out decimal value))
            return value;
        var entity = await dbContext.LifeTimeContributions.FindAsync(userId);
        value = entity?.LifetimeContributions ?? 0m;
        cache.Set(cacheKey, value, _cacheDuration);
        return value;
    }

    public async Task RemoveUserLifetimeContributionAsync(Guid userId)
    {
        var entity = await dbContext.LifeTimeContributions.FindAsync(userId);
        if (entity != null)
        {
            dbContext.LifeTimeContributions.Remove(entity);
            await dbContext.SaveChangesAsync();
        }
        cache.Remove(GetCacheKey(userId));
        cache.Remove(AllUsersCacheKey);
    }

    public async Task<List<Guid>> GetAllUsers()
    {
        if (cache.TryGetValue(AllUsersCacheKey, out List<Guid>? userIds))
            return userIds ?? new List<Guid>();
        userIds = await dbContext.LifeTimeContributions
            .Where(x => x.LifetimeContributions > 0)
            .Select(x => x.UserId)
            .ToListAsync();
        cache.Set(AllUsersCacheKey, userIds, _cacheDuration);
        return userIds;
    }
}

