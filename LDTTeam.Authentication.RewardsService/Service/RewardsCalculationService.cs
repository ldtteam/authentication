using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Messages.User;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace LDTTeam.Authentication.RewardsService.Service;

public class RewardsCalculationService(
    IRewardCalculationsRepository rewardCalculationsRepository,
    IUserRewardAssignmentsRepository userRewardAssignmentsRepository,
    IUserTiersRepository userTiersRepository,
    IUserLifetimeContributionsRepository userLifetimeContributionsRepository,
    IMessageBus messageBus,
    IMemoryCache memoryCache, 
    ILogger<RewardsCalculationService> logger)
    : IRewardsCalculationService
{
    private const string LambdaCacheKey = "RewardCalculationLambdas";

    private async Task EnsureCacheInitializedAsync()
    {
        var calculations = await rewardCalculationsRepository.GetAllRewardCalculationsAsync();
        if (memoryCache.TryGetValue(LambdaCacheKey,
                out Dictionary<(RewardType, string), Func<List<string>, decimal, bool>>? lambdaDict))
        {
            if (lambdaDict != null)
            {
                return;
            }
        }

        lambdaDict = new Dictionary<(RewardType, string), Func<List<string>, decimal, bool>>();
        
        foreach (var (reward, type, lambda) in calculations)
        {
            var key = (type, reward);
            var lambdaCode = "(tiers, lifetime) => " + lambda;
            var options = ScriptOptions.Default.AddReferences(typeof(List<string>).Assembly);
            var compiled = CSharpScript.EvaluateAsync<Func<List<string>, decimal, bool>>(lambdaCode, options).Result;
            lambdaDict[key] = compiled;
        }
        memoryCache.Set(LambdaCacheKey, lambdaDict);
    }

    public async Task RecalculateRewardsAsync(Guid userId)
    {
        await EnsureCacheInitializedAsync();
        var userTiers = await userTiersRepository.GetUserTiersAsync(userId);
        var userLifetime = await userLifetimeContributionsRepository.GetUserLifetimeContributionAsync(userId);
        var currentRewards = await userRewardAssignmentsRepository.GetUserRewardsAsync(userId);
        var currentRewardsSet = new HashSet<(RewardType, string)>(currentRewards);
        var newRewardsSet = new HashSet<(RewardType, string)>();

        var lambdaDict = memoryCache.Get<Dictionary<(RewardType, string), Func<List<string>, decimal, bool>>>(LambdaCacheKey);
        if (lambdaDict == null)
            throw new InvalidOperationException("Reward calculation lambda cache is not initialized.");

        foreach (var kvp in lambdaDict)
        {
            var (type, reward) = kvp.Key;
            var lambda = kvp.Value;
            
            logger.LogInformation("Recalculating reward {reward} of type {type} for user {userId}. Available tiers {Tiers}, and lifetime contributions: {Contributions}", reward, type, userId, userTiers, userLifetime);
            
            bool shouldHave;
            try
            {
                shouldHave = lambda(userTiers, userLifetime);
            }
            catch(Exception exception)
            {
                logger.LogError(exception, "Error evaluating reward calculation lambda for user {userId}, reward {reward}, type {type}", userId, reward, type);
                continue;
            }
            
            if (shouldHave)
                newRewardsSet.Add((type, reward));
        }

        await ProcessRewardChanges(userId, newRewardsSet, currentRewardsSet);
    }

    private async Task ProcessRewardChanges(Guid userId, HashSet<(RewardType, string)> newRewardsSet, HashSet<(RewardType, string)> currentRewardsSet)
    {
        var toAdd = newRewardsSet.Except(currentRewardsSet).ToList();
        var toRemove = currentRewardsSet.Except(newRewardsSet).ToList();

        if (toAdd.Count > 0)
            await userRewardAssignmentsRepository.AddUserRewardsAsync(userId, toAdd);
        if (toRemove.Count > 0)
            await userRewardAssignmentsRepository.RemoveUserRewardsAsync(userId, toRemove);
        
        logger.LogInformation("Processed reward changes for user {userId}. Added: {addedCount}, Removed: {removedCount}", userId, toAdd.Count, toRemove.Count);
        
        foreach (var (type, reward) in toAdd)
        {
            await messageBus.PublishAsync(new UserRewardAdded(userId, type, reward));
        }
        foreach (var (type, reward) in toRemove)
        {
            await messageBus.PublishAsync(new UserRewardRemoved(userId, type, reward));
        }
    }

    public async Task RecalculateRewardForAllUsers(RewardType type, string reward)
    {
        var tierUsers = await userTiersRepository.GetAllUsers();
        var contributionUsers = await userLifetimeContributionsRepository.GetAllUsers();
        var allUsers = tierUsers.Union(contributionUsers).Distinct().ToList();
        
        await EnsureCacheInitializedAsync();
        
        var lambdaDict = memoryCache.Get<Dictionary<(RewardType, string), Func<List<string>, decimal, bool>>>(LambdaCacheKey);
        if (lambdaDict == null)
            throw new InvalidOperationException("Reward calculation lambda cache is not initialized.");
        var lambda = lambdaDict.GetValueOrDefault((type, reward));

        logger.LogWarning("Recalculating reward {reward} of type {type} for all users. Found {userCount} users to process.", reward, type, allUsers.Count());
        
        //We removed the reward entirely, so just remove it from all users
        if (lambda == null)
        {
            foreach (var userId in allUsers)
            {
                var currentRewards = await userRewardAssignmentsRepository.GetUserRewardsAsync(userId);
                var currentRewardsSet = new HashSet<(RewardType, string)>(currentRewards);
                var newRewardsSet = new HashSet<(RewardType, string)>(currentRewards);
                newRewardsSet.Remove((type, reward));

                await ProcessRewardChanges(userId, newRewardsSet, currentRewardsSet);
            }

            return;
        }
        
        // Recalculate for each user
        foreach (var userId in allUsers)
        {
            var userTiers = await userTiersRepository.GetUserTiersAsync(userId);
            var userLifetime = await userLifetimeContributionsRepository.GetUserLifetimeContributionAsync(userId);
            var currentRewards = await userRewardAssignmentsRepository.GetUserRewardsAsync(userId);
            var currentRewardsSet = new HashSet<(RewardType, string)>(currentRewards);
            var newRewardsSet = new HashSet<(RewardType, string)>(currentRewards);
            
            logger.LogInformation("Recalculating reward {reward} of type {type} for user {userId}. Available tiers {Tiers}, and lifetime contributions: {Contributions}", reward, type, userId, userTiers, userLifetime);
            
            bool shouldHave;
            try
            {
                shouldHave = lambda(userTiers, userLifetime);
            }
            catch(Exception exception)
            {
                logger.LogError(exception, "Error evaluating reward calculation lambda for user {userId}, reward {reward}, type {type}", userId, reward, type);
                continue;
            }
            
            if (!shouldHave)
                newRewardsSet.Remove((type, reward));

            await ProcessRewardChanges(userId, newRewardsSet, currentRewardsSet);
        }
    }

    public void ClearCache()
    {
        memoryCache.Remove(LambdaCacheKey);
    }
}
