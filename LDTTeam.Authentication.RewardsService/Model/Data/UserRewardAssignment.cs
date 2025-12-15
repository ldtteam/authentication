using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Model.Data;

[PrimaryKey(nameof(UserId), nameof(Type), nameof(Reward))]
public class UserRewardAssignment
{
    public required Guid UserId { get; set; }
    public required RewardType Type { get; set; }
    public required string Reward { get; set; }
}