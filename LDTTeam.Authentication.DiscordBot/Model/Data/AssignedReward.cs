using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.DiscordBot.Model.Data;

[PrimaryKey(nameof(UserId), nameof(Reward), nameof(Type))]
public class AssignedReward
{
    public required Guid UserId { get; set; }
    public required string Reward { get; set; }
    public required RewardType Type { get; set; }
}