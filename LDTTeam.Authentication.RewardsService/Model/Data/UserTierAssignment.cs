using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Model.Data;

[PrimaryKey(nameof(UserId), nameof(Tier))]
public class UserTierAssignment
{
    public required Guid UserId { get; set; }
    
    public required string Tier { get; set; }
}