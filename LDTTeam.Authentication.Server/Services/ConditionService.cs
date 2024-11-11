using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable SpecifyStringComparison

namespace LDTTeam.Authentication.Server.Services
{
    public class ConditionService(DatabaseContext db, IServiceProvider services, ILogger<ConditionService> logger)
        : IConditionService
    {
        public async Task<bool?> CheckReward(string provider, string providerKey, string rewardId, CancellationToken cancellationToken)
        {
            if (!provider.Equals("minecraft", StringComparison.CurrentCultureIgnoreCase))
                return false;
            
            var reward = await db.Rewards
                .Include(x => x.Conditions)
                .FirstOrDefaultAsync(x => x.Id == rewardId, cancellationToken: cancellationToken);

            if (reward == null)
                return null;

            IdentityUserLogin<string>? loginInfo = await db.UserLogins
                .FirstOrDefaultAsync(x =>
                    x.LoginProvider == provider && 
                    x.ProviderKey == providerKey, cancellationToken: cancellationToken);

            var userId = loginInfo?.UserId;
            if (userId != null) return await CheckReward(userId, reward, cancellationToken);

            IdentityUserClaim<string>? claim = await db.UserClaims
                .FirstOrDefaultAsync(x =>
                    x.ClaimValue != null &&
                    x.ClaimType == "urn:minecraft:user:id" && 
                    x.ClaimValue == providerKey, cancellationToken: cancellationToken);

            if (claim == null)
                return false;

            userId = claim.UserId;

            return await CheckReward(userId, reward, cancellationToken);
        }

        private async Task<bool> CheckReward(string userId, Reward reward, CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope     scope  = services.CreateScope();

                foreach (ConditionInstance conditionInstance in reward.Conditions)
                {
                    ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                        x.ModuleName.Equals(conditionInstance.ModuleName,
                            StringComparison.InvariantCultureIgnoreCase) &&
                        x.Name.Equals(conditionInstance.ConditionName, StringComparison.InvariantCultureIgnoreCase));

                    if (condition == null) continue;

                    if (!await condition.ExecuteAsync(scope, conditionInstance,
                            userId,
                            cancellationToken)) continue;

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Api Controller Failed!");
                return false;
            }
        }

        public async Task<Dictionary<string, bool>?> GetRewardsForUser(string provider, string providerKey, CancellationToken token)
        {
            IdentityUserLogin<string>? loginInfo = await db.UserLogins.FirstOrDefaultAsync(x =>
                x.LoginProvider.ToLower() == provider.ToLower() &&
                x.ProviderKey.ToLower() == providerKey.ToLower(), 
                cancellationToken: token);

            var userId = loginInfo?.UserId;
            if (userId != null) return await GetRewardsForUser(userId, token);

            if (provider.ToLower() != "minecraft")
                return null;

            IdentityUserClaim<string>? claim = await db.UserClaims
                .FirstOrDefaultAsync(x =>
                    x.ClaimValue != null &&
                    x.ClaimType == "urn:minecraft:user:id" && 
                    x.ClaimValue.ToLower() == providerKey.ToLower(), cancellationToken: token);

            if (claim == null)
                return null;

            userId = claim.UserId;

            return await GetRewardsForUser(userId, token);
        }

        public async Task<Dictionary<string, bool>> GetRewardsForUser(string userId, CancellationToken token)
        {
            Dictionary<string, bool> results = new();
            await foreach (Reward dbReward in db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable().WithCancellation(token))
            {
                results.Add(dbReward.Id, await CheckReward(userId, dbReward, token));
            }

            return results;
        }

        public async Task<Dictionary<string, List<string>>> GetRewardsForProvider(string provider, CancellationToken token)
        {
            List<IdentityUserLogin<string>>? logins = await db.UserLogins.Where(x =>
                    x.LoginProvider.ToLower() == provider.ToLower())
                .ToListAsync(cancellationToken: token);

            Dictionary<string, List<string>> results = new();
            await foreach (Reward dbReward in db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable().WithCancellation(token))
            {
                List<string> users = new();

                foreach (IdentityUserLogin<string> login in logins)
                {
                    if (await CheckReward(login.UserId, dbReward, token))
                        users.Add(login.ProviderKey);
                }

                results.Add(dbReward.Id, users);
            }

            return results;
        }

        public async Task AddConditionToReward(string rewardId, string moduleName, string conditionName, string lambda, CancellationToken token)
        {
            ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                x.ModuleName.Equals(moduleName,
                    StringComparison.InvariantCultureIgnoreCase) &&
                x.Name.Equals(conditionName, StringComparison.InvariantCultureIgnoreCase));

            if (condition == null)
                throw new AddConditionException("No matching Condition registered for this module and condition name");

            ConditionInstance instance = new()
            {
                RewardId = rewardId,
                ModuleName = condition.ModuleName,
                ConditionName = condition.Name,
                LambdaString = lambda
            };

            if (!condition.Validate(instance))
                throw new AddConditionException(
                    "Error, executing newly created instance failed, probably bad lambda, condition not added");

            if (await db.ConditionInstances.AnyAsync(x =>
                    x.RewardId == rewardId &&
                    x.ModuleName == instance.ModuleName &&
                    x.ConditionName == instance.ConditionName, cancellationToken: token))
                throw new AddConditionException(
                    "Duplicate condition already exists for reward. cannot have duplicates");

            db.ConditionInstances.Add(instance);
            await db.SaveChangesAsync(token);
        }

        public async Task RemoveConditionFromReward(string rewardId, string moduleName, string conditionName, CancellationToken token)
        {
            ConditionInstance? instance = await db.ConditionInstances.FirstOrDefaultAsync(x =>
                x.RewardId.ToLower() == rewardId.ToLower() &&
                x.ModuleName.ToLower() == moduleName.ToLower() &&
                x.ConditionName.ToLower() == conditionName.ToLower(), cancellationToken: token);

            if (instance == null)
                throw new RemoveConditionException("Condition instance not found");

            db.ConditionInstances.Remove(instance);
            await db.SaveChangesAsync(token);
        }
    }
}