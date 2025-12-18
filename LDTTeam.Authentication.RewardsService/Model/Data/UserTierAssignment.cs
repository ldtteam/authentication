using LDTTeam.Authentication.Models.App.User;
using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Model.Data;

[PrimaryKey(nameof(UserId), nameof(AccountProvider), nameof(Tier))]
public class UserTierAssignment
{
    public required Guid UserId { get; set; }
    
    public required AccountProvider Provider { get; set; }
    
    public required string Tier { get; set; }
}