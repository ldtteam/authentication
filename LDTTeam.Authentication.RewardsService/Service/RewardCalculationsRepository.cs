using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.RewardsService.Data;
using LDTTeam.Authentication.RewardsService.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// In-memory and database-backed implementation of IRewardCalculationsRepository.
/// </summary>
public class RewardCalculationsRepository(DatabaseContext dbContext, IMemoryCache cache) : IRewardCalculationsRepository
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
    private const string AllRewardsCacheKey = "AllRewardCalculations";

    public async Task AddOrUpdateRewardCalculationAsync(string reward, RewardType type, string lambda)
    {
        var entity = await dbContext.RewardCalculations.FindAsync(type, reward);
        if (entity == null)
        {
            entity = new RewardCalculation { Reward = reward, Type = type, Lambda = lambda };
            await dbContext.RewardCalculations.AddAsync(entity);
        }
        else
        {
            entity.Lambda = lambda;
            dbContext.RewardCalculations.Update(entity);
        }
        await dbContext.SaveChangesAsync();
        await UpdateCacheAsync();
    }

    public async Task RemoveRewardCalculationAsync(string reward, RewardType type)
    {
        var entity = await dbContext.RewardCalculations.FindAsync(type, reward);
        if (entity != null)
        {
            dbContext.RewardCalculations.Remove(entity);
            await dbContext.SaveChangesAsync();
            await UpdateCacheAsync();
        }
    }

    public async Task<List<(string Reward, RewardType Type, string Lambda)>> GetAllRewardCalculationsAsync()
    {
        if (!cache.TryGetValue(AllRewardsCacheKey, out List<(string, RewardType, string)>? rewards))
        {
            rewards = await QueryAllRewardCalculationsAsync();
            cache.Set(AllRewardsCacheKey, rewards, _cacheDuration);
        }
        return rewards ?? new List<(string, RewardType, string)>();
    }

    private async Task<List<(string Reward, RewardType Type, string Lambda)>> QueryAllRewardCalculationsAsync()
    {
        return await dbContext.RewardCalculations
            .Select(rc => new ValueTuple<string, RewardType, string>(rc.Reward, rc.Type, rc.Lambda))
            .ToListAsync();
    }

    private async Task UpdateCacheAsync()
    {
        var all = await QueryAllRewardCalculationsAsync();
        cache.Set(AllRewardsCacheKey, all, _cacheDuration);
    }
}

