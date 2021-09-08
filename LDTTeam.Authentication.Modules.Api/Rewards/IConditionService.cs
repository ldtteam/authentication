using System.Collections.Generic;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public interface IConditionService
    {
        public Task<bool?> CheckReward(string provider, string providerKey, string rewardId);

        public Task<Dictionary<string, List<string>>> GetRewardsForProvider(string provider);

        public Task<Dictionary<string, bool>?> GetRewardsForUser(string provider, string providerKey);
        
        public Task<Dictionary<string, bool>> GetRewardsForUser(string userId);

        public Task AddConditionToReward(string rewardId, string moduleName, string conditionName, string lambda);

        public Task RemoveConditionFromReward(string rewardId, string moduleName, string conditionName);
    }
}