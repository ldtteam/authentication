using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable SpecifyStringComparison

namespace LDTTeam.Authentication.Server.Services
{
    public class ConditionService : IConditionService
    {
        private readonly DatabaseContext _db;
        private readonly IServiceProvider _services;
        private readonly ILogger<ConditionService> _logger;

        public ConditionService(DatabaseContext db, IServiceProvider services, ILogger<ConditionService> logger)
        {
            _db = db;
            _services = services;
            _logger = logger;
        }

        public async Task<bool> CheckReward(string provider, string providerKey, string rewardId)
        {
            IdentityUserLogin<string>? loginInfo = await _db.UserLogins.FirstOrDefaultAsync(x =>
                x.LoginProvider.ToLower() == provider.ToLower() &&
                x.ProviderKey.ToLower() == providerKey.ToLower());

            string? userId = loginInfo?.UserId;
            if (userId == null)
            {
                if (provider.ToLower() != "minecraft")
                    return false;

                IdentityUserClaim<string>? claim = await _db.UserClaims
                    .FirstOrDefaultAsync(x =>
                        x.ClaimType == "urn:minecraft:user:id" && x.ClaimValue.ToLower() == providerKey.ToLower());

                if (claim == null)
                    return false;

                userId = claim.UserId;
            }

            Reward? reward = await _db.Rewards
                .Include(x => x.Conditions)
                .FirstOrDefaultAsync(x => x.Id == rewardId);

            if (reward == null)
                return false;

            return await CheckReward(userId, reward);
        }

        private async Task<bool> CheckReward(string userId, Reward reward)
        {
            try
            {
                CancellationTokenSource source = new();
                using IServiceScope scope = _services.CreateScope();

                foreach (ConditionInstance conditionInstance in reward.Conditions)
                {
                    ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                        x.ModuleName.Equals(conditionInstance.ModuleName,
                            StringComparison.InvariantCultureIgnoreCase) &&
                        x.Name.Equals(conditionInstance.ConditionName, StringComparison.InvariantCultureIgnoreCase));

                    if (condition == null) continue;

                    if (!await condition.ExecuteAsync(scope, conditionInstance,
                        userId,
                        source.Token)) continue;

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Api Controller Failed!");
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> GetRewardsForUser(string userId)
        {
            Dictionary<string, bool> results = new();
            await foreach (Reward dbReward in _db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable())
            {
                results.Add(dbReward.Id, await CheckReward(userId, dbReward));
            }

            return results;
        }

        public async Task<Dictionary<string, List<string>>> GetRewardsForProvider(string provider)
        {
            List<IdentityUserLogin<string>>? logins = await _db.UserLogins.Where(x =>
                    x.LoginProvider.ToLower() == provider.ToLower())
                .ToListAsync();

            Dictionary<string, List<string>> results = new();
            await foreach (Reward dbReward in _db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable())
            {
                List<string> users = new();

                foreach (IdentityUserLogin<string> login in logins)
                {
                    if (await CheckReward(login.UserId, dbReward))
                        users.Add(login.ProviderKey);
                }

                results.Add(dbReward.Id, users);
            }

            return results;
        }
    }
}