using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.DiscordBot.Model.Data;

[PrimaryKey(nameof(Type), nameof(Name))]
public class Reward
{
    public required RewardType Type { get; set; }
    public required string Name { get; set; }
    public required string Lambda { get; set; }
}