using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Server.Services
{
    public class RewardService : IRewardService
    {
        private readonly DatabaseContext _db;

        public RewardService(DatabaseContext db)
        {
            _db = db;
        }

        public async Task AddReward(string id)
        {
            await _db.Rewards.AddAsync(new Reward
            {
                Id = id
            });
            await _db.SaveChangesAsync();
        }

        public async Task<ICollection<Reward>> GetRewards()
        {
            return await _db.Rewards.ToListAsync();
        }
        
        public async Task<Reward?> GetReward(string id)
        {
            return await _db.Rewards.Include(x => x.Conditions).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task RemoveReward(string id)
        {
            Reward? reward = await _db.Rewards.FirstOrDefaultAsync(x => x.Id == id);
            _db.Rewards.Remove(reward);
            await _db.SaveChangesAsync();
        }
    }
}