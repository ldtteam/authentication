using LDTTeam.Authentication.Models.App.Rewards;
using LDTTeam.Authentication.Models.App.User;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.Server.Models.Data;

[PrimaryKey(nameof(UserId), nameof(Reward))]
public class AssignedReward
{
    public required string UserId { get; set; }
    
    public required RewardType Type { get; set; }
    
    public required string Reward { get; set; }
}