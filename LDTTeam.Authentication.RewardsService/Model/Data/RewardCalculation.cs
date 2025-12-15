using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Model.Data;

[PrimaryKey(nameof(Type), nameof(Reward))]
public class RewardCalculation
{
    public required RewardType Type { get; set; }
    
    public required string Reward { get; set; }
    
    public required string Lambda { get; set; }
}