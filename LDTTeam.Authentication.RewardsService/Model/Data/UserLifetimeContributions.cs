using Microsoft.EntityFrameworkCore;

namespace LDTTeam.Authentication.RewardsService.Model.Data;

[PrimaryKey(nameof(UserId))]
public class UserLifetimeContributions
{
    public required Guid UserId { get; set; }
    
    public required decimal LifetimeContributions { get; set; }
}