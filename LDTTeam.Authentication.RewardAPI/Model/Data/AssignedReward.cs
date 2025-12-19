using System.ComponentModel.DataAnnotations.Schema;
using LDTTeam.Authentication.Models.App.Rewards;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardAPI.Model.Data;

[PrimaryKey(nameof(UserId), nameof(Reward), nameof(Type))]
[Index(nameof(Reward), nameof(Type), IsUnique = true)]
public class AssignedReward
{
    public required Guid UserId { get; set; }
    public required string Reward { get; set; }
    public required RewardType Type { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}