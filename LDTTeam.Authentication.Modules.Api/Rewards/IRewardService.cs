using System.Collections.Generic;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public interface IRewardService
    {
        public Task AddReward(string id);

        public Task<ICollection<Reward>> GetRewards();
        
        public Task<Reward?> GetReward(string id);

        public Task RemoveReward(string id);
    }
}