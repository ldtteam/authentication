using System.Drawing;
using System.Threading.Tasks;
using LDTTeam.Authentication.Messages.Rewards;
using LDTTeam.Authentication.Server.Models.Data;
using LDTTeam.Authentication.Server.Services;

namespace LDTTeam.Authentication.Server.Handlers;

public class RewardHandler(IRewardRepository repository)
{
    
    public async Task Handle(RewardCreatedOrUpdated message)
    {
        await repository.UpsertAsync(
            new KnownReward
            {
                Name = message.Reward,
                Type = message.Type,
                Lambda = message.Lambda
            });
    }
    
    public async Task Handle(RewardRemoved message)
    {
        await repository.DeleteAsync(message.Type, message.Reward);
    }
}