using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public interface IConditionService
    {
        public Task<bool?> CheckReward(string provider, string providerKey, string rewardId, CancellationToken token);

        public Task<Dictionary<string, List<string>>> GetRewardsForProvider(string provider, CancellationToken token);

        public Task<Dictionary<string, bool>?> GetRewardsForUser(string provider, string providerKey, CancellationToken token);
        
        public Task<Dictionary<string, bool>> GetRewardsForUser(string userId, CancellationToken token);

        public Task AddConditionToReward(string rewardId, string moduleName, string conditionName, string lambda, CancellationToken token);

        public Task RemoveConditionFromReward(string rewardId, string moduleName, string conditionName, CancellationToken token);
    }
}